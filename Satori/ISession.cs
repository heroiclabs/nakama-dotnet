// Copyright 2022 The Satori Authors
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

namespace Satori
{
    /// <summary>
    /// A session authenticated for a user with Satori server.
    /// </summary>
    public interface ISession
    {
        /// <summary>
        /// The authorization token used to construct this session.
        /// </summary>
        string AuthToken { get; }

        /// <summary>
        /// The UNIX timestamp when this session will expire.
        /// </summary>
        long ExpireTime { get; }

        /// <summary>
        /// If the session has expired.
        /// </summary>
        bool IsExpired { get; }

        /// <summary>
        /// If the refresh token has expired.
        /// </summary>
        bool IsRefreshExpired { get; }

        /// <summary>
        /// The UNIX timestamp when the refresh token will expire.
        /// </summary>
        long RefreshExpireTime { get; }

        /// <summary>
        /// Refresh token that can be used for session token renewal.
        /// </summary>
        string RefreshToken { get; }

        /// <summary>
        /// The ID of the user who owns this session.
        /// </summary>
        string IdentityId { get; }

        /// <summary>
        /// Check the session has expired against the offset time.
        /// </summary>
        /// <param name="offset">The datetime to compare against this session.</param>
        /// <returns>If the session has expired.</returns>
        bool HasExpired(DateTime offset);

        /// <summary>
        /// Check if the refresh token has expired against the offset time.
        /// </summary>
        /// <param name="offset">The datetime to compare against this refresh token.</param>
        /// <returns>If refresh token has expired.</returns>
        bool HasRefreshExpired(DateTime offset);
    }
}
