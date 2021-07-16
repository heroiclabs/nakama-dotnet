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
    internal class UserHostIngress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly VarKeys _keys;
        private EnvelopeBuilder _builder;

        public UserHostIngress(VarKeys keys, EnvelopeBuilder builder)
        {
            _keys = keys;
            _builder = builder;
        }

        public void HandleValue<T>(IUserPresence source, UserIngressContext<T> context)
        {
            switch (context.Value.ValidationStatus)
            {
                case KeyValidationStatus.None:
                    HandleNonValidatedValue(source, context.Var, context.Value, context.Value.TargetId);
                    break;
                case KeyValidationStatus.Pending:
                    if (context.Var.OnHostValidate(new UserVarEvent<T>(source, context.Value.TargetId, context.Var.GetValue(context.Value.TargetId), context.Value.Value)))
                    {
                        AcceptPendingValue<T>(source, context.Var, context.Value, context.VarAccessor, context.AckAccessor);
                    }
                    else
                    {
                        RollbackPendingValue<T>(context.Var, context.Value, context.VarAccessor);
                    }
                    break;
                case KeyValidationStatus.Validated:
                    ErrorHandler?.Invoke(new InvalidOperationException("Host received value that already claims to be validated."));
                    break;
            }
        }

        private void RollbackPendingValue<T>(UserVar<T> var, UserValue<T> value, UserVarAccessor<T> accessor)
        {
            // one guest has incorrect value. queue a rollback for all guests.
            _keys.IncrementLockVersion(value.Key);
            var outgoing = new UserValue<T>(value.Key, var.GetValue(), _keys.GetLockVersion(value.Key), KeyValidationStatus.Validated, value.TargetId);
            _builder.AddUserVar(accessor, value);
            _builder.SendEnvelope();
        }

        private void AcceptPendingValue<T>(IUserPresence source, UserVar<T> var, UserValue<T> value, UserVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            var.SetValue(value.Value, source, value.TargetId, KeyValidationStatus.Validated, var.OnRemoteValueChanged);
            _builder.AddUserVar(accessor, value);
            _builder.AddAck(ackAccessor, value.Key);
            _builder.SendEnvelope();
        }

        private void HandleNonValidatedValue<T>(IUserPresence source, UserVar<T> var, UserValue<T> value, string targetId)
        {
            var.SetValue(value.Value, source, targetId);
        }
    }
}
