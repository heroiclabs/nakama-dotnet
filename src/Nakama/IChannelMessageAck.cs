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

using System.Runtime.Serialization;

namespace Nakama
{
    /// <summary>
    /// An acknowledgement from the server when a chat message is delivered to a channel.
    /// </summary>
    public interface IChannelMessageAck
    {
        /// <summary>
        /// The server-assigned channel ID.
        /// </summary>
        string ChannelId { get; }

        /// <summary>
        /// A user-defined code for the chat message.
        /// </summary>
        int Code { get; }

        /// <summary>
        /// The UNIX time when the message was created.
        /// </summary>
        string CreateTime { get; }

        /// <summary>
        /// A unique ID for the chat message.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// True if the chat message has been stored in history.
        /// </summary>
        bool Persistent { get; }

        /// <summary>
        /// The UNIX time when the message was updated.
        /// </summary>
        string UpdateTime { get; }

        /// <summary>
        /// The username of the sender of the message.
        /// </summary>
        string Username { get; }

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

    /// <inheritdoc cref="IChannelMessageAck"/>
    internal class ChannelMessageAck : IChannelMessageAck
    {
        [DataMember(Name = "channel_id"), Preserve] public string ChannelId { get; set; }

        [DataMember(Name = "code"), Preserve] public int Code { get; set; }

        [DataMember(Name = "create_time"), Preserve] public string CreateTime { get; set; }

        [DataMember(Name = "message_id"), Preserve] public string MessageId { get; set; }

        [DataMember(Name = "persistent"), Preserve] public bool Persistent { get; set; }

        [DataMember(Name = "update_time"), Preserve] public string UpdateTime { get; set; }

        [DataMember(Name = "username"), Preserve] public string Username { get; set; }

        [DataMember(Name="room_name"), Preserve] public string RoomName { get; set; }

        [DataMember(Name="group_id"), Preserve] public string GroupId { get; set; }

        [DataMember(Name="user_id_one"), Preserve] public string UserIdOne { get; set; }

        [DataMember(Name="user_id_two"), Preserve] public string UserIdTwo { get; set; }

        public override string ToString()
        {
            return
                $"ChannelMessageAck(ChannelId='{ChannelId}', Code={Code}, CreateTime={CreateTime}, MessageId='{MessageId}', Persistent={Persistent}, UpdateTime={UpdateTime}, Username='{Username}', RoomName='{RoomName}', GroupId='{GroupId}', UserIdOne='{UserIdOne}', UserIdTwo='{UserIdTwo}')";
        }
    }
}
