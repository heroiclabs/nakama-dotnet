/**
 * Copyright 2018 The Nakama Authors
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

namespace Nakama
{
    /// <summary>
    /// Follow one or more other users for status updates.
    /// </summary>
    internal class StatusFollowMessage
    {
        [DataMember(Name = "user_ids"), Preserve] public List<string> UserIds { get; set; }

        [DataMember(Name = "usernames"), Preserve] public List<string> Usernames { get; set; }

        public override string ToString()
        {
            var userIds = string.Join(", ", UserIds);
            var usernames = string.Join(", ", Usernames);
            return $"StatusFollowMessage(UserIds=[{userIds}],Usernames=[{usernames}])";
        }
    }
}
