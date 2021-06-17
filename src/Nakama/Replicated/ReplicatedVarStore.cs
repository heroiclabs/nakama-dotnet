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

namespace Nakama.Replicated
{
    internal class ReplicatedVarStore
    {
        public IReadOnlyDictionary<ReplicatedKey, ReplicatedVar<bool>> Bools => _bools;
        public IReadOnlyDictionary<ReplicatedKey, ReplicatedVar<float>> Floats => _floats;
        public IReadOnlyDictionary<ReplicatedKey, ReplicatedVar<int>> Ints => _ints;
        public IReadOnlyDictionary<ReplicatedKey, ReplicatedVar<string>> Strings => _strings;

        private readonly ConcurrentDictionary<KeyValidationStatus, HashSet<ReplicatedKey>> _keys = new ConcurrentDictionary<KeyValidationStatus, HashSet<ReplicatedKey>>();
        private readonly ConcurrentDictionary<ReplicatedKey, int> _lockVersions = new ConcurrentDictionary<ReplicatedKey, int>();
        private readonly object _lockVersionLock = new object();

        // TODO what if we have outgoing at the same time
        private readonly ConcurrentDictionary<ReplicatedKey, ReplicatedVar<bool>> _bools = new ConcurrentDictionary<ReplicatedKey, ReplicatedVar<bool>>();
        private readonly ConcurrentDictionary<ReplicatedKey, ReplicatedVar<float>> _floats = new ConcurrentDictionary<ReplicatedKey, ReplicatedVar<float>>();
        private readonly ConcurrentDictionary<ReplicatedKey, ReplicatedVar<int>> _ints = new ConcurrentDictionary<ReplicatedKey, ReplicatedVar<int>>();
        private readonly ConcurrentDictionary<ReplicatedKey, ReplicatedVar<string>> _strings = new ConcurrentDictionary<ReplicatedKey, ReplicatedVar<string>>();

        public ReplicatedVarStore()
        {
            _keys[KeyValidationStatus.None] = new HashSet<ReplicatedKey>();
            _keys[KeyValidationStatus.Pending] = new HashSet<ReplicatedKey>();
            _keys[KeyValidationStatus.Validated] = new HashSet<ReplicatedKey>();
        }

        // note: MAY CONTAIN DUPLICATES because they will have been filtered by type
        // todo check keys validated by host and lock versions for duplicates.

        ~ReplicatedVarStore()
        {
            foreach (ReplicatedVar<bool> b in _bools.Values)
            {
                b.Clear();
            }

            foreach (ReplicatedVar<float> f in _floats.Values)
            {
                f.Clear();
            }

            foreach (ReplicatedVar<int> i in _ints.Values)
            {
                i.Clear();
            }

            foreach (ReplicatedVar<string> s in _strings.Values)
            {
                s.Clear();
            }
        }

        public List<ReplicatedKey> GetAllKeysAsList()
        {
            return new List<ReplicatedKey>(GetAllKeys());
        }

        public int GetLockVersion(ReplicatedKey key)
        {
            return _lockVersions[key];
        }

        public KeyValidationStatus GetValidationStatus(ReplicatedKey key)
        {
            if (_keys[KeyValidationStatus.None].Contains(key))
            {
                return KeyValidationStatus.None;
            }

            foreach (Enum value in Enum.GetValues(typeof(KeyValidationStatus)))
            {
                if (_keys[(KeyValidationStatus) value].Contains(key))
                {
                    return (KeyValidationStatus) value;
                }
            }

            throw new KeyNotFoundException($"Could not find replicated key {key}");
        }

        public bool HasLockVersion(ReplicatedKey key)
        {
            return _lockVersions.ContainsKey(key);
        }

        public void IncrementLockVersion(ReplicatedKey key)
        {
            lock (_lockVersionLock)
            {
                _lockVersions[key]++;
            }
        }

        public void RegisterBool(ReplicatedKey key, ReplicatedVar<bool> replicatedBool)
        {
            Register(key, replicatedBool, _bools);
        }

        public void RegisterFloat(ReplicatedKey key, ReplicatedVar<float> replicatedFloat)
        {
            Register(key, replicatedFloat, _floats);
        }

        public void RegisterInt(ReplicatedKey key, ReplicatedVar<int> replicatedInt)
        {
            Register(key, replicatedInt, _ints);
        }

        public void RegisterString(ReplicatedKey key, ReplicatedVar<string> replicatedString)
        {
            Register(key, replicatedString, _strings);
        }

        private HashSet<ReplicatedKey> GetAllKeys()
        {
            var keysCopy = new HashSet<ReplicatedKey>();

            foreach (var kvp in _keys)
            {
                foreach (var value in kvp.Value)
                {
                    keysCopy.Add(value);
                }
            }

            return keysCopy;
        }


        private void Register<T>(
            ReplicatedKey key,
            ReplicatedVar<T> replicated,
            ConcurrentDictionary<ReplicatedKey, ReplicatedVar<T>> collection)
        {
            if (collection.ContainsKey(key))
            {
                throw new ArgumentException($"Duplicate key for replicated variable: {key}");
            }

            _lockVersions[key] = 0;
            collection[key] = replicated;
        }
    }
}
