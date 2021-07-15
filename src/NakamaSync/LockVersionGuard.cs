

using System;
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
    internal class LockVersionGuard : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private VarKeys _keys;

        public LockVersionGuard(VarKeys keys)
        {
            _keys = keys;
        }

        public bool IsValidLockVersion(string key, int newLockVersion)
        {
            if (!_keys.HasLockVersion(key))
            {
                ErrorHandler?.Invoke(new ArgumentException($"Received unrecognized remote key: {key}"));
                return false;
            }

            // todo one client updated locally while another value was in flight
            // how to handle? think about 2x2 host guest combos
            // also if values are equal it doesn't matter. (review: I don't remember what that means)

            int localLockVersion = _keys.GetLockVersion(key);

            // if new lock version is zero and they are equal, that just means var hasn't been set yet by any client.
            // if lock versions are equal, find a way that user can use a callback to decide who wins?

            return _keys.GetLockVersion(key) <= newLockVersion;
        }
    }
}