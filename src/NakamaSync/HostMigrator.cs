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
                var self =  presenceTracker.GetSelf();
                if (evt.NewHost?.UserId == self.UserId)
                {
                    // pick up where the old host left off in terms of validating values.
                    ValidatePendingVars(_registry);
                    UpdateVarHost(_registry, true);
                }
                else if (evt.OldHost?.UserId == self.UserId)
                {
                    UpdateVarHost(_registry, false);
                }
            };
        }

        private void ValidatePendingVars(VarRegistry registry)
        {
            ValidatePendingVars<bool>(registry.SharedVarRegistry.SharedBools, env => env.SharedBoolAcks);
            ValidatePendingVars<float>(registry.SharedVarRegistry.SharedFloats, env => env.SharedFloatAcks);
            ValidatePendingVars<int>(registry.SharedVarRegistry.SharedInts, env => env.SharedIntAcks);
            ValidatePendingVars<string>(registry.SharedVarRegistry.SharedStrings, env => env.SharedStringAcks);

            ValidatePendingVars<bool>(registry.PresenceVarRegistry.PresenceBools, env => env.PresenceBoolAcks);
            ValidatePendingVars<float>(registry.PresenceVarRegistry.PresenceFloats, env => env.PresenceFloatAcks);
            ValidatePendingVars<int>(registry.PresenceVarRegistry.PresenceInts, env => env.PresenceIntAcks);
            ValidatePendingVars<string>(registry.PresenceVarRegistry.PresenceStrings, env => env.PresenceStringAcks);

            _builder.SendEnvelope();
        }

        private void ValidatePendingVars<T>(Dictionary<string, SharedVar<T>> vars, AckAccessor ackAccessor)
        {
            foreach (var kvp in vars)
            {
                _builder.AddAck(ackAccessor, kvp.Key);
            }
        }

        private void ValidatePendingVars<T>(Dictionary<string, PresenceVarCollection<T>> vars, AckAccessor ackAccessor)
        {
            // TODO validate each var individually.
            foreach (var kvp in vars)
            {
                _builder.AddAck(ackAccessor, kvp.Key);
            }
        }

        private void UpdateVarHost(VarRegistry varRegistry, bool isHost)
        {
            UpdateVarHost(varRegistry.SharedVarRegistry.SharedBools, isHost);
            UpdateVarHost(varRegistry.SharedVarRegistry.SharedFloats, isHost);
            UpdateVarHost(varRegistry.SharedVarRegistry.SharedInts, isHost);
            UpdateVarHost(varRegistry.SharedVarRegistry.SharedStrings, isHost);
            UpdateVarHost(varRegistry.PresenceVarRegistry.PresenceBools, isHost);
            UpdateVarHost(varRegistry.PresenceVarRegistry.PresenceFloats, isHost);
            UpdateVarHost(varRegistry.PresenceVarRegistry.PresenceInts, isHost);
            UpdateVarHost(varRegistry.PresenceVarRegistry.PresenceStrings, isHost);
        }

        private void UpdateVarHost<T>(Dictionary<string, SharedVar<T>> vars, bool isHost)
        {
            foreach (var var in vars.Values)
            {
                var.IsHost = isHost;
            }
        }

        private void UpdateVarHost<T>(Dictionary<string, PresenceVarCollection<T>> vars, bool isHost)
        {
            foreach (var var in vars.Values)
            {
                var.SelfVar.IsHost = isHost;

                foreach (var presenceVar in var.PresenceVars)
                {
                    presenceVar.IsHost = isHost;
                }
            }
        }
    }
}
