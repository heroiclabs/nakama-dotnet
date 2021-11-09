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
using System.Collections.Generic;
using System.Linq;
using Nakama;

namespace NakamaSync
{
    // todo should we await on sending on handshake
    internal class HandshakeRequester<T>
    {
        public ILogger Logger { get; set; }

        public event Action OnHandshakeSuccess;
        public event Action<IUserPresence> OnHandshakeFailure;

        private SyncSocket<T> _socket;
        private readonly VarIngress<T> _varGuestIngress;

        private IEnumerable<string> _allKeys;

        public HandshakeRequester(IEnumerable<string> allKeys, VarIngress<T> varGuestIngress, SyncSocket<T> socket)
        {
            _allKeys = allKeys;
            _varGuestIngress = varGuestIngress;
            _socket = socket;
        }

        public void Subscribe()
        {
            Logger?.DebugFormat($"User subscribed to socket and presence tracker.");
            _socket.OnHandshakeResponse += HandleHandshakeResponse;
        }

        public void ReceiveMatch(IMatch match)
        {
            Logger?.DebugFormat($"Handshake requester received match.");

            if (match.Presences.Any())
            {
                RequestHandshake(_socket, match.Presences.First());
            }
        }

        private void HandleHandshakeResponse(IUserPresence source, HandshakeResponse<T> response)
        {
            // todo if no longer host at this point, then reroute the request to the new host.
            if (response.Success)
            {
                Logger?.InfoFormat("Received successful handshake response.");
                _varGuestIngress.ReceiveSyncEnvelope(source, response.Store, _socket);
                OnHandshakeSuccess();
            }
            else
            {
                OnHandshakeFailure(source);
            }
        }

        private void RequestHandshake(SyncSocket<T> socket, IUserPresence target)
        {
            var keysForValidation = _allKeys;
            socket.SendHandshakeRequest(new HandshakeRequest(keysForValidation.ToList()), target);
        }
    }
}
