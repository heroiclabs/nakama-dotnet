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
/// <inheritdoc cref="IChannelPresenceEvent"/>
    [DataContract]
    public class ChannelPresenceEvent : IChannelPresenceEvent
    {
        [DataMember(Name="channel_id", Order = 1), Preserve]
        public string ChannelId { get; set; }

        public IEnumerable<IUserPresence> Joins => _joins ?? new List<UserPresence>(0);
        [DataMember(Name="joins", Order = 2), Preserve]
        public List<UserPresence> _joins { get; set; }

        public IEnumerable<IUserPresence> Leaves => _leaves ?? new List<UserPresence>(0);
        [DataMember(Name="leaves", Order = 3), Preserve]
        public List<UserPresence> _leaves { get; set; }

        [DataMember(Name="room_name", Order = 4), Preserve]
        public string RoomName { get; set; }

        [DataMember(Name="group_id", Order= 5), Preserve]
        public string GroupId { get; set; }

        [DataMember(Name="user_id_one", Order = 6), Preserve]
        public string UserIdOne { get; set; }

        [DataMember(Name="user_id_two", Order = 7), Preserve]
        public string UserIdTwo { get; set; }

        public override string ToString()
        {
            var joins = string.Join(",", Joins);
            var leaves = string.Join(",", Leaves);
            return $"ChannelPresenceEvent(ChannelId='{ChannelId}', Joins=[{joins}], Leaves=[{leaves}], RoomName='{RoomName}', GroupId='{GroupId}', UserIdOne='{UserIdOne}', UserIdTwo='{UserIdTwo}')";
        }
    }
}

