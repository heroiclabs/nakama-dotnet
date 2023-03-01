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

using System.Collections.Generic;

namespace Nakama
{
    /// <summary>
    /// Incoming information about a party.
    /// </summary>
    public interface IParty
    {
        /// <summary>
        /// The unique party identifier.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// True, if the party is open to join.
        /// </summary>
        bool Open { get; }

        /// <summary>
        /// The maximum number of party members.
        /// </summary>
        int MaxSize { get; }

        /// <summary>
        /// The current user in this party. i.e. Yourself.
        /// </summary>
        IUserPresence Self { get; }

        /// <summary>
        /// The current party leader.
        /// </summary>
        IUserPresence Leader { get; }

        /// <summary>
        /// All members currently in the party.
        /// </summary>
        IEnumerable<IUserPresence> Presences { get; }

        /// <summary>
        /// Apply the joins and leaves from a presence event to the presences tracked by the party.
        /// </summary>
        void UpdatePresences(IPartyPresenceEvent presenceEvent);
    }
}
