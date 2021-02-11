

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
    /// <inheritdoc cref="IChannel"/>
    [DataContract]
    public class Channel : IChannel
    {
        [DataMember(Name="id", Order = 1), Preserve]
        public string Id { get; set; }

        public IEnumerable<IUserPresence> Presences => _presences ?? UserPresence.NoPresences;
        [DataMember(Name="presences", Order = 2), Preserve]
        public List<UserPresence> _presences { get; set; }

        public IUserPresence Self => _self;
        [DataMember(Name="self", Order = 3), Preserve]
        public UserPresence _self { get; set; }

        [DataMember(Name="room_name", Order = 4), Preserve]
        public string RoomName { get; set; }

        [DataMember(Name="group_id", Order = 5), Preserve]
        public string GroupId { get; set; }

        [DataMember(Name="user_id_one", Order = 6), Preserve]
        public string UserIdOne { get; set; }

        [DataMember(Name="user_id_two", Order = 7), Preserve]
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
