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
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Some game state update in a match.
    /// </summary>
    public interface IMatchState
    {
        /// <summary>
        /// The unique match identifier.
        /// </summary>
        string MatchId { get; }

        /// <summary>
        /// The operation code for the state change.
        /// </summary>
        /// <remarks>
        /// This value can be used to mark the type of the contents of the state.
        /// </remarks>
        long OpCode { get; }

        /// <summary>
        /// The byte contents of the state change.
        /// </summary>
        byte[] State { get; }

        /// <summary>
        /// Information on the user who sent the state change.
        /// </summary>
        IUserPresence UserPresence { get; }
    }

    /// <inheritdoc />
    internal class MatchState : IMatchState
    {
        [DataMember(Name="match_id")]
        public string MatchId { get; set; }

        public long OpCode => System.Convert.ToInt64(_opCode);
        [DataMember(Name="op_code")]
        public string _opCode { get; set; }

        public byte[] State => Convert.FromBase64String(_state);
        [DataMember(Name="data")]
        public string _state { get; set; }

        public IUserPresence UserPresence => _userPresence;
        [DataMember(Name="presence")]
        public UserPresence _userPresence { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            var presences = string.Join(", ", UserPresence);
            return $"MatchState(MatchId={MatchId}, OpCode={OpCode}, State={State}, UserPresence={presences})";
        }
    }
}
