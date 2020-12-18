

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
    [DataContract]
    public class ApiChannelMessage : IApiChannelMessage
    {
        /// <inheritdoc />
        [DataMember(Name="channel_id", Order = 1), Preserve]
        public string ChannelId { get; set; }

        /// <inheritdoc />
        public int Code => _codeValue.HasValue ? _codeValue.Value : _code;

        /// <inheritdoc />
        [DataMember(Name="content", Order = 6), Preserve]
        public string Content { get; set; }

        /// <inheritdoc />
        public string CreateTime => _createTimeValue.HasValue ? _createTimeValue.Value.ToString() : _createTime;

        /// <inheritdoc />
        [DataMember(Name="group_id", Order = 11), Preserve]
        public string GroupId { get; set; }

        /// <inheritdoc />
        [DataMember(Name="message_id", Order = 2), Preserve]
        public string MessageId { get; set; }

        /// <inheritdoc />
        public bool Persistent => _persistentValue.HasValue ? _persistentValue.Value : _persistent;

        /// <inheritdoc />
        [DataMember(Name="room_name", Order = 10), Preserve]
        public string RoomName { get; set; }

        /// <inheritdoc />
        [DataMember(Name="sender_id", Order = 4), Preserve]
        public string SenderId { get; set; }

        /// <inheritdoc />
        public string UpdateTime => _updateTimeValue.HasValue ? _updateTimeValue.Value.ToString() : _updateTime.ToString();

        /// <inheritdoc />
        [DataMember(Name="user_id_one", Order = 12), Preserve]
        public string UserIdOne { get; set; }

        /// <inheritdoc />
        [DataMember(Name="user_id_two", Order = 13), Preserve]
        public string UserIdTwo { get; set; }

        /// <inheritdoc />
        [DataMember(Name="username", Order = 5), Preserve]
        public string Username { get; set; }

        [DataMember(Name="code"), Preserve]
        private int _code { get; set; }

        [DataMember(Order = 3), Preserve]
        private IntValue _codeValue;

        [DataMember(Name="create_time"), Preserve]
        private string _createTime;

        [DataMember(Order = 7), Preserve]
        private IntValue _createTimeValue;

        [DataMember(Name="persistent"), Preserve]
        private bool _persistent;

        [DataMember(Order = 9), Preserve]
        private BoolValue _persistentValue;

        [DataMember(Name="update_time"), Preserve]
        private int _updateTime;

        [DataMember(Order = 8), Preserve]
        private IntValue _updateTimeValue;

        public override string ToString()
        {
            var output = "";
            output = string.Concat(output, "ChannelId: ", ChannelId, ", ");
            output = string.Concat(output, "Code: ", _code, ", ");
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
