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

using System.Collections.Generic;

namespace Nakama
{
    /// <summary>
    /// Begin matchmaking as a party.
    public interface IPartyMatchmakerAdd
    {
        /// <summary>
        /// Party ID.
        /// </summary>
        string PartyId { get; }

        /// <summary>
        /// Minimum total user count to match together.
        /// </summary>
        int MinCount { get; }

        /// <summary>
        /// Maximum total user count to match together.
        /// </summary>
        int MaxCount { get; }

        /// <summary>
        /// Filter query used to identify suitable users.
        /// </summary>
        string Query { get; }

        /// <summary>
        /// String properties.
        /// </summary>
        Dictionary<string, string> StringProperties { get; }

        /// <summary>
        /// Numeric properties.
        /// </summary>
        Dictionary<string, double> NumericProperties { get; }
    }
}
