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

using System;
using System.Runtime.Serialization;

namespace Nakama.SocketInternal
{
    /// <inheritdoc cref="IMatchState"/>
    [DataContract]
    public class MatchState : IMatchState
    {
        private static readonly byte[] NoBytes = new byte[0];

        [DataMember(Name = "match_id", Order = 1), Preserve]
        public string MatchId { get; set; }

        public long OpCode => Convert.ToInt64(_opCode);

        [DataMember(Name = "op_code", Order = 3), Preserve]
        private string _opCode { get; set; }

        public byte[] State => _state == null ? NoBytes : Convert.FromBase64String(_state);

        [DataMember(Name = "data", Order = 4), Preserve]
        private string _state;

        public IUserPresence UserPresence => _userPresence;

        [DataMember(Name = "presence", Order = 2), Preserve]
        private UserPresence _userPresence;

        public override string ToString()
        {
            return $"MatchState(MatchId='{MatchId}', OpCode={OpCode}, State='{_state}', UserPresence={UserPresence})";
        }
    }
}
