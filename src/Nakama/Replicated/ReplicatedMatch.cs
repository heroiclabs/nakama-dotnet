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

namespace Nakama.Replicated
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
    // todo replicated composite object
    // todo replicated list
    // todo potential race when creating and joining a match between the construction of this object
    // and the dispatching of presence objects off the socket.
    // TODO restore the default getvalue call with self
    // ~destructor definitely doesn't work, think about end match flow.
    public class ReplicatedMatch : IMatch
    {
        public event Action<Exception> OnError;

        public bool Authoritative => _match.Authoritative;
        public string Id => _match.Id;
        public string Label => _match.Label;
        public IEnumerable<IUserPresence> Presences => _match.Presences;
        public int Size => _match.Size;
        public IUserPresence Self => _match.Self;

        private readonly IMatch _match;
        private readonly ReplicatedPresenceTracker _presenceTracker;
        private readonly ReplicatedVarStore _varStore;

        internal ReplicatedMatch(IMatch match, ReplicatedVarStore varStore, ReplicatedPresenceTracker presenceTracker)
        {
            _match = match;
            _varStore = varStore;
            _presenceTracker = presenceTracker;
        }

        public void RegisterBool(string id, ReplicatedVar<bool> replicatedBool)
        {
            var key = new ReplicatedKey(id, _match.Self.UserId);
            replicatedBool.Self = _match.Self;
            _varStore.RegisterBool(key, replicatedBool);
            replicatedBool.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.GetSelf().HandleLocalDataChanged<bool>(key, newValue, (store, val) => store.AddBool(val));
        }

        public void RegisterFloat(string id, ReplicatedVar<float> replicatedFloat)
        {
            var key = new ReplicatedKey(id, _match.Self.UserId);
            replicatedFloat.Self = _match.Self;
            _varStore.RegisterFloat(key, replicatedFloat);
            replicatedFloat.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.GetSelf().HandleLocalDataChanged<float>(key, newValue, (store, val) => store.AddFloat(val));
        }

        public void RegisterInt(string id, ReplicatedVar<int> replicatedInt)
        {
            var key = new ReplicatedKey(id, _match.Self.UserId);
            replicatedInt.Self = _match.Self;
            _varStore.RegisterInt(key, replicatedInt);
            replicatedInt.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.GetSelf().HandleLocalDataChanged<int>(key, newValue, (store, val) => store.AddInt(val));
        }

        public void RegisterString(string id, ReplicatedVar<string> replicatedString)
        {
            var key = new ReplicatedKey(id, _match.Self.UserId);
            replicatedString.Self = _match.Self;
            _varStore.RegisterString(key, replicatedString);
            replicatedString.OnValueChangedLocal += (oldValue, newValue) => _presenceTracker.GetSelf().HandleLocalDataChanged<string>(key, newValue, (store, val) => store.AddString(val));
        }
    }
}
