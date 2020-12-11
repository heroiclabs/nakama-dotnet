

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

using System.Runtime.Serialization;

namespace Nakama.SocketInternal
{
    /// <inheritdoc />
    public class ApiChannelMessage : IApiChannelMessage
    {
        /// <inheritdoc />
        [DataMember(Name="channel_id"), Preserve]
        public string ChannelId { get; set; }

        /// <inheritdoc />
        [DataMember(Name="code"), Preserve]
        public int Code { get; set; }

        /// <inheritdoc />
        [DataMember(Name="content"), Preserve]
        public string Content { get; set; }

        /// <inheritdoc />
        [DataMember(Name="create_time"), Preserve]
        public string CreateTime { get; set; }

        /// <inheritdoc />
        [DataMember(Name="group_id"), Preserve]
        public string GroupId { get; set; }

        /// <inheritdoc />
        [DataMember(Name="message_id"), Preserve]
        public string MessageId { get; set; }

        /// <inheritdoc />
        [DataMember(Name="persistent"), Preserve]
        public bool Persistent { get; set; }

        /// <inheritdoc />
        [DataMember(Name="room_name"), Preserve]
        public string RoomName { get; set; }

        /// <inheritdoc />
        [DataMember(Name="sender_id"), Preserve]
        public string SenderId { get; set; }

        /// <inheritdoc />
        [DataMember(Name="update_time"), Preserve]
        public string UpdateTime { get; set; }

        /// <inheritdoc />
        [DataMember(Name="user_id_one"), Preserve]
        public string UserIdOne { get; set; }

        /// <inheritdoc />
        [DataMember(Name="user_id_two"), Preserve]
        public string UserIdTwo { get; set; }

        /// <inheritdoc />
        [DataMember(Name="username"), Preserve]
        public string Username { get; set; }

        public override string ToString()
        {
            var output = "";
            output = string.Concat(output, "ChannelId: ", ChannelId, ", ");
            output = string.Concat(output, "Code: ", Code, ", ");
            output = string.Concat(output, "Content: ", Content, ", ");
            output = string.Concat(output, "CreateTime: ", CreateTime, ", ");
            output = string.Concat(output, "GroupId: ", GroupId, ", ");
            output = string.Concat(output, "MessageId: ", MessageId, ", ");
            output = string.Concat(output, "Persistent: ", Persistent, ", ");
            output = string.Concat(output, "RoomName: ", RoomName, ", ");
            output = string.Concat(output, "SenderId: ", SenderId, ", ");
            output = string.Concat(output, "UpdateTime: ", UpdateTime, ", ");
            output = string.Concat(output, "UserIdOne: ", UserIdOne, ", ");
            output = string.Concat(output, "UserIdTwo: ", UserIdTwo, ", ");
            output = string.Concat(output, "Username: ", Username, ", ");
            return output;
        }
    }
}
