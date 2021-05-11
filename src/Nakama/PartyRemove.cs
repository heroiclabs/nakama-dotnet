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
    /// Kick a party member, or decline a request to join.
    /// </summary>
    internal class PartyRemove : IPartyRemove
    {
        [DataMember(Name = "party_id")]
        public string PartyId { get; set; }

        public IUserPresence Presence
        {
            get => _presence;
            set => _presence = value;
        }

        [DataMember(Name = "presence")]
        private IUserPresence _presence;
    }
}
