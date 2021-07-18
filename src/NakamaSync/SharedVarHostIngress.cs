/**
* Copyright 2021 The Nakama Authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using Nakama;

namespace NakamaSync
{
    internal class SharedVarHostIngress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly LockVersionGuard _lockVersionGuard;
        private EnvelopeBuilder _builder;

        public SharedVarHostIngress(LockVersionGuard lockVersionGuard, EnvelopeBuilder builder)
        {
            _lockVersionGuard = lockVersionGuard;
            _builder = builder;
        }

        public void ProcessValue<T>(IUserPresence source, SharedVarIngressContext<T> context)
        {
            switch (context.Value.ValidationStatus)
            {
                case ValidationStatus.None:
                    HandleNonValidatedValue(source, context.Var, context.Value);
                    break;
                case ValidationStatus.Pending:
                    T oldValue = context.Var.GetValue();
                    T newValue = context.Value.Value;
                    var valueChange = new ValueChange<T>(oldValue, newValue);

                    ValidationStatus oldStatus = context.Var.ValidationStatus;
                    ValidationStatus newStatus;
                    if (context.Var.RequiresValidation)
                    {
                        newStatus = context.Var.InvokeValidationHandler(source, valueChange) ? ValidationStatus.Validated : ValidationStatus.Invalid;
                    }
                    else
                    {
                        newStatus = oldStatus;
                        ErrorHandler?.Invoke(new InvalidOperationException("Pending value has no host validation handler."));
                    }

                    if (newStatus == ValidationStatus.Validated || newStatus == ValidationStatus.Pending)
                    {
                        AcceptPendingValue<T>(source, context.Var, context.Value, context.VarAccessor, context.AckAccessor);
                    }
                    else
                    {
                        RollbackPendingValue<T>(context.Var, context.Value, context.VarAccessor);
                    }
                    break;
                case ValidationStatus.Validated:
                    ErrorHandler?.Invoke(new InvalidOperationException("Host received value that already claims to be validated."));
                    break;
            }
        }

        private void RollbackPendingValue<T>(SharedVar<T> var, SharedValue<T> value, SharedVarAccessor<T> accessor)
        {
            // one guest has incorrect value. queue a rollback for all guests.
            _lockVersionGuard.IncrementLockVersion(value.Key);
            var outgoing = new SharedValue<T>(value.Key, var.GetValue(), _lockVersionGuard.GetLockVersion(value.Key), ValidationStatus.Validated);
            _builder.AddSharedVar(accessor, value);
            _builder.SendEnvelope();
        }

        private void AcceptPendingValue<T>(IUserPresence source, SharedVar<T> var, SharedValue<T> value, SharedVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            var.SetValue(source, value.Value, ValidationStatus.Validated);
            _builder.AddSharedVar(accessor, value);
            _builder.AddAck(ackAccessor, value.Key);
            _builder.SendEnvelope();
        }

        private void HandleNonValidatedValue<T>(IUserPresence source, SharedVar<T> var, SharedValue<T> value)
        {
            var.SetValue(source, value.Value, ValidationStatus.None);
        }
    }
}
