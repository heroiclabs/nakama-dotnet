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

namespace NakamaSync
{
    internal class LockVersionGuard
    {
        public ILogger Logger { get; set; }

        private readonly ConcurrentDictionary<string, int> _lockVersions = new ConcurrentDictionary<string, int>();

        public LockVersionGuard(IEnumerable<string> allKeys)
        {
            foreach (var key in allKeys)
            {
                Register(key);
            }
        }

        public bool IsValidLockVersion(string key, int newLockVersion)
        {
            if (!HasLockVersion(key))
            {
                throw new ArgumentException($"Received unrecognized remote key: {key}");
            }

            // todo one client updated locally while another value was in flight
            // how to handle? think about 2x2 host guest combos
            // also if values are equal it doesn't matter. (review: I don't remember what that means)

            int localLockVersion = GetLockVersion(key);

            // if new lock version is zero and they are equal, that just means var hasn't been set yet by any client.
            // if lock versions are equal, find a way that user can use a callback to decide who wins?
            // can default to a callback that says first one wins.
            return localLockVersion <= newLockVersion;
        }

        public int GetLockVersion(string key)
        {
            if (!HasLockVersion(key))
            {
                throw new KeyNotFoundException($"Lock version guard could not find key when getting lock version: {key}");
            }

            return _lockVersions[key];
        }

        public bool HasLockVersion(string key)
        {
            return _lockVersions.ContainsKey(key);
        }

        public void IncrementLockVersion(string key)
        {
            if (!HasLockVersion(key))
            {
                throw new KeyNotFoundException($"Lock version guard could not find key when incrementing lock version: {key}");
            }

            _lockVersions[key]++;
        }

        private void Register(string key)
        {
            _lockVersions[key] = 0;
        }
    }
}
