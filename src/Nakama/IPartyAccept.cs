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

namespace Nakama
{
    // Accept a request to join.
    public interface IPartyAccept
    {
        /// <summary>
        /// Party ID to accept a join request for.
        /// </summary>
        string PartyId { get; set; }

        /// <summary>
        /// The presence to accept as a party member.
        /// </summary>
        IUserPresence Presence { get; }
    }
}
