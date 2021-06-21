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
using System.Threading.Tasks;
using Nakama;
using Nakama.TinyJson;

namespace NakamaSync
{
    // todo entire concurrency pass on all this
    // enforce key ids should be unique across types
    // TODO what if someone changes the type collection that the key is in, will try to send to the incorrect type
    // between clients and may pass handshake.
    // removed client -- flush values from Replicated<T> after some time.
    // catch all exceptions and route them through the OnError event?
    // todo protobuf support when that is merged.
    // todo think about allowing user to not stomp socket events if they so choose, or to sequence as they see fit.
    // you will need to not pass in the socket in order to do this.
    // todo synced composite object
    // todo synced list
    // todo potential race when creating and joining a match between the construction of this object
    // and the dispatching of presence objects off the socket.
    // TODO restore the default getvalue call with self
    // ~destructor definitely doesn't work, think about end match flow.
    // fix OnHostValdiate so that you have a better way of signalling intent that you want a var to be validated.
    // to string calls
    // expose interfaces, not concrete classes.
    // todo rename this class?
    // something is weird about guesthandler requiring self presence but presence tracker not. can we potentially just add self to presence tracker?
    // todo handle host changed
    // todo handle guest left
    public class SyncedMatch : IMatch
    {
        public event Action<Exception> OnError;

        public bool Authoritative => _match.Authoritative;
        public string Id => _match.Id;
        public string Label => _match.Label;
        public IEnumerable<IUserPresence> Presences => _match.Presences;
        public int Size => _match.Size;
        public IUserPresence Self => _match.Self;

        private readonly GuestHandler _guestHandler;
        private readonly HostHandler _hostHandler;
        private IMatch _match;
        private readonly SyncedOpcodes _opcodes;
        private readonly PresenceTracker _presenceTracker;
        private readonly ISession _session;
        private readonly ISocket _socket;
        private readonly VarKeys _varKeys = new VarKeys();
        private readonly VarStore _varStore = new VarStore();

        public static async Task<SyncedMatch> Create(ISocket socket, ISession session, SyncedOpcodes opcodes)
        {
            var newMatch = new SyncedMatch(socket, session, opcodes);
            socket.ReceivedMatchPresence += newMatch._presenceTracker.HandlePresenceEvent;
            newMatch._presenceTracker.OnGuestJoined += newMatch.HandleGuestJoined;
            newMatch._match = await socket.CreateMatchAsync();
            return newMatch;
        }

        public static async Task<SyncedMatch> Join(ISocket socket, ISession session, SyncedOpcodes opcodes, string matchId)
        {
            var newMatch = new SyncedMatch(socket, session, opcodes);
            socket.ReceivedMatchPresence += newMatch._presenceTracker.HandlePresenceEvent;
            newMatch._presenceTracker.OnGuestJoined += newMatch.HandleGuestJoined;
            newMatch._match = await socket.JoinMatchAsync(matchId);
            return newMatch;
        }

        public void RegisterBool(string id, UserVar<bool> userBool)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userBool, _varStore.UserBools, store => store.AddBool);
        }

        public void RegisterFloat(string id, UserVar<float> userFloat)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userFloat, _varStore.UserFloats, store => store.AddFloat);
        }

        public void RegisterInt(string id, UserVar<int> userInt)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userInt, _varStore.UserInts, store => store.AddInt);
        }

        public void RegisterString(string id, UserVar<string> userString)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userString, _varStore.UserStrings, store => store.AddString);
        }

        private SyncedMatch(ISocket socket, ISession session, SyncedOpcodes opcodes)
        {
            _presenceTracker = new PresenceTracker(session.UserId, trackHost: true, hostHeuristic: PresenceTracker.HostHeuristic.OldestMember);
            _session = session;
            _socket = socket;
            _opcodes = opcodes;
            _guestHandler = new GuestHandler(_presenceTracker, _varStore, _varKeys);
            _hostHandler = new HostHandler(_presenceTracker, _varStore, _varKeys);
            _socket.ReceivedMatchState += HandleReceivedMatchState;
        }

        private void Register<T>(VarKey key, UserVar<T> userVar, IDictionary<VarKey, UserVar<T>> varStore, Func<SyncVarValues, Action<SyncVarValue<T>>> getAddToQueue)
        {
            _varKeys.RegisterKey(key, userVar.KeyValidationStatus);
            varStore[key] = userVar;
            userVar.Self = _presenceTracker.GetSelf();
            userVar.OnLocalValueChanged += (evt) => HandleUserVarChanged<T>(key, evt, getAddToQueue);
        }

        private void HandleGuestJoined(IUserPresence joinedGuest)
        {
            var keysForValidation = _varKeys.GetKeys().ToList();
            _socket.SendMatchStateAsync(
                _match.Id,
                _opcodes.HandshakeOpcode,
                Encode(new HandshakeRequest(keysForValidation)),
                new IUserPresence[]{_presenceTracker.GetHost()});
        }

        private void HandleHandshakeRequestSend(HandshakeRequest request)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.HandshakeOpcode, Encode(request), new IUserPresence[]{_presenceTracker.GetHost()});
        }

        private void HandleHandshakeResponseSend(IUserPresence target, HandshakeResponse response)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.DataOpcode, Encode(response), new IUserPresence[]{target});
        }

        private void HandleReplicatedDataSend(IEnumerable<IUserPresence> targets, SyncVarValues values)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.DataOpcode, Encode(values), targets);
        }

        private void HandleReceivedMatchState(IMatchState matchState)
        {
            if (matchState.OpCode == _opcodes.DataOpcode)
            {
                if (_presenceTracker.GetHost().UserId == _session.UserId)
                {
                    SyncVarValues incomingStore = JsonParser.FromJson<SyncVarValues>(System.Text.Encoding.UTF8.GetString(matchState.State));
                    _hostHandler.HandleRemoteDataChanged(matchState.UserPresence, incomingStore);
                }
                else
                {
                    SyncVarValues incomingStore = JsonParser.FromJson<SyncVarValues>(System.Text.Encoding.UTF8.GetString(matchState.State));
                    _guestHandler.HandleRemoteDataChanged(matchState.UserPresence, incomingStore);
                }

            }
            else if (matchState.OpCode == _opcodes.HandshakeOpcode)
            {
                if (_presenceTracker.GetHost().UserId == _session.UserId)
                {
                    string json = System.Text.Encoding.UTF8.GetString(matchState.State);
                    var handshakeRequest = JsonParser.FromJson<HandshakeRequest>(json);
                    _hostHandler.ReceivedHandshakeRequest(matchState.UserPresence, handshakeRequest);
                }
                else
                {
                    string json = System.Text.Encoding.UTF8.GetString(matchState.State);
                    var handshakeResponse = JsonParser.FromJson<HandshakeResponse>(json);
                    _guestHandler.ReceivedHandshakeResponse(handshakeResponse);
                }
            }
        }

        private byte[] Encode(object data)
        {
            return System.Text.Encoding.UTF8.GetBytes(data.ToJson());
        }

        private T Decode<T>(byte[] data)
        {
            return Nakama.TinyJson.JsonParser.FromJson<T>(System.Text.Encoding.UTF8.GetString(data));
        }

        private void HandleUserVarChanged<T>(VarKey key, IUserVarEvent<T> evt, Func<SyncVarValues, Action<SyncVarValue<T>>> getOutgoingQueue)
        {
            if (_varKeys.HasLockVersion(key))
            {
                _varKeys.IncrementLockVersion(key);
            }
            else
            {
                throw new KeyNotFoundException("Tried incrementing lock version for non-existent key: " + key);
            }

            if (_presenceTracker.IsSelfHost())
            {
                var handler = new HostHandler(_presenceTracker, _varStore, _varKeys);
                handler.HandleLocalDataChanged<T>(key, evt.NewValue, getOutgoingQueue);
            }
            else
            {
                var handler = new GuestHandler(_presenceTracker, _varStore, _varKeys);
                handler.HandleLocalDataChanged<T>(key, evt.NewValue, getOutgoingQueue);
            }
        }
    }
}
