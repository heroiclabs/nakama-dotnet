/**
* Copyright 2021 The Nakama Authors
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

using System.Runtime.Serialization;

namespace Nakama
{
    /// <summary>
    /// Incoming information about a party.
    /// </summary>
    internal class Party : IParty
    {
        [DataMember(Name = "party_id"), Preserve]
        public string PartyId { get; set; }

        [DataMember(Name = "open"), Preserve]
        public bool Open { get; set; }

        [DataMember(Name = "max_size"), Preserve]
        public int MaxSize { get; set; }

        public IUserPresence Self => _self;

        [DataMember(Name = "self"), Preserve]
        private UserPresence _self;

        public IUserPresence Leader => _leader;

        [DataMember(Name = "leader"), Preserve]
        private UserPresence _leader;

        public IUserPresence[] Presences => _presences;

        [DataMember(Name = "presences"), Preserve]
        private UserPresence[] _presences;
    }
}
