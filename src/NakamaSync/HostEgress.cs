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
using Nakama;

namespace NakamaSync
{
    internal class HostEgress : IVarEgress
    {
        private readonly SyncSocket _socket;
        private readonly SyncVarKeys _keys;
        private readonly PresenceTracker _presenceTracker;

        public HostEgress(SyncSocket socket, SyncVarKeys keys, PresenceTracker presenceTracker)
        {
            _socket = socket;
            _keys = keys;
            _presenceTracker = presenceTracker;
        }

        public void HandleLocalSharedVarChanged<T>(SyncVarKey key, T newValue, SharedVarAccessor<T> accessor)
        {
            var status = _keys.GetValidationStatus(key);

            if (status == KeyValidationStatus.Pending)
            {
                throw new InvalidOperationException("Host should not have local key pending validation: " + key);
            }

            SyncEnvelope envelope = new SyncEnvelope();
            accessor(envelope).Add(new SharedValue<T>(key, newValue, _keys.GetLockVersion(key), status));
            _socket.SendSyncDataToAll(envelope);
        }

        public void HandleLocalUserVarChanged<T>(SyncVarKey key, T newValue, UserVarAccessor<T> accessor, IUserPresence target)
        {
            var status = _keys.GetValidationStatus(key);

            if (status == KeyValidationStatus.Pending)
            {
                throw new InvalidOperationException("Host should not have local key pending validation: " + key);
            }

            // TODO clear collection if successful send + ack
            SyncEnvelope envelope = new SyncEnvelope();
            accessor(envelope).Add(new UserValue<T>(key, newValue, _keys.GetLockVersion(key), status, target));
            _socket.SendSyncDataToAll(envelope);
        }
    }
}
