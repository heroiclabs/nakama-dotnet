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
    // todo synced composite object?
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
    // override tostring
    public class SyncMatch
    {
        public event Action<Exception> OnError;

        private SyncVarKeys _keys;
        private string _matchId;
        private readonly SyncOpcodes _opcodes;
        private readonly ISocket _socket;
        private readonly PresenceTracker _presenceTracker;
        private SyncVarRouter _router;
        private readonly ISession _session;

        private readonly SyncVarRegistryInternal _registryInternal;
        private readonly SyncVarRegistryIngress _registryIngress;

        private readonly Dictionary<string, SyncValues> _outgoingToGuest = new Dictionary<string, SyncValues>();
        private readonly SyncValues _outgoingToHost = new SyncValues();
        private SyncValues _outgoingToAll = new SyncValues();
        private readonly SyncValues _incomingValues = new SyncValues();

        private readonly SyncCollections _collections;

        internal SyncMatch(string matchId, ISession session, ISocket socket, SyncOpcodes opcodes, SyncVarRegistry registry)
        {
            _matchId = matchId;
            _session = session;
            _socket = socket;
            _opcodes = opcodes;

            _keys = new SyncVarKeys();
            _presenceTracker = new PresenceTracker(session.UserId);
            _router = new SyncVarRouter(_keys, _presenceTracker);
            _collections = new SyncCollections(_registryInternal, _outgoingToAll, _outgoingToHost, _outgoingToGuest, _incomingValues);
            _registryIngress = new SyncVarRegistryIngress(session, _keys, _collections, _router);
            _socket.ReceivedMatchState += HandleReceivedMatchState;

            _router.OnHandshakeRequestReady += HandleHandshakeRequestReady;
            _router.OnHandshakeResponseReady += HandleHandshakeResponseReady;
            _router.OnHostVarsReady+= HandleHostVarsReady;
            _router.OnGuestVarsReady += HandleGuestVarsReady;
        }

        public void HandleGuestJoined(IUserPresence joinedGuest)
        {
            var keysForValidation = _keys.GetKeys();

            _socket.SendMatchStateAsync(
                _matchId,
                _opcodes.HandshakeOpcode,
                Encode(new HandshakeRequest(keysForValidation.ToList())),
                new IUserPresence[]{_presenceTracker.GetHost()});
        }

        private void HandleHandshakeRequestReady(IUserPresence target)
        {
            var request = new HandshakeRequest(_keys.GetKeys().ToList());
            _socket.SendMatchStateAsync(_matchId, _opcodes.DataOpcode, Encode(request), new IUserPresence[]{target});
        }

        private void HandleHandshakeResponseReady(IUserPresence target, bool success)
        {
            HandshakeResponse response = new HandshakeResponse(success ? _outgoingToGuest[target.UserId] : new SyncValues(), success);
            _socket.SendMatchStateAsync(_matchId, _opcodes.DataOpcode, Encode(response), new IUserPresence[]{target});
        }

        private void HandleHostVarsReady()
        {
            _socket.SendMatchStateAsync(_matchId, _opcodes.DataOpcode, Encode(_outgoingToAll));

            // TODO only clear if successful send + ack
            _outgoingToAll = new SyncValues();

            foreach (KeyValuePair<string, SyncValues> guestValue in _outgoingToGuest)
            {
                _socket.SendMatchStateAsync(_matchId, _opcodes.DataOpcode, Encode(guestValue.Value), new IUserPresence[]{_presenceTracker.GetPresence(guestValue.Key)});
            }

            _outgoingToGuest.Clear();
        }

        private void HandleGuestVarsReady()
        {
            _socket.SendMatchStateAsync(_matchId, _opcodes.DataOpcode, Encode(_outgoingToAll));
            _socket.SendMatchStateAsync(_matchId, _opcodes.DataOpcode, Encode(_outgoingToHost), new IUserPresence[]{_presenceTracker.GetHost()});
        }

        private void HandleReceivedMatchState(IMatchState state)
        {
            if (state.OpCode == _opcodes.DataOpcode)
            {
                SyncValues values = Decode<SyncValues>(state.State);
                _router.HandleIncomingSyncValues(state.UserPresence, values, _collections);
            }
            else if (state.OpCode == _opcodes.HandshakeOpcode)
            {
                if (_presenceTracker.IsSelfHost())
                {
                    _router.HandleHandshakeRequest(state.UserPresence, Decode<HandshakeRequest>(state.State), _collections);
                }
                else
                {
                    _router.HandleHandshakeResponse(state.UserPresence, Decode<HandshakeResponse>(state.State), _collections);
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
