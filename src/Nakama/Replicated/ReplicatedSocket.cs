

using System.Collections.Generic;
using Nakama.TinyJson;
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
namespace Nakama.Replicated
{
    internal class ReplicatedSocket
    {
        private readonly string _matchId;
        private readonly ReplicatedOpcodes _opcodes;
        private readonly ReplicatedPresenceTracker _presenceTracker;
        private readonly ISocket _socket;
        private readonly ReplicatedVarStore _varStore;

        internal ReplicatedSocket(string matchId, ReplicatedOpcodes opcodes, ReplicatedPresenceTracker presenceTracker, ISocket socket, ReplicatedVarStore varStore)
        {
            _matchId = matchId;
            _opcodes = opcodes;
            _presenceTracker = presenceTracker;
            _socket = socket;
            _varStore = varStore;
        }

        public void HandleGuestJoined(ReplicatedGuest joinedGuest)
        {
            joinedGuest.OnReplicatedDataSend += HandleReplicatedDataSend;

            var keysForValidation = _varStore.GetAllKeysAsList();
            _socket.SendMatchStateAsync(
                _matchId,
                _opcodes.HandshakeOpcode,
                Encode(new HandshakeRequest(keysForValidation)),
                new IUserPresence[]{_presenceTracker.Host.Presence});
        }

        public void HandleGuestLeft(ReplicatedGuest leftGuest)
        {
            leftGuest.OnReplicatedDataSend -= HandleReplicatedDataSend;
        }

        public void HandleHostChanged(ReplicatedHost oldHost, ReplicatedHost newHost)
        {
            if (oldHost != null)
            {
                oldHost.OnHandshakeResponseSend -= HandleHandshakeResponseSend;
                oldHost.OnReplicatedDataSend -= HandleReplicatedDataSend;
            }

            if (newHost != null)
            {
                newHost.OnHandshakeResponseSend += HandleHandshakeResponseSend;
                newHost.OnReplicatedDataSend += HandleReplicatedDataSend;
            }
        }

        private void HandleHandshakeRequestSend(HandshakeRequest request)
        {
            _socket.SendMatchStateAsync(_matchId, _opcodes.HandshakeOpcode, Encode(request), new IUserPresence[]{_presenceTracker.Host.Presence});
        }

        private void HandleHandshakeResponseSend(IUserPresence target, HandshakeResponse response)
        {
            _socket.SendMatchStateAsync(_matchId, _opcodes.ReplicatedDataOpcode, Encode(response), new IUserPresence[]{target});
        }

        private void HandleReplicatedDataSend(IEnumerable<IUserPresence> targets, ReplicatedValueStore values)
        {
            _socket.SendMatchStateAsync(_matchId, _opcodes.ReplicatedDataOpcode, Encode(values), targets);
        }

        private void HandleReceivedMatchState(IMatchState matchState)
        {
            if (matchState.OpCode == _opcodes.ReplicatedDataOpcode)
            {
                ReplicatedValueStore incomingStore = JsonParser.FromJson<ReplicatedValueStore>(System.Text.Encoding.UTF8.GetString(matchState.State));
                _presenceTracker.GetSelf().HandleRemoteDataChanged(matchState.UserPresence, incomingStore);
            }
            else if (matchState.OpCode == _opcodes.HandshakeOpcode)
            {
                if (_presenceTracker.GetSelf() is ReplicatedHost hostSelf)
                {
                    string json = System.Text.Encoding.UTF8.GetString(matchState.State);
                    var handshakeRequest = JsonParser.FromJson<HandshakeRequest>(json);
                    hostSelf.ReceivedHandshakeRequest(matchState.UserPresence, handshakeRequest);
                }
                else
                {
                    var guestSelf = _presenceTracker.GetSelf() as ReplicatedGuest;
                    string json = System.Text.Encoding.UTF8.GetString(matchState.State);
                    var handshakeResponse = JsonParser.FromJson<HandshakeResponse>(json);
                    guestSelf.ReceivedHandshakeResponse(handshakeResponse);
                }
            }
        }

        private byte[] Encode(object data)
        {
            return System.Text.Encoding.UTF8.GetBytes(data.ToJson());
        }

        private T Decode<T>(byte[] data)
        {
            return TinyJson.JsonParser.FromJson<T>(System.Text.Encoding.UTF8.GetString(data));
        }
    }
}