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

        public IUserPresence Presence => _presence;

        private readonly IUserPresence _presence;
        private readonly ReplicatedVarStore _varStore;

        private readonly ReplicatedValueStore _valuesToAll = new ReplicatedValueStore();
        private readonly Dictionary<string, ReplicatedValueStore> _valuesToGuest = new Dictionary<string, ReplicatedValueStore>();
        private readonly ReplicatedPresenceTracker _presenceTracker;

        public ReplicatedHost(IUserPresence presence, ReplicatedPresenceTracker presenceTracker, ReplicatedVarStore varStore)
        {
            _presence = presence;
            _presenceTracker = presenceTracker;
            _varStore = varStore;
        }

        public void ReceivedHandshakeRequest(IUserPresence requester, HandshakeRequest request)
        {
            HandshakeResponse response;

            List<ReplicatedKey> localKeys = _varStore.GetAllKeysAsList();

            bool success = localKeys.All(request.AllKeys.Contains);

            ReplicatedValueStore valStore = null;

            if (success)
            {
                // user may have joined mid-match. send data for them to sync.
                // todo we don't send any pending values in the var store.
                // that is perhaps an optimization that can be made later.
                valStore = new ReplicatedValueStore();

                foreach (KeyValuePair<ReplicatedKey, ReplicatedVar<bool>> kvp in _varStore.Bools)
                {
                    valStore.AddBool(ReplicatedVarToValue(kvp));
                }

                foreach (KeyValuePair<ReplicatedKey, ReplicatedVar<float>> kvp in _varStore.Floats)
                {
                    valStore.AddFloat(ReplicatedVarToValue(kvp));
                }

                foreach (KeyValuePair<ReplicatedKey, ReplicatedVar<int>> kvp in _varStore.Ints)
                {
                    valStore.AddInt(ReplicatedVarToValue(kvp));
                }

                foreach (KeyValuePair<ReplicatedKey, ReplicatedVar<string>> kvp in _varStore.Strings)
                {
                    valStore.AddString(ReplicatedVarToValue(kvp));
                }
            }

            response = new HandshakeResponse(valStore, success);

            if (OnHandshakeResponseSend != null)
            {
                OnHandshakeResponseSend(requester, response);
            }
        }

        public void HandleRemoteDataChanged(IUserPresence sender, ReplicatedValueStore remoteVals)
        {
            // prepare data to send back to user for data that requires host validation.
            if (!_valuesToGuest.ContainsKey(sender.UserId))
            {
                _valuesToGuest[sender.UserId] = new ReplicatedValueStore();
            }

            var merger = new ValueMergerHost(sender, _varStore, remoteVals, _valuesToGuest[sender.UserId]);
            merger.Merge();

            OnReplicatedDataSend(new IUserPresence[]{sender}, _valuesToGuest[sender.UserId]);
            OnReplicatedDataSend(_presenceTracker.Guests.Select(guest => guest.Presence), _valuesToAll);
        }

        private ReplicatedValue<T> ReplicatedVarToValue<T>(KeyValuePair<ReplicatedKey, ReplicatedVar<T>> kvp)
        {
            return new ReplicatedValue<T>(kvp.Key, kvp.Value.GetValue(), _varStore.GetLockVersion(kvp.Key), kvp.Value.KeyValidationStatus, _presence);
        }

        public void HandleLocalDataChanged<T>(ReplicatedKey key, T newValue, Action<ReplicatedValueStore, ReplicatedValue<T>> addMethod)
        {
            if (_varStore.HasLockVersion(key))
            {
                _varStore.IncrementLockVersion(key);
            }
            else
            {
                throw new KeyNotFoundException("Tried incrementing lock version for non-existent key: " + key);
            }

            var status = _varStore.GetValidationStatus(key);

            if (status == KeyValidationStatus.Pending)
            {
                throw new InvalidOperationException("Host should not have local key pending validation: " + key);
            }

            addMethod(_valuesToAll, new ReplicatedValue<T>(key, newValue, _varStore.GetLockVersion(key), status, _presence));
        }
    }
}
