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
        public event Action<IUserPresence> OnHandshakeFailure;

        private readonly VarRegistry _registry;

        private PresenceTracker _presenceTracker;
        // todo handle error with sending handshake and resend if needed
        private bool _sentHandshake;

        private readonly SharedVarIngress _sharedVarGuestIngress;
        private readonly PresenceVarIngress _presenceRoleIngress;
        private readonly SyncSocket _socket;
        private readonly string _userId;

        public HandshakeRequester(VarRegistry registry, PresenceTracker presenceTracker, SyncSocket socket, SharedVarIngress sharedVarGuestIngress, PresenceVarIngress presenceRoleIngress, string userId)
        {
            _registry = registry;
            _presenceTracker = presenceTracker;
            _socket = socket;
            _sharedVarGuestIngress = sharedVarGuestIngress;
            _presenceRoleIngress = presenceRoleIngress;
            _userId = userId;
        }

        public void Subscribe(HostTracker hostTracker)
        {
            _presenceTracker.OnPresenceAdded += (presence) =>
            {
                Logger?.DebugFormat($"Handshake requester for {_userId} saw presence added: {presence.UserId}");

                if (_sentHandshake)
                {
                    Logger?.DebugFormat($"Handshake requester for {_userId} already sent handshake.");
                    return;
                }

                if (presence.UserId != _userId)
                {
                    RequestHandshake(_socket, presence);
                }

                Logger?.DebugFormat($"Handshake requester done seeing presence added.");
            };

            _socket.OnHandshakeResponse += (source, response) =>
            {
                HandleHandshakeResponse(source, response, hostTracker.IsSelfHost());
            };

            Logger?.DebugFormat($"User {_presenceTracker.UserId} subscribed to socket and presence tracker.");
        }

        public void ReceiveMatch(IMatch match)
        {
            Logger?.DebugFormat($"Handshake requester for {_userId} received match.");

            if (_sentHandshake)
            {
                Logger?.DebugFormat($"Handshake requester for {_userId} already sent handshake.");
                return;
            }

            // todo randomize?
            var otherUser = _presenceTracker.GetSortedOthers().FirstOrDefault();

            if (!_sentHandshake && otherUser != null)
            {
                RequestHandshake(_socket, otherUser);
            }

            Logger?.DebugFormat($"Handshake requeuster done receiving match.");

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
                OnHandshakeFailure(source);
            }
        }

        private void RequestHandshake(SyncSocket socket, IUserPresence target)
        {
            var keysForValidation = _registry.GetAllKeys();
            socket.SendHandshakeRequest(new HandshakeRequest(keysForValidation.ToList()), target);
            _sentHandshake = true;
        }
    }
}
