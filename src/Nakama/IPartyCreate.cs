

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
    /// <summary>
    /// Create a party.
    /// </summary>
    public interface IPartyCreate
    {
        /// <summary>
        /// Whether or not the party will require join requests to be approved by the party leader.
        /// </summary>
        bool Open { get; }

        /// <summary>
        /// Maximum number of party members.
        /// </summary>
        int MaxSize { get; }
    }
}
