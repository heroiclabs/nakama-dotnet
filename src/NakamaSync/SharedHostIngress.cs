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
    internal class SharedHostIngress
    {
        private EnvelopeBuilder _builder;
        private readonly VarKeys _keys;

        public SharedHostIngress(EnvelopeBuilder builder, VarKeys keys)
        {
            _builder = builder;
            _keys = keys;
        }

        public void HandleValue<T>(IUserPresence source, SharedContext<T> context)
        {
            switch (context.Value.KeyValidationStatus)
            {
                case KeyValidationStatus.None:
                    HandleNonValidatedValue(source, context.Var, context.Value);
                    break;
                case KeyValidationStatus.Pending:
                    if (context.Var.OnHostValidate(new SharedVarEvent<T>(source, context.Var.GetValue(), context.Value.Value)))
                    {
                        AcceptPendingValue<T>(source, context.Var, context.Value, context.VarAccessor, context.AckAccessor);
                    }
                    else
                    {
                        RollbackPendingValue<T>(context.Var, context.Value, context.VarAccessor);
                    }
                    break;
                case KeyValidationStatus.Validated:
                    throw new InvalidOperationException("Host received value that already claims to be validated.");
            }
        }

        private void RollbackPendingValue<T>(SharedVar<T> var, SharedValue<T> value, SharedVarAccessor<T> accessor)
        {
            // one guest has incorrect value. queue a rollback for all guests.
            _keys.IncrementLockVersion(value.Key);
            var outgoing = new SharedValue<T>(value.Key, var.GetValue(), _keys.GetLockVersion(value.Key), KeyValidationStatus.Validated);
            _builder.AddSharedVar(accessor, value);
            _builder.SendEnvelope();
        }

        private void AcceptPendingValue<T>(IUserPresence source, SharedVar<T> var, SharedValue<T> value, SharedVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            var.SetValue(source, value.Value, KeyValidationStatus.Validated, var.OnRemoteValueChanged);
            _builder.AddSharedVar(accessor, value);
            _builder.AddAck(ackAccessor, value.Key);
            _builder.SendEnvelope();
        }

        private void HandleNonValidatedValue<T>(IUserPresence source, SharedVar<T> var, SharedValue<T> value)
        {
            var.SetValue(source, value.Value, KeyValidationStatus.None, var.OnRemoteValueChanged);
        }
    }
}
