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
using Nakama;

namespace NakamaSync
{
    internal class SyncSocket
    {
        public delegate void SyncDataHandler(IUserPresence source, SyncValues values);
        public delegate void HandshakeRequestHandler(IUserPresence source, HandshakeRequest request);
        public delegate void HandshakeResponseHandler(IUserPresence source, HandshakeResponse response);

        public event SyncDataHandler OnSyncData;
        public event HandshakeRequestHandler OnHandshakeRequest;
        public event HandshakeResponseHandler OnHandshakeResponse;

        private readonly SyncEncoding _encoding = new SyncEncoding();
        private readonly ISocket _socket;
        private readonly IMatch _match;
        private readonly SyncOpcodes _opcodes;
        private readonly PresenceTracker _presenceTracker;

        public SyncSocket(ISocket socket, IMatch match, SyncOpcodes opcodes, PresenceTracker presenceTracker)
        {
            _socket = socket;
            _match = match;
            _opcodes = opcodes;
            _presenceTracker = presenceTracker;

            _socket.ReceivedMatchState += HandleReceivedMatchState;
        }

        public void SendHandshakeResponse(IUserPresence target, HandshakeResponse response)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.HandshakeOpcode, _encoding.Encode(response), new IUserPresence[]{target});
        }

        public void SendHandshakeRequest(HandshakeRequest request)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.HandshakeOpcode, _encoding.Encode(request), new IUserPresence[]{_presenceTracker.GetHost()});
        }

        public void SendSyncData(IUserPresence target, SyncValues values)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.DataOpcode, _encoding.Encode(values), new IUserPresence[]{target});
        }

        public void SendSyncDataToAll(SyncValues values)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.DataOpcode, _encoding.Encode(values));
        }

        public void SendSyncDataToHost(SyncValues values)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.DataOpcode, _encoding.Encode(values), new IUserPresence[]{_presenceTracker.GetHost()});
        }

        private void HandleReceivedMatchState(IMatchState state)
        {
            if (state.OpCode == _opcodes.DataOpcode)
            {
                SyncValues values = _encoding.Decode<SyncValues>(state.State);
                OnSyncData(state.UserPresence, values);
            }
            else if (state.OpCode == _opcodes.HandshakeOpcode)
            {
                if (_presenceTracker.IsSelfHost())
                {
                    OnHandshakeRequest?.Invoke(state.UserPresence, _encoding.Decode<HandshakeRequest>(state.State));
                }
                else
                {
                    OnHandshakeResponse?.Invoke(state.UserPresence, _encoding.Decode<HandshakeResponse>(state.State));
                }
            }
        }
    }
}
