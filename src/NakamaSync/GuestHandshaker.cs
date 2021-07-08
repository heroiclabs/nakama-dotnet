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
using System.Threading;
using System.Threading.Tasks;
using Nakama;

namespace NakamaSync
{
    internal class GuestHandshaker
    {
        private SyncVarKeys _keys;
        private RoleIngress _ingress;
        private PresenceTracker _presenceTracker;

        private TaskCompletionSource<object> _handshakeTcs;

        public GuestHandshaker(SyncVarKeys keys, RoleIngress ingress, PresenceTracker presenceTracker)
        {
            _keys = keys;
            _ingress = ingress;
            _presenceTracker = presenceTracker;
        }

        public Task DoHandshake(SyncSocket socket)
        {
            if (_handshakeTcs != null)
            {
                throw new InvalidOperationException("Guest has already requested a handhake.");
            }

            _handshakeTcs = new TaskCompletionSource<object>();
            _presenceTracker.OnGuestJoined += (guest) => HandleGuestJoined(guest, socket);
            socket.OnHandshakeResponse += (source, response) => HandleHandshakeResponse(source, response, socket);
            return _handshakeTcs.Task;
        }

        private void HandleHandshakeResponse(IUserPresence source, HandshakeResponse response, SyncSocket socket)
        {
            if (response.Success)
            {
                _ingress.HandleSyncData(source, response.Store);
                _handshakeTcs.TrySetResult(null);
            }
            else
            {
                _handshakeTcs?.TrySetException(new InvalidOperationException("Synced match handshake with host failed."));
            }
        }

        private void HandleGuestJoined(IUserPresence joinedGuest, SyncSocket socket)
        {
            var keysForValidation = _keys.GetKeys();
            socket.SendHandshakeRequest(new HandshakeRequest(keysForValidation.ToList()));
        }
    }
}
