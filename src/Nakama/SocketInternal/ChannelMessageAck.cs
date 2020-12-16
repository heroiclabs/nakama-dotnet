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
        [DataMember(Name = "channel_id", Order = 1), Preserve] public string ChannelId { get; set; }

        [DataMember(Name = "code", Order = 3), Preserve] public int Code { get; set; }

        [DataMember(Name = "create_time", Order = 5), Preserve] public string CreateTime { get; set; }

        [DataMember(Name = "message_id", Order = 2), Preserve] public string MessageId { get; set; }

        [DataMember(Name = "persistent", Order = 7), Preserve] public bool Persistent { get; set; }

        [DataMember(Name = "update_time", Order = 6), Preserve] public string UpdateTime { get; set; }

        [DataMember(Name = "username", Order = 4), Preserve] public string Username { get; set; }

        [DataMember(Name="room_name", Order = 8), Preserve] public string RoomName { get; set; }

        [DataMember(Name="group_id", Order = 9), Preserve] public string GroupId { get; set; }

        [DataMember(Name="user_id_one", Order = 10), Preserve] public string UserIdOne { get; set; }

        [DataMember(Name="user_id_two", Order = 11), Preserve] public string UserIdTwo { get; set; }

        public override string ToString()
        {
            return
                $"ChannelMessageAck(ChannelId='{ChannelId}', Code={Code}, CreateTime={CreateTime}, MessageId='{MessageId}', Persistent={Persistent}, UpdateTime={UpdateTime}, Username='{Username}', RoomName='{RoomName}', GroupId='{GroupId}', UserIdOne='{UserIdOne}', UserIdTwo='{UserIdTwo}')";
        }
    }
}
