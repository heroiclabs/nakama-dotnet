

/**
* Copyright 2020 The Nakama Authors
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

namespace Nakama.SocketInternal
{
/// <inheritdoc cref="IUserPresence"/>
    [DataContract]
    public class UserPresence : IUserPresence
    {
        public static readonly IReadOnlyList<UserPresence> NoPresences = new List<UserPresence>(0);

        [DataMember(Name = "persistence", Order = 4), Preserve]
        public bool Persistence { get; set; }

        [DataMember(Name = "session_id", Order = 2), Preserve]
        public string SessionId { get; set; }

        public string Status => _statusValue.Value ?? _status;

        [DataMember(Name = "username", Order = 3), Preserve]
        public string Username { get; set; }

        [DataMember(Name = "user_id", Order = 1), Preserve]
        public string UserId { get; set; }

        [DataMember(Name = "status"), Preserve]
        private string _status;

        [DataMember(Order = 5), Preserve]
        private StringValue _statusValue = new StringValue();

        public override bool Equals(object obj)
        {
            if (!(obj is UserPresence item))
            {
                return false;
            }
            return Equals(item);
        }

        private bool Equals(IUserPresence other) => string.Equals(SessionId, other.SessionId) && string.Equals(UserId, other.UserId);

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable twice NonReadonlyMemberInGetHashCode
                return ((SessionId?.GetHashCode() ?? 0) * 397) ^ (UserId?.GetHashCode() ?? 0);
            }
        }

        public override string ToString()
        {
            return
                $"UserPresence(Persistence={Persistence}, SessionId='{SessionId}', Status='{Status}', Username='{Username}', UserId='{UserId}')";
        }
    }
}