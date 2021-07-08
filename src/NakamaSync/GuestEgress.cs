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

using Nakama;

namespace NakamaSync
{
    internal class GuestEgress : IVarEgress
    {
        private readonly SyncSocket _socket;
        private readonly SyncVarKeys _keys;

        public GuestEgress(SyncSocket socket, SyncVarKeys keys)
        {
            _socket = socket;
            _keys = keys;
        }

        public void HandleLocalSharedVarChanged<T>(SyncVarKey key, T newValue, SharedVarAccessor<T> accessor)
        {
            var status = _keys.GetValidationStatus(key);

            if (status == KeyValidationStatus.Validated)
            {
                status = KeyValidationStatus.Pending;
                _keys.SetValidationStatus(key, status);
            }

            var newSyncedValue = new SharedValue<T>(key, newValue, _keys.GetLockVersion(key), status);

            var values = new SyncEnvelope();
            accessor(values).Add(newSyncedValue);

            if (status == KeyValidationStatus.Pending)
            {
                _socket.SendSyncDataToHost(values);
            }
            else
            {
                _socket.SendSyncDataToAll(values);
            }

            // todo clear on ack
        }

        public void HandleLocalUserVarChanged<T>(SyncVarKey key, T newValue, UserVarAccessor<T> accessor, IUserPresence target)
        {
            var status = _keys.GetValidationStatus(key);

            // this value was validated and now we've
            // modified it as a guest so revert it to pending status
            if (status == KeyValidationStatus.Validated)
            {
                status = KeyValidationStatus.Pending;
                _keys.SetValidationStatus(key, status);
            }

            var newSyncedValue = new UserValue<T>(key, newValue, _keys.GetLockVersion(key), status, target);

            var values = new SyncEnvelope();
            accessor(values).Add(newSyncedValue);

            _socket.SendSyncDataToAll(values);
        }
    }
}
