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
        [DataMember(Name="collectionKey"), Preserve]
        public string CollectionKey { get; set; }

        [DataMember(Name="userId"), Preserve]
        public string UserId { get; set; }

        public PresenceVarKey(string collectionKey, string userId)
        {
            CollectionKey = collectionKey;
            UserId = userId;
        }

        public override string ToString()
        {
            return $"PresenceVarKey(Key='{CollectionKey}', Index='{UserId})'";
        }
    }
}
