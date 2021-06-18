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

namespace Nakama.Replicated
{
    internal class ReplicatedHost : IReplicatedMember
    {
        public event Action<IUserPresence, HandshakeResponse> OnHandshakeResponseSend;
        public event Action<IEnumerable<IUserPresence>, ReplicatedValueStore> OnReplicatedDataSend;

        private PresenceTracker _presenceTracker;
        public IUserPresence Presence => _presenceTracker.GetHost();

        private readonly IReadOnlyDictionary<string, ReplicatedGuest> _guests;
        private readonly ReplicatedValueStore _valuesToAll = new ReplicatedValueStore();
        private readonly Dictionary<string, ReplicatedValueStore> _valuesToGuest = new Dictionary<string, ReplicatedValueStore>();
        private readonly Store _ownedStore;
        public ReplicatedHost(PresenceTracker presenceTracker, Store ownedStore)
        {
            _presenceTracker = presenceTracker;
            _ownedStore = ownedStore;
        }

        public void ReceivedHandshakeRequest(IUserPresence requester, HandshakeRequest request)
        {
            HandshakeResponse response;

            List<ReplicatedKey> localKeys = _ownedStore.GetAllKeysAsList();

            bool success = localKeys.All(request.AllKeys.Contains);

            ReplicatedValueStore outgoingValues = null;

            if (success)
            {
                // user may have joined mid-match. send data for them to sync.
                // todo we don't send any pending values in the var store.
                // that is perhaps an optimization that can be made later.
                outgoingValues = new ReplicatedValueStore();

                foreach (KeyValuePair<ReplicatedKey, Owned<bool>> kvp in _ownedStore.Bools)
                {
                    outgoingValues.AddBool(OwnedToValue(kvp));
                }

                foreach (KeyValuePair<ReplicatedKey, Owned<float>> kvp in _ownedStore.Floats)
                {
                    outgoingValues.AddFloat(OwnedToValue(kvp));
                }

                foreach (KeyValuePair<ReplicatedKey, Owned<int>> kvp in _ownedStore.Ints)
                {
                    outgoingValues.AddInt(OwnedToValue(kvp));
                }

                foreach (KeyValuePair<ReplicatedKey, Owned<string>> kvp in _ownedStore.Strings)
                {
                    outgoingValues.AddString(OwnedToValue(kvp));
                }
            }

            response = new HandshakeResponse(outgoingValues, success);

            if (OnHandshakeResponseSend != null)
            {
                OnHandshakeResponseSend(requester, response);
            }
        }

        public void HandleRemoteDataChanged(IUserPresence source, ReplicatedValueStore remoteVals)
        {
            // prepare data to send back to user for data that requires host validation.
            if (!_valuesToGuest.ContainsKey(source.UserId))
            {
                _valuesToGuest[source.UserId] = new ReplicatedValueStore();
            }

            var merger = new ValueMergerHost(_presenceTracker, source, _ownedStore, remoteVals, _valuesToGuest[source.UserId]);
            merger.Merge();

            OnReplicatedDataSend(new IUserPresence[]{source}, _valuesToGuest[source.UserId]);
            OnReplicatedDataSend(_guests.Select(kvp => kvp.Value.Presence), _valuesToAll);
        }

        private ReplicatedValue<T> OwnedToValue<T>(KeyValuePair<ReplicatedKey, Owned<T>> kvp)
        {
            return new ReplicatedValue<T>(kvp.Key, kvp.Value.GetValue(Presence), _ownedStore.GetLockVersion(kvp.Key), kvp.Value.KeyValidationStatus, Presence);
        }

        public void HandleLocalDataChanged<T>(ReplicatedKey key, T newValue, Action<ReplicatedValueStore, ReplicatedValue<T>> addMethod)
        {
            if (_ownedStore.HasLockVersion(key))
            {
                _ownedStore.IncrementLockVersion(key);
            }
            else
            {
                throw new KeyNotFoundException("Tried incrementing lock version for non-existent key: " + key);
            }

            var status = _ownedStore.GetValidationStatus(key);

            if (status == KeyValidationStatus.Pending)
            {
                throw new InvalidOperationException("Host should not have local key pending validation: " + key);
            }

            addMethod(_valuesToAll, new ReplicatedValue<T>(key, newValue, _ownedStore.GetLockVersion(key), status, Presence));
        }
    }
}
