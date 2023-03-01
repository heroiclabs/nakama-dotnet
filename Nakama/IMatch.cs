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

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Nakama
{
    /// <summary>
    /// A multiplayer match.
    /// </summary>
    public interface IMatch
    {
        /// <summary>
        /// If this match has an authoritative handler on the server.
        /// </summary>
        bool Authoritative { get; }

        /// <summary>
        /// The unique match identifier.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// A label for the match which can be filtered on.
        /// </summary>
        string Label { get; }

        /// <summary>
        /// The presences already in the match.
        /// </summary>
        IEnumerable<IUserPresence> Presences { get; }

        /// <summary>
        /// The number of users currently in the match.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// The current user in this match. i.e. Yourself.
        /// </summary>
        IUserPresence Self { get; }

        /// <summary>
        /// Apply the joins and leaves from a presence event to the presences tracked by the match.
        /// </summary>
        void UpdatePresences(IMatchPresenceEvent presenceEvent);
    }

    /// <inheritdoc cref="IMatch"/>
    internal class Match : IMatch
    {
        [DataMember(Name = "authoritative"), Preserve] public bool Authoritative { get; set; }

        [DataMember(Name = "match_id"), Preserve] public string Id { get; set; }

        [DataMember(Name = "label"), Preserve] public string Label { get; set; }

        public IEnumerable<IUserPresence> Presences => _presences ?? UserPresence.NoPresences;
        [DataMember(Name = "presences"), Preserve] public List<UserPresence> _presences { get; set; }

        [DataMember(Name = "size"), Preserve] public int Size { get; set; }

        public IUserPresence Self => _self;
        [DataMember(Name = "self"), Preserve] public UserPresence _self { get; set; }

        public override string ToString()
        {
            var presences = string.Join(", ", Presences);
            return
                $"Match(Authoritative={Authoritative}, Id='{Id}', Label='{Label}', Presences=[{presences}], Size={Size}, Self={Self})";
        }

        public void UpdatePresences(IMatchPresenceEvent presenceEvent)
        {
            if (presenceEvent.MatchId != Id)
            {
                throw new InvalidOperationException("Tried updating presences belonging to the wrong match.");
            }

            _presences = PresenceUtil.CopyJoinsAndLeaves(_presences, presenceEvent.Joins, presenceEvent.Leaves);
        }
    }
}
