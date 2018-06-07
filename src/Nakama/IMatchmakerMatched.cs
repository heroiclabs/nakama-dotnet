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
    /// The result of a successful matchmaker operation sent to the server.
    /// </summary>
    public interface IMatchmakerMatched
    {
        /// <summary>
        /// The id used to join the match.
        /// </summary>
        /// <remarks>
        /// A match ID used to join the match.
        /// </remarks>
        string MatchId { get; }

        /// <summary>
        /// The ticket sent by the server when the user requested to matchmake for other players.
        /// </summary>
        string Ticket { get; }

        /// <summary>
        /// The token used to join a match.
        /// </summary>
        string Token { get; }

        /// <summary>
        /// The other users matched with this user and the parameters they sent.
        /// </summary>
        IEnumerable<IMatchmakerUser> Users { get; }

        /// <summary>
        /// The current user who matched with opponents.
        /// </summary>
        IMatchmakerUser Self { get; }
    }

    /// <summary>
    /// The user with the parameters they sent to the server when asking for opponents.
    /// </summary>
    public interface IMatchmakerUser
    {
        /// <summary>
        /// The numeric properties which this user asked to matchmake with.
        /// </summary>
        IDictionary<string, double> NumericProperties { get; }

        /// <summary>
        /// The presence of the user.
        /// </summary>
        IUserPresence Presence { get; }

        /// <summary>
        /// The string properties which this user asked to matchmake with.
        /// </summary>
        IDictionary<string, string> StringProperties { get; }
    }

    /// <inheritdoc />
    internal class MatchmakerMatched : IMatchmakerMatched
    {
        [DataMember(Name="match_id")]
        public string MatchId { get; set; }

        [DataMember(Name="ticket")]
        public string Ticket { get; set; }

        [DataMember(Name="token")]
        public string Token { get; set; }

        public IEnumerable<IMatchmakerUser> Users => _users ?? new List<MatchmakerUser>(0);
        [DataMember(Name="users")]
        public List<MatchmakerUser> _users { get; set; }

        public IMatchmakerUser Self => _self;
        [DataMember(Name="self")]
        public MatchmakerUser _self { get; set; }
    }

    /// <inheritdoc />
    internal class MatchmakerUser : IMatchmakerUser
    {
        public IDictionary<string, double> NumericProperties => _numericProperties ?? new Dictionary<string, double>();
        [DataMember(Name="numeric_properties")]
        public Dictionary<string, double> _numericProperties { get; set; }

        public IUserPresence Presence => _presence;
        [DataMember(Name="presence")]
        public UserPresence _presence { get; set; }

        public IDictionary<string, string> StringProperties => _stringProperties ?? new Dictionary<string, string>();
        [DataMember(Name="string_properties")]
        public Dictionary<string, string> _stringProperties { get; set; }
    }
}
