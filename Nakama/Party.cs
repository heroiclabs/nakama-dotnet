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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Nakama
{
    /// <summary>
    /// Incoming information about a party.
    /// </summary>
    internal class Party : IParty
    {
        [DataMember(Name = "party_id"), Preserve]
        public string Id { get; set; }

        [DataMember(Name = "open"), Preserve] public bool Open { get; set; }

        [DataMember(Name = "max_size"), Preserve]
        public int MaxSize { get; set; }

        public IUserPresence Self => SelfField;

        [DataMember(Name = "self"), Preserve] public UserPresence SelfField { get; set; }

        public IUserPresence Leader => LeaderField;

        [DataMember(Name = "leader"), Preserve]
        public UserPresence LeaderField { get; set; }

        public IEnumerable<IUserPresence> Presences => PresencesField ?? UserPresence.NoPresences;

        [DataMember(Name = "presences"), Preserve]
        public List<UserPresence> PresencesField { get; set; }


        public void UpdatePresences(IPartyPresenceEvent presenceEvent)
        {
            if (presenceEvent.PartyId != Id)
            {
                throw new InvalidOperationException("Tried updating presences belonging to the wrong party.");
            }

            PresencesField = PresenceUtil.CopyJoinsAndLeaves(PresencesField, presenceEvent.Joins, presenceEvent.Leaves);
        }
    }
}
