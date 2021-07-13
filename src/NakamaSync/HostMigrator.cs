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

using System.Collections.Generic;

namespace NakamaSync
{
    internal class HostMigrator
    {
        private VarRegistry _registry;
        private readonly EnvelopeBuilder _builder;

        internal HostMigrator(VarRegistry registry, EnvelopeBuilder builder)
        {
            _registry = registry;
            _builder = builder;
        }

        public void Subscribe(RolePresenceTracker presenceTracker)
        {
            presenceTracker.OnHostChanged += (evt) =>
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

            ValidatePendingVars<bool>(registry.UserBools, env => env.UserBoolAcks);
            ValidatePendingVars<float>(registry.UserFloats, env => env.UserFloatAcks);
            ValidatePendingVars<int>(registry.UserInts, env => env.UserIntAcks);
            ValidatePendingVars<string>(registry.UserStrings, env => env.UserStringAcks);

            _builder.SendEnvelope();
        }

        private void ValidatePendingVars<T>(Dictionary<string, SharedVar<T>> vars, AckAccessor ackAccessor)
        {
            foreach (var kvp in vars)
            {
                _builder.AddAck(ackAccessor, kvp.Key);
            }
        }

        private void ValidatePendingVars<T>(Dictionary<string, UserVar<T>> vars, AckAccessor ackAccessor)
        {
            foreach (var kvp in vars)
            {
                _builder.AddAck(ackAccessor, kvp.Key);
            }
        }
    }
}