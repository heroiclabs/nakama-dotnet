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
    // todo replicated composite object
    // todo replicated list
    // todo potential race when creating and joining a match between the construction of this object
    // and the dispatching of presence objects off the socket.
    public class ReplicatedMatch : IMatch
    {
        public event Action<Exception> OnError;

        public bool Authoritative => _match.Authoritative;
        public string Id => _match.Id;
        public string Label => _match.Label;
        public IEnumerable<IUserPresence> Presences => _match.Presences;
        public int Size => _match.Size;
        public IUserPresence Self => _match.Self;

        private readonly ReplicatedOpcodes _opcodes;
        private readonly IMatch _match;
        private readonly ReplicatedPresenceTracker _presenceTracker;
        private readonly ISocket _socket;
        private readonly ReplicatedValueStore _valuesToAll = new ReplicatedValueStore();
        private readonly ReplicatedVarStore _varStore;

        internal ReplicatedMatch(ISocket socket, IMatch match, ReplicatedOpcodes opcodes, ReplicatedVarStore varStore, ReplicatedPresenceTracker presenceTracker)
        {
            _socket = socket;
            _opcodes = opcodes;
            _match = match;
            _varStore = varStore;
            _presenceTracker = presenceTracker;
            _presenceTracker.OnReplicatedGuestJoined += HandleGuestJoined;
            _presenceTracker.OnReplicatedGuestLeft += HandleGuestLeft;
            _presenceTracker.OnReplicatedHostChanged += HandleHostChanged;
        }

        public void HandleReceivedMatchPresence(IMatchPresenceEvent e)
        {
            if (e.MatchId != _match.Id)
            {
                throw new ArgumentException($"Received presence for unexpected match: {e.MatchId}");
            }

            _presenceTracker.HandlePresenceEvent(e);
        }

        public void RegisterBool(string id, ReplicatedVar<bool> replicatedBool)
        {
            var key = new ReplicatedKey(id, _match.Self.UserId);
            _varStore.RegisterBool(key, replicatedBool);
            replicatedBool.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.GetSelf().HandleLocalDataChanged<bool>(key, newValue, (store, val) => store.AddBool(val));
        }

        public void RegisterFloat(string id, ReplicatedVar<float> replicatedFloat)
        {
            var key = new ReplicatedKey(id, _match.Self.UserId);
            _varStore.RegisterFloat(key, replicatedFloat);
            replicatedFloat.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.GetSelf().HandleLocalDataChanged<float>(key, newValue, (store, val) => store.AddFloat(val));
        }

        public void RegisterInt(string id, ReplicatedVar<int> replicatedInt)
        {
            var key = new ReplicatedKey(id, _match.Self.UserId);
            _varStore.RegisterInt(key, replicatedInt);
            replicatedInt.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.GetSelf().HandleLocalDataChanged<int>(key, newValue, (store, val) => store.AddInt(val));
        }

        public void RegisterString(string id, ReplicatedVar<string> replicatedString)
        {
            var key = new ReplicatedKey(id, _match.Self.UserId);
            _varStore.RegisterString(key, replicatedString);
            replicatedString.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.GetSelf().HandleLocalDataChanged<string>(key, newValue, (store, val) => store.AddString(val));
        }

        private void HandleGuestJoined(ReplicatedGuest joinedGuest)
        {
            joinedGuest.OnReplicatedDataSend += HandleReplicatedDataSend;

            var keysForValidation = _varStore.GetAllKeysAsList();
            _socket.SendMatchStateAsync(
                _match.Id,
                _opcodes.HandshakeOpcode,
                Encode(new HandshakeRequest(keysForValidation)),
                new IUserPresence[]{_presenceTracker.Host.Presence});
        }

        private void HandleGuestLeft(ReplicatedGuest leftGuest)
        {
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
            _socket.SendMatchStateAsync(_match.Id, _opcodes.HandshakeOpcode, Encode(request), new IUserPresence[]{_presenceTracker.Host.Presence});
        }

        private void HandleHandshakeResponseSend(IUserPresence target, HandshakeResponse response)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.ReplicatedDataOpcode, Encode(response), new IUserPresence[]{target});
        }

        private void HandleReplicatedDataSend(IEnumerable<IUserPresence> targets, ReplicatedValueStore values)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.ReplicatedDataOpcode, Encode(values), targets);
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
