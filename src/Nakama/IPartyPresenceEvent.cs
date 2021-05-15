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
    /// Presence update for a particular party.
    /// </summary>
    public interface IPartyPresenceEvent
    {
        /// <summary>
        /// The ID of the party.
        /// </summary>
        string PartyId { get; }

        /// <summary>
        /// The user presences that have just joined the party.
        /// </summary>
        IEnumerable<IUserPresence> Joins { get; }

        /// <summary>
        /// The user presences that have just left the party.
        /// </summary>
        IEnumerable<IUserPresence> Leaves { get; }
    }
}
