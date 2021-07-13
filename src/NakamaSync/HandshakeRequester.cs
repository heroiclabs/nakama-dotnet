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
using System.Threading.Tasks;
using Nakama;

namespace NakamaSync
{
    internal class HandshakeRequester
    {
        private readonly VarKeys _keys;
        private readonly Ingresses _ingresses;
        private readonly TaskCompletionSource<object> _handshakeTcs;

        public HandshakeRequester(VarKeys keys, Ingresses ingresses)
        {
            _keys = keys;
            _ingresses = ingresses;
            _handshakeTcs = new TaskCompletionSource<object>();
        }

        public void Subscribe(SyncSocket socket, RolePresenceTracker presenceTracker)
        {
            if (presenceTracker.GetPresenceCount() <= 1)
            {
                // nobody to handshake with, don't request handshake
                _handshakeTcs.SetResult(null);
                return;
            }

            presenceTracker.OnGuestJoined += (guest) => RequestHandshake(guest, socket);
            socket.OnHandshakeResponse += (source, response) =>
            {
                HandleHandshakeResponse(source, response, presenceTracker.IsSelfHost());
            };

        }

        public Task WaitForResponse()
        {
            return _handshakeTcs.Task;
        }

        private void HandleHandshakeResponse(IUserPresence source, HandshakeResponse response, bool isHost)
        {
            if (response.Success)
            {
                _ingresses.SharedRoleIngress.ReceiveSyncEnvelope(source, response.Store, isHost);
                _ingresses.UserRoleIngress.ReceiveSyncEnvelope(source, response.Store, isHost);
                _handshakeTcs.TrySetResult(null);
            }
            else
            {
                _handshakeTcs.TrySetException(new InvalidOperationException("Synced match handshake with host failed."));
            }
        }

        private void RequestHandshake(IUserPresence self, SyncSocket socket)
        {
            var keysForValidation = _keys.GetKeys();
            socket.SendHandshakeRequest(new HandshakeRequest(keysForValidation.ToList()));
        }
    }
}
