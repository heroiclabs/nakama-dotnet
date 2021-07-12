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
using Nakama;

namespace NakamaSync
{
    internal class SharedRoleIngress
    {
        private readonly GuestIngress _guestIngress;
        private readonly SharedHostIngress _sharedHostIngress;
        private readonly SyncVarRegistry _registry;

        public SharedRoleIngress(GuestIngress guestIngress, SharedHostIngress sharedHostIngress, SyncVarRegistry registry)
        {
            _guestIngress = guestIngress;
            _sharedHostIngress = sharedHostIngress;
            _registry = registry;
        }

        public void Subscribe(SyncSocket socket, RolePresenceTracker presenceTracker)
        {
            socket.OnSyncEnvelope += (source, envelope) =>
            {
                HandleSyncEnvelope(source, envelope, presenceTracker.IsSelfHost());
            };
        }

        public void HandleSyncEnvelope(IUserPresence source, Envelope envelope, bool isHost)
        {
            var bools = SharedContext<bool>.Create(_registry.SharedBools, envelope.SharedBools, env => env.SharedBools, env => env.SharedBoolAcks);
            HandleSyncEnvelope(source, bools, isHost);

            var floats = SharedContext<float>.Create(_registry.SharedFloats, envelope.SharedFloats, env => env.SharedFloats, env => env.SharedFloatAcks);
            HandleSyncEnvelope(source, floats, isHost);

            var ints = SharedContext<int>.Create(_registry.SharedInts, envelope.SharedInts, env => env.SharedInts, env => env.SharedFloatAcks);
            HandleSyncEnvelope(source, ints, isHost);

            var strings = SharedContext<string>.Create(_registry.SharedStrings, envelope.SharedStrings, env => env.SharedStrings, env => env.SharedStringAcks);
            HandleSyncEnvelope(source, strings, isHost);
        }

        private void HandleSyncEnvelope<T>(IUserPresence source, List<SharedContext<T>> contexts, bool isHost)
        {
            foreach (SharedContext<T> context in contexts)
            {
                if (isHost)
                {
                    _sharedHostIngress.HandleValue(source, context);
                }
                else
                {
                    _guestIngress.HandleValue(context.Var, source, context.Value);
                }
            }
        }
    }
}
