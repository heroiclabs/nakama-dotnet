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
                ReceiveSyncEnvelope(source, envelope, presenceTracker.IsSelfHost());
            };
        }

        public void ReceiveSyncEnvelope(IUserPresence source, Envelope envelope, bool isHost)
        {
            var bools = SharedContext.FromBoolValues(envelope, _registry);
            ReceiveSyncEnvelope(source, bools, isHost);

            var floats = SharedContext.FromFloatValues(envelope, _registry);
            ReceiveSyncEnvelope(source, floats, isHost);

            var ints = SharedContext.FromIntValues(envelope, _registry);
            ReceiveSyncEnvelope(source, ints, isHost);

            var strings = SharedContext.FromStringValues(envelope, _registry);
            ReceiveSyncEnvelope(source, strings, isHost);
        }

        private void ReceiveSyncEnvelope<T>(IUserPresence source, List<SharedContext<T>> contexts, bool isHost)
        {
            foreach (SharedContext<T> context in contexts)
            {
                if (isHost)
                {
                    _sharedHostIngress.ProcessValue(source, context);
                }
                else
                {
                    _guestIngress.ProcessValue(context.Var, source, context.Value);
                }
            }
        }
    }
}
