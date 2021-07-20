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
using System.Runtime.Serialization;

namespace NakamaSync
{
    internal class PresenceVarKey
    {
        [DataMember(Name="key"), Preserve]
        public string CollectionKey { get; set; }

        [DataMember(Name="index"), Preserve]
        public int Index { get; set; }

        public PresenceVarKey(string key, int index)
        {
            CollectionKey = key;
            Index = index;
        }

        public static List<PresenceVarKey> Create<T>(string collectionKey, PresenceVarCollection<T> collection)
        {
            var selfVar = collection.SelfVar;
            var presenceVar = collection.PresenceVars;

            var keys = new List<PresenceVarKey>();

            keys.Add(new PresenceVarKey(collectionKey, 0));

            for (int i = 0; i < collection.PresenceVars.Count; i++)
            {
                keys.Add(new PresenceVarKey(collectionKey, i + 1));
            }

            return keys;
        }

        public override string ToString()
        {
            return $"PresenceVarKey(Key='{CollectionKey}', Index='{Index})'";
        }
    }
}
