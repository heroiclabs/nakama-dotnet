/**
 * Copyright 2020 The Nakama Authors
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
    /// The available channel types on the server.
    /// </summary>
    public enum ChannelType : uint
    {
        /// <summary>
        /// A chat room which can be created dynamically with a name.
        /// </summary>
        Room = 1,
        /// <summary>
        /// A private chat between two users.
        /// </summary>
        DirectMessage = 2,
        /// <summary>
        /// A chat within a group on the server.
        /// </summary>
        Group = 3
    }
}
