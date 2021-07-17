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

using Nakama;
using System.Collections.Generic;

namespace NakamaSync
{
    internal class HostMigrator : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private VarRegistry _registry;
        private readonly EnvelopeBuilder _builder;

        internal HostMigrator(VarRegistry registry, EnvelopeBuilder builder)
        {
            _registry = registry;
            _builder = builder;
        }

        public void Subscribe(PresenceTracker presenceTracker, HostTracker hostTracker)
        {
            hostTracker.OnHostChanged += (evt) =>
            {
                if (evt.NewHost == presenceTracker.GetSelf())
                {
                    // pick up where the old host left off in terms of validating values.
                    Migrate(_registry);
                }
            };
        }

        private void Migrate(VarRegistry registry)
        {
            ValidatePendingVars<bool>(registry.SharedBools, env => env.SharedBoolAcks);
            ValidatePendingVars<float>(registry.SharedFloats, env => env.SharedFloatAcks);
            ValidatePendingVars<int>(registry.SharedInts, env => env.SharedIntAcks);
            ValidatePendingVars<string>(registry.SharedStrings, env => env.SharedStringAcks);

            ValidatePendingVars<bool>(registry.PresenceBools, env => env.PresenceBoolAcks);
            ValidatePendingVars<float>(registry.PresenceFloats, env => env.PresenceFloatAcks);
            ValidatePendingVars<int>(registry.PresenceInts, env => env.PresenceIntAcks);
            ValidatePendingVars<string>(registry.PresenceStrings, env => env.PresenceStringAcks);

            _builder.SendEnvelope();
        }

        private void ValidatePendingVars<T>(Dictionary<string, SharedVar<T>> vars, AckAccessor ackAccessor)
        {
            foreach (var kvp in vars)
            {
                _builder.AddAck(ackAccessor, kvp.Key);
            }
        }

        private void ValidatePendingVars<T>(Dictionary<string, PresenceVar<T>> vars, AckAccessor ackAccessor)
        {
            foreach (var kvp in vars)
            {
                _builder.AddAck(ackAccessor, kvp.Key);
            }
        }
    }
}