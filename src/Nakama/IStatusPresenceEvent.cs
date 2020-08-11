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
    /// A status update event about other users who've come online or gone offline.
    /// </summary>
    public interface IStatusPresenceEvent
    {
        /// <summary>
        /// Presences of users who left the server.
        /// </summary>
        /// <remarks>
        /// This leave information is in response to a subscription made to be notified when a user goes offline.
        /// </remarks>
        IEnumerable<IUserPresence> Leaves { get; }

        /// <summary>
        /// Presences of users who joined the server.
        /// </summary>
        /// <remarks>
        /// This join information is in response to a subscription made to be notified when a user comes online.
        /// </remarks>
        IEnumerable<IUserPresence> Joins { get; }
    }

    /// <inheritdoc cref="IStatusPresenceEvent"/>
    internal class StatusPresenceEvent : IStatusPresenceEvent
    {
        public IEnumerable<IUserPresence> Leaves => _leaves ?? UserPresence.NoPresences;
        [DataMember(Name = "leaves"), Preserve] public List<UserPresence> _leaves { get; set; }

        public IEnumerable<IUserPresence> Joins => _joins ?? UserPresence.NoPresences;
        [DataMember(Name = "joins"), Preserve] public List<UserPresence> _joins { get; set; }

        public override string ToString()
        {
            var joins = string.Join(", ", Joins);
            var leaves = string.Join(", ", Leaves);
            return $"StatusPresenceEvent(Leaves=[{leaves}], Joins=[{joins}])";
        }
    }
}
