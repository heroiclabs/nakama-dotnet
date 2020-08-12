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
    /// An object which represents a connected user in the server.
    /// </summary>
    /// <remarks>
    /// The server allows the same user to be connected with multiple sessions. To uniquely identify them a tuple of
    /// <c>{ node_id, user_id, session_id }</c> is used which is exposed as this object.
    /// </remarks>
    public interface IUserPresence
    {
        /// <summary>
        /// If this presence generates stored events like persistent chat messages or notifications.
        /// </summary>
        bool Persistence { get; }

        /// <summary>
        /// The session id of the user.
        /// </summary>
        string SessionId { get; }

        /// <summary>
        /// The status of the user with the presence on the server.
        /// </summary>
        string Status { get; }

        /// <summary>
        /// The username for the user.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// The id of the user.
        /// </summary>
        string UserId { get; }
    }

    /// <inheritdoc cref="IUserPresence"/>
    internal class UserPresence : IUserPresence
    {
        internal static readonly IReadOnlyList<UserPresence> NoPresences = new List<UserPresence>(0);

        [DataMember(Name = "persistence"), Preserve] public bool Persistence { get; set; }

        [DataMember(Name = "session_id"), Preserve] public string SessionId { get; set; }

        [DataMember(Name = "status"), Preserve] public string Status { get; set; }

        [DataMember(Name = "username"), Preserve] public string Username { get; set; }

        [DataMember(Name = "user_id"), Preserve] public string UserId { get; set; }

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
