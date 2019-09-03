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
    /// Remove a message from a chat channel.
    /// </summary>
    internal class ChannelRemoveMessage
    {
        [DataMember(Name="channel_id")]
        public string ChannelId { get; set; }

        [DataMember(Name="message_id")]
        public string MessageId { get; set; }
        
        [DataMember(Name="room_name")]
        public string RoomName { get; set; }
        
        [DataMember(Name="group_id")]
        public string GroupId { get; set; }
        
        [DataMember(Name="user_id_one")]
        public string UserIdOne { get; set; }
        
        [DataMember(Name="user_id_two")]
        public string UserIdTwo { get; set; }

        public override string ToString()
        {
            return $"ChannelRemoveMessage(ChannelId='{ChannelId}', MessageId='{MessageId}', RoomName='{RoomName}', GroupId='{GroupId}', UserIdOne='{UserIdOne}', UserIdTwo='{UserIdTwo}')";
        }
    }
}
