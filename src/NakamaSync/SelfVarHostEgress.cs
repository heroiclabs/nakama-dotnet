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
    internal class SelfVarHostEgress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly EnvelopeBuilder _builder;

        public SelfVarHostEgress(EnvelopeBuilder builder)
        {
            _builder = builder;
        }

        public void HandleLocalSelfVarChanged<T>(PresenceVarKey key, SelfVar<T> var, T newValue, PresenceVarAccessor<T> accessor)
        {
            var status = var.ValidationStatus;

            if (status == ValidationStatus.Pending)
            {
                ErrorHandler?.Invoke(new InvalidOperationException("Host should not have local key pending validation: " + key));
                return;
            }

            var selfValue = new PresenceValue<T>(key, newValue, status);
            _builder.AddPresenceVar(accessor, selfValue);
            _builder.SendEnvelope();
        }
    }
}
