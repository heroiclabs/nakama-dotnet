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
    /// A chat channel on the server.
    /// </summary>
    public interface IChannel
    {
        /// <summary>
        /// The server-assigned channel ID.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The presences visible on the chat channel.
        /// </summary>
        IEnumerable<IUserPresence> Presences { get; }

        /// <summary>
        /// The presence of the current user. i.e. Your self.
        /// </summary>
        IUserPresence Self { get; }

        /// <summary>
        /// The name of the chat room, or an empty string if this message was not sent through a chat room.
        /// </summary>
        string RoomName { get; }

        /// <summary>
        /// The ID of the group, or an empty string if this message was not sent through a group channel.
        /// </summary>
        string GroupId { get; }

        /// <summary>
        /// The ID of the first DM user, or an empty string if this message was not sent through a DM chat.
        /// </summary>
        string UserIdOne { get; }

        /// <summary>
        /// The ID of the second DM user, or an empty string if this message was not sent through a DM chat.
        /// </summary>
        string UserIdTwo { get; }
    }

    /// <inheritdoc cref="IChannel"/>
    internal class Channel : IChannel
    {
        [DataMember(Name="id"), Preserve]
        public string Id { get; set; }

        public IEnumerable<IUserPresence> Presences => _presences ?? UserPresence.NoPresences;
        [DataMember(Name="presences"), Preserve]
        public List<UserPresence> _presences { get; set; }

        public IUserPresence Self => _self;
        [DataMember(Name="self"), Preserve]
        public UserPresence _self { get; set; }

        [DataMember(Name="room_name"), Preserve]
        public string RoomName { get; set; }

        [DataMember(Name="group_id"), Preserve]
        public string GroupId { get; set; }

        [DataMember(Name="user_id_one"), Preserve]
        public string UserIdOne { get; set; }

        [DataMember(Name="user_id_two"), Preserve]
        public string UserIdTwo { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is Channel item))
            {
                return false;
            }
            return Equals(item);
        }

        private bool Equals(IChannel other) => string.Equals(Id, other.Id);

        public override int GetHashCode() => Id != null ? Id.GetHashCode() : 0;

        public override string ToString()
        {
            var presences = string.Join(", ", Presences);
            return $"Channel(Id='{Id}', Presences=[{presences}], Self={Self}, RoomName='{RoomName}', GroupId='{GroupId}', UserIdOne='{UserIdOne}', UserIdTwo='{UserIdTwo}')";
        }
    }
}
