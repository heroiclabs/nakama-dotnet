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
using System.Collections.Concurrent;
using System.Collections.Generic;

// todo where do we even change validation status
namespace NakamaSync
{
    internal class SyncVarKeys
    {
        private readonly HashSet<SyncVarKey> _keys = new HashSet<SyncVarKey>();
        private readonly ConcurrentDictionary<SyncVarKey, int> _lockVersions = new ConcurrentDictionary<SyncVarKey, int>();
        private readonly object _lockVersionLock = new object();
        private readonly object _registerLock = new object();
        private readonly ConcurrentDictionary<SyncVarKey, KeyValidationStatus> _validationStatus = new ConcurrentDictionary<SyncVarKey, KeyValidationStatus>();

        public void RegisterKey(SyncVarKey key, KeyValidationStatus status)
        {
            lock (_registerLock)
            {
                if (!_keys.Add(key))
                {
                    throw new ArgumentException("Failed to register duplicate key: " + key);
                }

                _lockVersions[key] = 0;
                _validationStatus[key] = status;
            }
        }

        public HashSet<SyncVarKey> GetKeys()
        {
            return _keys;
        }

        public int GetLockVersion(SyncVarKey key)
        {
            return _lockVersions[key];
        }

        public KeyValidationStatus GetValidationStatus(SyncVarKey key)
        {
            if (!_validationStatus.ContainsKey(key))
            {
                throw new KeyNotFoundException($"Could not find key: {key}");
            }

            return _validationStatus[key];
        }

        public bool HasLockVersion(SyncVarKey key)
        {
            return _lockVersions.ContainsKey(key);
        }

        public void IncrementLockVersion(SyncVarKey key)
        {
            lock (_lockVersionLock)
            {
                _lockVersions[key]++;
            }
        }

        public void SetValidationStatus(SyncVarKey key, KeyValidationStatus status)
        {
            if (!_validationStatus.ContainsKey(key))
            {
                throw new KeyNotFoundException($"Could not find key: {key}");
            }

             _validationStatus[key] = status;
        }
    }
}