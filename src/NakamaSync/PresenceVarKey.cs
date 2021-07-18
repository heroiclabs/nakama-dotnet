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

using System.Runtime.Serialization;

namespace NakamaSync
{
    // use a struct for bit-by-bit equality comparisons.
    internal struct PresenceVarKey
    {
        [DataMember(Name="key"), Preserve]
        public string Key { get; set; }

        [DataMember(Name="user_id"), Preserve]
        public string UserId { get; set; }

        public PresenceVarKey(string key, string userId)
        {
            Key = key;
            UserId = userId;
        }

        public override string ToString()
        {
            return $"PresenceValueKey(Key='{Key}', UserId='{UserId}'";
        }
    }
}
