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

namespace Nakama
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

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
    }

    /// <inheritdoc />
    internal class ChannelPresenceEvent : IChannelPresenceEvent
    {
        [DataMember(Name="channel_id")]
        public string ChannelId { get; set; }

        public IEnumerable<IUserPresence> Joins => _joins ?? new List<UserPresence>(0);
        [DataMember(Name="joins")]
        public List<UserPresence> _joins { get; set; }

        public IEnumerable<IUserPresence> Leaves => _leaves ?? new List<UserPresence>(0);
        [DataMember(Name="leaves")]
        public List<UserPresence> _leaves { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            var joins = string.Join(",", Joins);
            var leaves = string.Join(",", Leaves);
            return $"ChannelPresenceEvent[ChannelId={ChannelId}, Joins=({joins}), Leaves=({leaves})]";
        }
    }
}
