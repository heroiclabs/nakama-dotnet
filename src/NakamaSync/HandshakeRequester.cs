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
    internal class HandshakeRequester : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        public event Action OnHandshakeSuccess;
        public event Action OnHandshakeFailure;

        private readonly VarKeys _keys;

        // todo handle error with sending handshake and resend if needed
        private bool _sentHandshake;

        private SharedVarIngress _sharedVarGuestIngress;
        private PresenceRoleIngress _presenceRoleIngress;
        private string _userId;

        public HandshakeRequester(VarKeys keys, SharedVarIngress sharedVarGuestIngress, PresenceRoleIngress presenceRoleIngress, string userId)
        {
            _keys = keys;
            _sharedVarGuestIngress = sharedVarGuestIngress;
            _presenceRoleIngress = presenceRoleIngress;
            _userId = userId;
        }

        public void Subscribe(SyncSocket socket, HostTracker hostTracker, PresenceTracker presenceTracker)
        {
            presenceTracker.OnPresenceAdded += (presence) =>
            {
                if (_sentHandshake)
                {
                    return;
                }

                if (presence.UserId != _userId)
                {
                    RequestHandshake(presence, socket);
                }
            };

            socket.OnHandshakeResponse += (source, response) =>
            {
                HandleHandshakeResponse(source, response, hostTracker.IsSelfHost());
            };

            Logger?.DebugFormat($"User {presenceTracker.UserId} subscribed to socket and presence tracker.");
        }

        private void HandleHandshakeResponse(IUserPresence source, HandshakeResponse response, bool isHost)
        {
            if (response.Success)
            {
                Logger?.InfoFormat("Received successful handshake response.");
                _sharedVarGuestIngress.ReceiveSyncEnvelope(source, response.Store, isHost);
                _presenceRoleIngress.ReceiveSyncEnvelope(source, response.Store, isHost);
                OnHandshakeSuccess();
            }
            else
            {
                OnHandshakeFailure();
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
