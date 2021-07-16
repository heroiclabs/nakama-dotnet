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
using Nakama;

// todo where do we even change validation status
namespace NakamaSync
{
    internal class VarKeys : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly HashSet<string> _keys = new HashSet<string>();
        private readonly ConcurrentDictionary<string, int> _lockVersions = new ConcurrentDictionary<string, int>();
        private readonly object _lockVersionLock = new object();
        private readonly object _registerLock = new object();
        private readonly ConcurrentDictionary<string, ValidationStatus> _validationStatus = new ConcurrentDictionary<string, ValidationStatus>();

        public void RegisterKey(string key, ValidationStatus status)
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

        public HashSet<string> GetKeys()
        {
            return _keys;
        }

        public int GetLockVersion(string key)
        {
            return _lockVersions[key];
        }

        public ValidationStatus GetValidationStatus(string key)
        {
            if (!_validationStatus.ContainsKey(key))
            {
                throw new KeyNotFoundException($"Could not find key: {key}");
            }

            return _validationStatus[key];
        }

        public bool HasLockVersion(string key)
        {
            return _lockVersions.ContainsKey(key);
        }

        public void IncrementLockVersion(string key)
        {
            lock (_lockVersionLock)
            {
                _lockVersions[key]++;
            }
        }

        public void SetValidationStatus(string key, ValidationStatus status)
        {
            if (!_validationStatus.ContainsKey(key))
            {
                ErrorHandler?.Invoke(new KeyNotFoundException($"Could not find key for setting validation status: {key}"));
                return;
            }

             _validationStatus[key] = status;
        }
    }
}
