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
    internal class GuestEgress : IVarEgress
    {
        public event Action OnSyncedDataReady;

        private SyncVarKeys _keys;

        public GuestEgress(SyncVarKeys keys)
        {
            _keys = keys;
        }

        public void HandleLocalSharedVarChanged<T>(SyncVarKey key, T newValue, SharedVarCollections<T> collections)
        {
            var status = _keys.GetValidationStatus(key);

            if (status == KeyValidationStatus.Validated)
            {
                status = KeyValidationStatus.Pending;
                _keys.SetValidationStatus(key, status);
            }

            var newSyncedValue = new SyncSharedValue<T>(key, newValue, _keys.GetLockVersion(key), status);

            if (status == KeyValidationStatus.Pending)
            {
                collections.SharedValuesToHost.Add(newSyncedValue);
            }
            else
            {
                collections.SharedValuesToAll.Add(newSyncedValue);
            }
        }

        public void HandleLocalUserVarChanged<T>(SyncVarKey key, T newValue, UserVarCollections<T> collections, IUserPresence target)
        {
            var status = _keys.GetValidationStatus(key);

            if (status == KeyValidationStatus.Validated)
            {
                status = KeyValidationStatus.Pending;
                _keys.SetValidationStatus(key, status);
            }

            var newSyncedValue = new SyncUserValue<T>(key, newValue, _keys.GetLockVersion(key), status, target);

            if (status == KeyValidationStatus.Pending)
            {
                collections.UserValuesToHost.Add(newSyncedValue);
            }
            else
            {
                collections.UserValuesToHost.Add(newSyncedValue);
            }
        }
    }
}
