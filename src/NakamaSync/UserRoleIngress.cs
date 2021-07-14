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
    internal class UserRoleIngress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly UserGuestIngress _userGuestIngress;
        private readonly UserHostIngress _userHostIngress;
        private readonly VarRegistry _registry;

        public UserRoleIngress(UserGuestIngress userGuestIngress, UserHostIngress userHostIngress, VarRegistry registry)
        {
            _userGuestIngress = userGuestIngress;
            _userHostIngress = userHostIngress;
            _registry = registry;
        }

        public void Subscribe(SyncSocket socket, RoleTracker presenceTracker)
        {
            socket.OnSyncEnvelope += (source, envelope) =>
            {
                ReceiveSyncEnvelope(source, envelope, presenceTracker.IsSelfHost());
            };
        }

        public void ReceiveSyncEnvelope(IUserPresence source, Envelope envelope, bool isHost)
        {
            var bools = UserIngressContext.FromBoolValues(envelope, _registry);
            HandleSyncEnvelope(source, bools, isHost);

            var floats = UserIngressContext.FromFloatValues(envelope, _registry);
            HandleSyncEnvelope(source, floats, isHost);

            var ints = UserIngressContext.FromIntValues(envelope, _registry);
            HandleSyncEnvelope(source, ints, isHost);

            var strings = UserIngressContext.FromStringValues(envelope, _registry);
            HandleSyncEnvelope(source, strings, isHost);
        }

        private void HandleSyncEnvelope<T>(IUserPresence source, List<UserIngressContext<T>> contexts, bool isHost)
        {
            foreach (UserIngressContext<T> context in contexts)
            {
                if (isHost)
                {
                    _userHostIngress.HandleValue(source, context);
                }
                else
                {
                    _userGuestIngress.HandleValue(context.Var, source, context.Value);
                }
            }
        }
    }
}
