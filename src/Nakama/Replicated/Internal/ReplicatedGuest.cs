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
    internal class ReplicatedGuest : IReplicatedMember
    {
        public event Action<IEnumerable<IUserPresence>, ReplicatedValueStore> OnReplicatedDataSend;

        public IUserPresence Presence { get; }

        private readonly PresenceTracker _presenceTracker;
        private readonly Store _ownedStore;

        private readonly ReplicatedValueStore _valuesToHost = new ReplicatedValueStore();
        private readonly ReplicatedValueStore _valuesToAll = new ReplicatedValueStore();

        public ReplicatedGuest(IUserPresence presence, PresenceTracker presenceTracker, Store ownedStore)
        {
            Presence = presence;
            _presenceTracker = presenceTracker;
            _ownedStore = ownedStore;
        }

        public void ReceivedHandshakeResponse(HandshakeResponse response)
        {
            if (response.Success)
            {
                var merger = new ValueMergerGuest(
                    _presenceTracker,
                    _presenceTracker.GetHost(),
                    _ownedStore,
                    response.CurrentStore);

                merger.Merge();
            }
            else
            {
                throw new Exception("Host rejected client due to mismatched app binaries.");
            }
        }

        public void HandleLocalDataChanged<T>(ReplicatedKey key, T newValue, Action<ReplicatedValueStore, ReplicatedValue<T>> addToOutgoingStore)
        {
            if (_ownedStore.HasLockVersion(key))
            {
                _ownedStore.IncrementLockVersion(key);
            }
            else
            {
                throw new KeyNotFoundException("Tried incrementing lock version for non-existent key: " + key);
            }

            KeyValidationStatus status = _ownedStore.GetValidationStatus(key);

            if (status == KeyValidationStatus.Validated)
            {
                status = KeyValidationStatus.Pending;
            }

            var replicatedValue = new ReplicatedValue<T>(key, newValue, _ownedStore.GetLockVersion(key), status, Presence);

            ReplicatedValueStore outgoingStore = status == KeyValidationStatus.Pending ? _valuesToHost : _valuesToAll;
            addToOutgoingStore(outgoingStore, replicatedValue);
            // send to all
            OnReplicatedDataSend(null, outgoingStore);
        }

        public void HandleRemoteDataChanged(IUserPresence sender, ReplicatedValueStore remoteVals)
        {
            var merger = new ValueMergerGuest(_presenceTracker, sender, _ownedStore, remoteVals);
            merger.Merge();
        }
    }
}
