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
    // todo handle host changed
    // todo handle guest left
    // todo what happens if user id changes? can it even?
    public class SyncedMatch : IMatch
    {
        public event Action<Exception> OnError;

        public bool Authoritative => _match.Authoritative;
        public string Id => _match.Id;
        public string Label => _match.Label;
        public IEnumerable<IUserPresence> Presences => _match.Presences;
        public int Size => _match.Size;
        public IUserPresence Self => _match.Self;

        private IMatch _match;
        private readonly SyncedOpcodes _opcodes;
        private readonly SyncedVarRegistration _registration;
        private readonly ISocket _socket;

        internal static async Task<SyncedMatch> Create(ISocket socket, ISession session, SyncedOpcodes opcodes, SyncedVarRegistration registration)
        {
            var newMatch = new SyncedMatch(socket, session, opcodes, registration);
            socket.ReceivedMatchPresence += newMatch._registration.PresenceTracker.HandlePresenceEvent;
            newMatch._registration.PresenceTracker.OnGuestJoined += newMatch.HandleGuestJoined;
            newMatch._match = await socket.CreateMatchAsync();
            return newMatch;
        }

        internal static async Task<SyncedMatch> Join(ISocket socket, ISession session, SyncedOpcodes opcodes, string matchId, SyncedVarRegistration registration)
        {
            var newMatch = new SyncedMatch(socket, session, opcodes, registration);
            socket.ReceivedMatchPresence += newMatch._registration.PresenceTracker.HandlePresenceEvent;
            newMatch._registration.PresenceTracker.OnGuestJoined += newMatch.HandleGuestJoined;
            newMatch._match = await socket.JoinMatchAsync(matchId);
            return newMatch;
        }

        private SyncedMatch(ISocket socket, ISession session, SyncedOpcodes opcodes, SyncedVarRegistration registration)
        {
            _socket = socket;
            _opcodes = opcodes;
            _registration = registration;
            _socket.ReceivedMatchState += HandleReceivedMatchState;
        }

        private void HandleGuestJoined(IUserPresence joinedGuest)
        {
            var keysForValidation = _registration.GetAllKeys();
            _socket.SendMatchStateAsync(
                _match.Id,
                _opcodes.HandshakeOpcode,
                Encode(new HandshakeRequest(keysForValidation)),
                new IUserPresence[]{_registration.PresenceTracker.GetHost()});
        }

        private void HandleHandshakeRequestSend(HandshakeRequest request)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.HandshakeOpcode, Encode(request), new IUserPresence[]{_registration.PresenceTracker.GetHost()});
        }

        private void HandleHandshakeResponseSend(IUserPresence target, HandshakeResponse response)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.DataOpcode, Encode(response), new IUserPresence[]{target});
        }

        private void HandleDataSend(IEnumerable<IUserPresence> targets, SyncVarValues values)
        {
            _socket.SendMatchStateAsync(_match.Id, _opcodes.DataOpcode, Encode(values), targets);
        }

        private void HandleReceivedMatchState(IMatchState matchState)
        {
            if (matchState.OpCode == _opcodes.DataOpcode)
            {
                SyncVarValues incomingStore = Decode<SyncVarValues>(matchState.State);

                if (_registration.PresenceTracker.GetHost().UserId == _registration.Session.UserId)
                {
                    _registration.HostHandler.HandleRemoteDataChanged(matchState.UserPresence, incomingStore);
                }
                else
                {
                    _registration.GuestHandler.HandleRemoteDataChanged(matchState.UserPresence, incomingStore);
                }
            }
            else if (matchState.OpCode == _opcodes.HandshakeOpcode)
            {
                if (_registration.PresenceTracker.GetHost().UserId == _registration.Session.UserId)
                {
                    var handshakeRequest = Decode<HandshakeRequest>(matchState.State);
                    _registration.HostHandler.ReceivedHandshakeRequest(matchState.UserPresence, handshakeRequest);
                }
                else
                {
                    var handshakeResponse = Decode<HandshakeResponse>(matchState.State);
                    _registration.GuestHandler.ReceivedHandshakeResponse(handshakeResponse);
                }
            }
        }

        private T Decode<T>(byte[] data)
        {
            return Nakama.TinyJson.JsonParser.FromJson<T>(System.Text.Encoding.UTF8.GetString(data));
        }

        private byte[] Encode(object data)
        {
            return System.Text.Encoding.UTF8.GetBytes(data.ToJson());
        }
    }
}
