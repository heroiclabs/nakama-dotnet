// Copyright 2021 The Nakama Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Runtime.Serialization;

namespace Nakama
{
    /// <summary>
    /// Incoming party data delivered from the server.
    /// </summary>
    internal class PartyData : IPartyData
    {
        private static readonly byte[] NoBytes = new byte[0];

        [DataMember(Name = "party_id"), Preserve]
        public string PartyId { get; set; }

        public IUserPresence Presence => PresenceField;

        [DataMember(Name = "presence"), Preserve]
        public UserPresence PresenceField { get; set; }

        public long OpCode => Convert.ToInt64(OpCodeField);

        [DataMember(Name = "op_code"), Preserve]
        public string OpCodeField { get; set; }

        public byte[] Data => DataField == null ? NoBytes : Convert.FromBase64String(DataField);
        [DataMember(Name = "data"), Preserve] public string DataField { get; set; }

        public override string ToString() =>
            $"PartyData(PartyId='{PartyId}', Presence={Presence}, OpCode={OpCode}, Data={Data})";
    }
}
