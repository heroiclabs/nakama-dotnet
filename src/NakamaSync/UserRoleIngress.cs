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
    internal class UserRoleIngress
    {
        private readonly GuestIngress _guestIngress;
        private readonly UserHostIngress _userHostIngress;
        private readonly SyncVarRegistry _registry;

        public UserRoleIngress(GuestIngress guestIngress, UserHostIngress userHostIngress, SyncVarRegistry registry)
        {
            _guestIngress = guestIngress;
            _userHostIngress = userHostIngress;
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
            var bools = UserContext<bool>.Create(_registry.UserBools, envelope.UserBools, env => env.UserBools, env => env.UserBoolAcks);
            HandleSyncEnvelope(source, bools, isHost);

            var floats = UserContext<float>.Create(_registry.UserFloats, envelope.UserFloats, env => env.UserFloats, env => env.UserFloatAcks);
            HandleSyncEnvelope(source, floats, isHost);

            var ints = UserContext<int>.Create(_registry.UserInts, envelope.UserInts, env => env.UserInts, env => env.UserFloatAcks);
            HandleSyncEnvelope(source, ints, isHost);

            var strings = UserContext<string>.Create(_registry.UserStrings, envelope.UserStrings, env => env.UserStrings, env => env.UserStringAcks);
            HandleSyncEnvelope(source, strings, isHost);
        }

        private void HandleSyncEnvelope<T>(IUserPresence source, List<UserContext<T>> contexts, bool isHost)
        {
            foreach (UserContext<T> context in contexts)
            {
                if (isHost)
                {
                    _userHostIngress.HandleValue(source, context);
                }
                else
                {
                    _guestIngress.HandleValue(context.Var, source, context.Value);
                }
            }
        }
    }
}
