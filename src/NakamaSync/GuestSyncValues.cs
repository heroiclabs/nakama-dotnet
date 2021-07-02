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

using System.Collections.Generic;

namespace NakamaSync
{
    internal class GuestSharedValues<T>
    {
        private readonly Dictionary<string, List<SyncSharedValue<T>>> _values = new Dictionary<string, List<SyncSharedValue<T>>>();

        public void Add(string userId, SyncSharedValue<T> value)
        {
            if (!_values.ContainsKey(userId))
            {
                _values[userId] = new List<SyncSharedValue<T>>();
            }

            _values[userId].Add(value);
        }
    }

    internal class GuestUserValues<T>
    {
        private readonly Dictionary<string, List<SyncUserValue<T>>> _values = new Dictionary<string, List<SyncUserValue<T>>>();

        public void Add(string userId, SyncUserValue<T> value)
        {
            if (!_values.ContainsKey(userId))
            {
                _values[userId] = new List<SyncUserValue<T>>();
            }

            _values[userId].Add(value);
        }
    }
}
