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
    /// <summary>
    /// A key that uniquely identifies a single sync var.
    /// The key is a combination of a user id and the sync var id.
    /// </summary>
    internal struct VarKey
    {
        public string UserId => _userId;

        [DataMember(Name="user_id"), Preserve]
        private string _userId;

        [DataMember(Name="synced_id"), Preserve]
        private string _syncedId;

        public VarKey(string userId, string syncedId)
        {
            _userId = userId;
            _syncedId = syncedId;
        }

        public override string ToString()
        {
            return $"ReplicatedKey(UserId='{_userId}', ReplicatedId='{_syncedId}')";
        }
    }
}