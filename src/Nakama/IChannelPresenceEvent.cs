/**
 * Copyright 2018 The Nakama Authors
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
    using System.Collections.Generic;

    /// <summary>
    /// A batch of join and leave presences on a chat channel.
    /// </summary>
    public interface IChannelPresenceEvent
    {
        /// <summary>
        /// The unique identifier of the chat channel.
        /// </summary>
        string ChannelId { get; }

        /// <summary>
        /// Presences of users who left the channel.
        /// </summary>
        IEnumerable<IUserPresence> Leaves { get; }

        /// <summary>
        /// Presences of the users who joined the channel.
        /// </summary>
        IEnumerable<IUserPresence> Joins { get; }
    }
}
