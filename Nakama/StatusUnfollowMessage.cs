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
    /// Unfollow one or more users on the server.
    /// </summary>
    internal class StatusUnfollowMessage
    {
        [DataMember(Name="user_ids"), Preserve]
        public List<string> UserIds { get; set; }

        public override string ToString()
        {
            var userIds = string.Join(", ", UserIds);
            return $"StatusUnfollowMessage(UserIds=[{userIds}])";
        }
    }
}
