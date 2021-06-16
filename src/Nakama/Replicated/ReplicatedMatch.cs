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
using Nakama.TinyJson;

namespace Nakama.Replicated
{
    // todo entire concurrency pass on all this
    // TODO what if someone changes the type collection that the key is in, will try to send to the incorrect type
    // between clients and may pass handshake.
    // removed client -- flush values from Replicated<T> after some time.
    // catch all exceptions and route them through the OnError event?
    // todo protobuf support when that is merged.
    // todo think about allowing user to not stomp socket events if they so choose, or to sequence as they see fit.
    // you will need to not pass in the socket in order to do this.
    public class ReplicatedMatch
    {
        public event Action<Exception> OnError;

        private readonly int _dataOpcode;
        private readonly int _handshakeOpcode;
        private readonly IMatch _match;
        private readonly ReplicatedPresenceTracker _presenceTracker;
        private readonly ISocket _socket;
        private readonly ReplicatedValueStore _valuesToAll = new ReplicatedValueStore();
        private readonly ReplicatedVarStore _varStore;

        public ReplicatedMatch(ISocket socket, IMatch match, IUserPresence self, int dataOpcode, int handshakeOpcode)
        {
            _socket = socket;

            if (match == null)
            {
                throw new NullReferenceException("Null match passed to replicated match.");
            }

            if (match.Authoritative)
            {
                throw new ArgumentException("Replicated match cannot be authoritative.");
            }

            if (dataOpcode == handshakeOpcode)
            {
                throw new ArgumentException("Data opcode and handshake opcode must be different values.");
            }

            _dataOpcode = dataOpcode;
            _handshakeOpcode = handshakeOpcode;
            _match = match;

            _presenceTracker = new ReplicatedPresenceTracker(self, _valuesToAll, _varStore);
            _presenceTracker.OnReplicatedGuestJoined += HandleGuestJoined;
            _presenceTracker.OnReplicatedGuestLeft += HandleGuestLeft;
            _presenceTracker.OnReplicatedHostChanged += HandleHostChanged;

            _varStore = new ReplicatedVarStore(self);
        }

        public void RegisterBool(string id, ReplicatedVar<bool> replicatedBool)
        {
            var key = new ReplicatedKey(id, _presenceTracker.Self.Presence.UserId);
            _varStore.RegisterBool(key, replicatedBool);
            replicatedBool.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.Self.HandleLocalDataChanged<bool>(key, newValue, (store, val) => store.AddBool(val));
        }

        public void RegisterFloat(string id, ReplicatedVar<float> replicatedFloat)
        {
            var key = new ReplicatedKey(id, _presenceTracker.Self.Presence.UserId);
            _varStore.RegisterFloat(key, replicatedFloat);
            replicatedFloat.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.Self.HandleLocalDataChanged<float>(key, newValue, (store, val) => store.AddFloat(val));
        }

        public void RegisterInt(string id, ReplicatedVar<int> replicatedInt)
        {
            var key = new ReplicatedKey(id, _presenceTracker.Self.Presence.UserId);
            _varStore.RegisterInt(key, replicatedInt);
            replicatedInt.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.Self.HandleLocalDataChanged<int>(key, newValue, (store, val) => store.AddInt(val));
        }

        public void RegisterString(string id, ReplicatedVar<string> replicatedString)
        {
            var key = new ReplicatedKey(id, _presenceTracker.Self.Presence.UserId);
            _varStore.RegisterString(key, replicatedString);
            replicatedString.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.Self.HandleLocalDataChanged<string>(key, newValue, (store, val) => store.AddString(val));
        }

        private void HandleGuestJoined(ReplicatedGuest joinedGuest)
        {
            joinedGuest.OnHandshakeRequestSend += HandleHandshakeRequestSend;
            joinedGuest.OnReplicatedDataSend += HandleReplicatedDataSend;
        }

        private void HandleGuestLeft(ReplicatedGuest leftGuest)
        {
            leftGuest.OnHandshakeRequestSend -= HandleHandshakeRequestSend;
            leftGuest.OnReplicatedDataSend -= HandleReplicatedDataSend;
        }

        private void HandleHostChanged(ReplicatedHost oldHost, ReplicatedHost newHost)
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
            _socket.SendMatchStateAsync(_match.Id, _dataOpcode, Encode(request), new IUserPresence[]{_presenceTracker.Host.Presence});
        }

        private void HandleHandshakeResponseSend(IUserPresence target, HandshakeResponse response)
        {
            _socket.SendMatchStateAsync(_match.Id, _dataOpcode, Encode(response), new IUserPresence[]{target});
        }

        private void HandleReplicatedDataSend(IEnumerable<IUserPresence> targets, ReplicatedValueStore values)
        {
            _socket.SendMatchStateAsync(_match.Id, _dataOpcode, Encode(values), targets);
        }

        private void HandleReceivedMatchPresence(IMatchPresenceEvent e)
        {
            if (e.MatchId != _match.Id)
            {
                throw new ArgumentException($"Received presence for unexpected match: {e.MatchId}");
            }

            _presenceTracker.HandlePresenceEvent(e);
        }

        private void HandleReceivedMatchState(IMatchState matchState)
        {
            if (matchState.OpCode == _dataOpcode)
            {
                ReplicatedValueStore incomingStore = JsonParser.FromJson<ReplicatedValueStore>(System.Text.Encoding.UTF8.GetString(matchState.State));
                _presenceTracker.Self.HandleRemoteDataChanged(matchState.UserPresence, incomingStore);
            }
            else if (matchState.OpCode == _handshakeOpcode)
            {
                if (_presenceTracker.Self is ReplicatedHost hostSelf)
                {
                    string json = System.Text.Encoding.UTF8.GetString(matchState.State);
                    var handshakeRequest = JsonParser.FromJson<HandshakeRequest>(json);
                    hostSelf.ReceivedHandshakeRequest(matchState.UserPresence, handshakeRequest);
                }
                else
                {
                    var guestSelf = _presenceTracker.Self as ReplicatedGuest;
                    string json = System.Text.Encoding.UTF8.GetString(matchState.State);
                    var handshakeRepsonse = JsonParser.FromJson<HandshakeResponse>(json);
                    guestSelf.ReceivedHandshakeResponse(handshakeRepsonse);
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
