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
    internal class HostIngress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly LockVersionGuard _lockVersionGuard;
        private EnvelopeBuilder _builder;

        public HostIngress(LockVersionGuard lockVersionGuard, EnvelopeBuilder builder)
        {
            _lockVersionGuard = lockVersionGuard;
            _builder = builder;
        }

        public void ProcessValue<T>(IUserPresence source, VarIngressContext<T> context)
        {
            if (context.Value.ValidationStatus == ValidationStatus.Validated)
            {
                ErrorHandler?.Invoke(new InvalidOperationException("Host received value that already claims to be validated."));
                return;
            }

            bool success = context.Var.SetValue(source, context.Value.Value, context.Value.ValidationStatus);

            if (context.Value.ValidationStatus != ValidationStatus.Pending)
            {
                return;
            }

            if (success)
            {
                _builder.AddVar(context.VarAccessor, context.Value);
                _builder.AddAck(context.Value.Key);
                _builder.SendEnvelope();
            }
            else
            {
                _lockVersionGuard.IncrementLockVersion(context.Value.Key);
                var outgoing = new VarValue<T>(context.Value.Key, context.Var.GetValue(), _lockVersionGuard.GetLockVersion(context.Value.Key), ValidationStatus.Validated);
                _builder.AddVar(context.VarAccessor, context.Value);
                _builder.SendEnvelope();
            }
        }
    }
}
