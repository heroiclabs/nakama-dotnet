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
    /// <inheritdoc cref="IMatchmakerMatched"/>
    public class MatchmakerMatched : IMatchmakerMatched
    {
        [DataMember(Name = "match_id"), Preserve] public string MatchId { get; set; }

        [DataMember(Name = "ticket"), Preserve] public string Ticket { get; set; }

        [DataMember(Name = "token"), Preserve] public string Token { get; set; }

        public IEnumerable<IMatchmakerUser> Users => _users ?? new List<MatchmakerUser>(0);
        [DataMember(Name = "users"), Preserve] public List<MatchmakerUser> _users { get; set; }

        public IMatchmakerUser Self => _self;
        [DataMember(Name = "self"), Preserve] public MatchmakerUser _self { get; set; }

        public override string ToString()
        {
            var users = string.Join(", ", Users);
            return
                $"MatchmakerMatched(MatchId='{MatchId}', Ticket='{Ticket}', Token='{Token}', Users=[{users}], Self={Self})";
        }
    }

    /// <inheritdoc cref="IMatchmakerUser"/>
    public class MatchmakerUser : IMatchmakerUser
    {
        public IDictionary<string, double> NumericProperties => _numericProperties ?? new Dictionary<string, double>();

        [DataMember(Name = "numeric_properties"), Preserve]
        public Dictionary<string, double> _numericProperties { get; set; }

        public IUserPresence Presence => _presence;
        [DataMember(Name = "presence"), Preserve] public UserPresence _presence { get; set; }

        public IDictionary<string, string> StringProperties => _stringProperties ?? new Dictionary<string, string>();

        [DataMember(Name = "string_properties"), Preserve]
        public Dictionary<string, string> _stringProperties { get; set; }

        public override string ToString()
        {
            return
                $"MatchmakerUser(NumericProperties={NumericProperties}, Presence={Presence}, StringProperties={StringProperties})";
        }
    }
}
