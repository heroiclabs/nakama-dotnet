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
    /// A batch of join and leave presences on a chat channel.
    /// </summary>
    public interface IChannelPresenceEvent
    {
        /// <summary>
        /// The unique identifier of the chat channel.
        /// </summary>
        string ChannelId { get; }

        /// <summary>
        /// Presences of the users who joined the channel.
        /// </summary>
        IEnumerable<IUserPresence> Joins { get; }

        /// <summary>
        /// Presences of users who left the channel.
        /// </summary>
        IEnumerable<IUserPresence> Leaves { get; }

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

    /// <inheritdoc cref="IChannelPresenceEvent"/>
    internal class ChannelPresenceEvent : IChannelPresenceEvent
    {
        [DataMember(Name="channel_id"), Preserve]
        public string ChannelId { get; set; }

        public IEnumerable<IUserPresence> Joins => _joins ?? new List<UserPresence>(0);
        [DataMember(Name="joins"), Preserve]
        public List<UserPresence> _joins { get; set; }

        public IEnumerable<IUserPresence> Leaves => _leaves ?? new List<UserPresence>(0);
        [DataMember(Name="leaves"), Preserve]
        public List<UserPresence> _leaves { get; set; }

        [DataMember(Name="room_name"), Preserve]
        public string RoomName { get; set; }

        [DataMember(Name="group_id"), Preserve]
        public string GroupId { get; set; }

        [DataMember(Name="user_id_one"), Preserve]
        public string UserIdOne { get; set; }

        [DataMember(Name="user_id_two"), Preserve]
        public string UserIdTwo { get; set; }

        public override string ToString()
        {
            var joins = string.Join(",", Joins);
            var leaves = string.Join(",", Leaves);
            return $"ChannelPresenceEvent(ChannelId='{ChannelId}', Joins=[{joins}], Leaves=[{leaves}], RoomName='{RoomName}', GroupId='{GroupId}', UserIdOne='{UserIdOne}', UserIdTwo='{UserIdTwo}')";
        }
    }
}
