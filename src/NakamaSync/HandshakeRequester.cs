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
using System.Linq;
using Nakama;

namespace NakamaSync
{
    // todo should we await on sending on handshake
    internal class HandshakeRequester
    {
        private readonly VarKeys _keys;
        private readonly Ingresses _ingresses;

        // todo handle error with sending handshake and resend if needed
        private bool _sentHandshake;

        public HandshakeRequester(VarKeys keys, Ingresses ingresses)
        {
            _keys = keys;
            _ingresses = ingresses;
        }

        public void Subscribe(SyncSocket socket, RolePresenceTracker presenceTracker)
        {
            presenceTracker.OnPresenceAdded += (presence) =>
            {
                if (_sentHandshake)
                {
                    return;
                }

                RequestHandshake(presence, socket);
            };

            socket.OnHandshakeResponse += (source, response) =>
            {
                HandleHandshakeResponse(source, response, presenceTracker.IsSelfHost());
            };
        }

        private void HandleHandshakeResponse(IUserPresence source, HandshakeResponse response, bool isHost)
        {
            if (response.Success)
            {
                _ingresses.SharedRoleIngress.ReceiveSyncEnvelope(source, response.Store, isHost);
                _ingresses.UserRoleIngress.ReceiveSyncEnvelope(source, response.Store, isHost);
            }
            else
            {
                throw new InvalidOperationException("Synced match handshake with host failed.");
            }
        }

        private void RequestHandshake(IUserPresence self, SyncSocket socket)
        {
            var keysForValidation = _keys.GetKeys();
            socket.SendHandshakeRequest(new HandshakeRequest(keysForValidation.ToList()));
            _sentHandshake = true;
        }
    }
}
