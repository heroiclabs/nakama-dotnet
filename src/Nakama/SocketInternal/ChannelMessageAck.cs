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
    /// <inheritdoc cref="IChannelMessageAck"/>
    [DataContract]
    public class ChannelMessageAck : IChannelMessageAck
    {
        [DataMember(Name = "channel_id", Order = 1), Preserve]
        public string ChannelId { get; set; }

        public int Code => _codeValue.HasValue ? _codeValue.Value : _code;

        public string CreateTime => _createTimeValue.HasValue ? _createTimeValue.Value.ToString() : _createTime;

        [DataMember(Name = "message_id", Order = 2), Preserve]
        public string MessageId { get; set; }

        public bool Persistent => _persistentValue.HasValue ? _persistentValue.Value : _persistent;

        public string UpdateTime => _updateTimeValue.HasValue ? _updateTimeValue.Value.ToString() : _updateTime;

        [DataMember(Name = "username", Order = 4), Preserve]
        public string Username { get; set; }

        [DataMember(Name="room_name", Order = 8), Preserve]
        public string RoomName { get; set; }

        [DataMember(Name="group_id", Order = 9), Preserve]
        public string GroupId { get; set; }

        [DataMember(Name="user_id_one", Order = 10), Preserve]
        public string UserIdOne { get; set; }

        [DataMember(Name="user_id_two", Order = 11), Preserve]
        public string UserIdTwo { get; set; }

        [DataMember(Name = "code"), Preserve]
        private int _code;

        [DataMember(Order = 3), Preserve]
        private IntValue _codeValue;

        [DataMember(Name = "create_time"), Preserve]
        private string _createTime;

        [DataMember(Order = 5), Preserve]
        private IntValue _createTimeValue;

        [DataMember(Name = "persistent"), Preserve]
        private bool _persistent;

        [DataMember(Order = 7), Preserve]
        private BoolValue _persistentValue;

        [DataMember(Name = "update_time"), Preserve]
        private string _updateTime;

        [DataMember(Order = 6), Preserve]
        private IntValue _updateTimeValue;

        public override string ToString()
        {
            return
                $"ChannelMessageAck(ChannelId='{ChannelId}', Code={Code}, CreateTime={CreateTime}, MessageId='{MessageId}', Persistent={Persistent}, UpdateTime={UpdateTime}, Username='{Username}', RoomName='{RoomName}', GroupId='{GroupId}', UserIdOne='{UserIdOne}', UserIdTwo='{UserIdTwo}')";
        }
    }
}
