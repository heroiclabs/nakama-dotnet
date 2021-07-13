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
        public delegate void SyncEnvelopeHandler(IUserPresence source, Envelope envelope);
        public delegate void HandshakeRequestHandler(IUserPresence source, HandshakeRequest request);
        public delegate void HandshakeResponseHandler(IUserPresence source, HandshakeResponse response);

        public event SyncEnvelopeHandler OnSyncEnvelope;
        public event HandshakeRequestHandler OnHandshakeRequest;
        public event HandshakeResponseHandler OnHandshakeResponse;

        private readonly SyncEncoding _encoding = new SyncEncoding();
        private readonly ISocket _socket;
        private readonly SyncOpcodes _opcodes;
        private readonly RolePresenceTracker _presenceTracker;
        private IMatch _match;

        public SyncSocket(ISocket socket, SyncOpcodes opcodes, RolePresenceTracker presenceTracker)
        {
            _socket = socket;
            _opcodes = opcodes;
            _presenceTracker = presenceTracker;
            _socket.ReceivedMatchState += HandleReceivedMatchState;
        }

        public void ReceiveMatch(IMatch match)
        {
            _match = match;
        }

        public void SendHandshakeRequest(HandshakeRequest request)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.HandshakeRequestOpcode, _encoding.Encode(request), new IUserPresence[]{_presenceTracker.GetHost()});
        }

        public void SendHandshakeResponse(IUserPresence target, HandshakeResponse response)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.HandshakeResponseOpcode, _encoding.Encode(response), new IUserPresence[]{target});
        }

        public void SendSyncData(IUserPresence target, Envelope envelope)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.DataOpcode, _encoding.Encode(envelope), new IUserPresence[]{target});
        }

        public void SendSyncDataToAll(Envelope envelope)
        {
            // clear envelope for each of these after sending?
            _socket.SendMatchStateAsync(_match.Id, _opcodes.DataOpcode, _encoding.Encode(envelope));
        }

        public void SendSyncDataToHost(Envelope envelope)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.DataOpcode, _encoding.Encode(envelope), new IUserPresence[]{_presenceTracker.GetHost()});
        }

        private void HandleReceivedMatchState(IMatchState state)
        {
            if (state.OpCode == _opcodes.DataOpcode)
            {
                Envelope envelope = _encoding.Decode<Envelope>(state.State);
                OnSyncEnvelope(state.UserPresence, envelope);
            }
            else if (state.OpCode == _opcodes.HandshakeRequestOpcode)
            {
                OnHandshakeRequest?.Invoke(state.UserPresence, _encoding.Decode<HandshakeRequest>(state.State));
            }
            else if (state.OpCode == _opcodes.HandshakeResponseOpcode)
            {
                OnHandshakeResponse?.Invoke(state.UserPresence, _encoding.Decode<HandshakeResponse>(state.State));
            }
        }
    }
}
