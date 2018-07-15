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
    using System;

    /// <summary>
    /// A session used with requests sent to Nakama server.
    /// </summary>
    public interface ISession
    {
        /// <summary>
        /// The authentication token used to construct this session.
        /// </summary>
        string AuthToken { get; }

        /// <summary>
        /// <c>True</c> if the user account for this session was just created.
        /// </summary>
        bool Created { get; }

        /// <summary>
        /// The timestamp in seconds when this session object was created.
        /// </summary>
        long CreateTime { get; }

        /// <summary>
        /// The timestamp in seconds when this session will expire.
        /// </summary>
        long ExpireTime { get; }

        /// <summary>
        /// <c>True</c> if the session has expired against the current time.
        /// </summary>
        bool IsExpired { get; }

        /// <summary>
        /// The username of the user who owns this session.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// The ID of the user who owns this session.
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Check if the session has expired against the input time.
        /// </summary>
        /// <param name="dateTime">The time to compare against the session.</param>
        /// <returns><c>true</c> if the session has expired.</returns>
        bool HasExpired(DateTime dateTime);
    }
}
