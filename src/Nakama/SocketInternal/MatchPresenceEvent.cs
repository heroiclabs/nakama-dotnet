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
    /// <inheritdoc cref="IMatchPresenceEvent"/>
    [DataContract]
    public class MatchPresenceEvent : IMatchPresenceEvent
    {
        public IEnumerable<IUserPresence> Joins => _joins ?? UserPresence.NoPresences;
        [DataMember(Name = "joins", Order = 1), Preserve] public List<UserPresence> _joins { get; set; }

        public IEnumerable<IUserPresence> Leaves => _leaves ?? UserPresence.NoPresences;
        [DataMember(Name = "leaves", Order = 2), Preserve] public List<UserPresence> _leaves { get; set; }

        [DataMember(Name = "match_id", Order = 3), Preserve] public string MatchId { get; set; }

        public override string ToString()
        {
            var joins = string.Join(", ", Joins);
            var leaves = string.Join(", ", Leaves);
            return $"MatchPresenceEvent(Joins=[{joins}], Leaves=[{leaves}], MatchId='{MatchId}')";
        }
    }

}
