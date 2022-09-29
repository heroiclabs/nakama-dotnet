// Copyright 2019 The Satori Authors
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
using Satori.TinyJson;

namespace Satori
{
    /// <inheritdoc cref="ISession"/>
    public class Session : ISession
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc cref="ISession.AuthToken"/>
        public string AuthToken { get; private set; }

        /// <inheritdoc cref="ISession.ExpireTime"/>
        public long ExpireTime { get; private set; }

        /// <inheritdoc cref="ISession.IsExpired"/>
        public bool IsExpired => HasExpired(DateTime.UtcNow);

        /// <inheritdoc cref="IsRefreshExpired"/>
        public bool IsRefreshExpired => HasRefreshExpired(DateTime.UtcNow);

        /// <inheritdoc cref="ISession.RefreshExpireTime"/>
        public long RefreshExpireTime { get; private set; }

        /// <inheritdoc cref="ISession.RefreshToken"/>
        public string RefreshToken { get; private set; }

        /// <inheritdoc cref="ISession.IdentityId"/>
        public string IdentityId { get; private set; }

        /// <inheritdoc cref="ISession.HasExpired"/>
        public bool HasExpired(DateTime offset)
        {
            var expireDateTime = Epoch + TimeSpan.FromSeconds(ExpireTime);
            return offset > expireDateTime;
        }

        /// <inheritdoc cref="ISession.HasRefreshExpired"/>
        public bool HasRefreshExpired(DateTime offset)
        {
            var expireDateTime = Epoch + TimeSpan.FromSeconds(RefreshExpireTime);
            return offset > expireDateTime;
        }

        public override string ToString()
        {
            return
                $"Session(AuthToken='{AuthToken}', ExpireTime={ExpireTime}, RefreshToken={RefreshToken}, RefreshExpireTime={RefreshExpireTime}, UserId='{IdentityId}')";
        }

        internal Session(string authToken, string refreshToken)
        {
            RefreshExpireTime = 0L;
            Update(authToken, refreshToken);
        }

        /// <summary>
        /// Update the current session token with a new authorization token and refresh token.
        /// </summary>
        /// <param name="authToken">The authorization token to update into the session.</param>
        /// <param name="refreshToken">The refresh token to update into the session.</param>
        internal void Update(string authToken, string refreshToken)
        {
            AuthToken = authToken;
            RefreshToken = refreshToken;

            var json = JwtUnpack(authToken);
            var decoded = json.FromJson<Dictionary<string, object>>();
            ExpireTime = Convert.ToInt64(decoded["exp"]);
            IdentityId = decoded["iid"].ToString();

            // Check in case clients have not updated to use refresh tokens yet.
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var json2 = JwtUnpack(refreshToken);
                var decoded2 = json2.FromJson<Dictionary<string, object>>();
                RefreshExpireTime = Convert.ToInt64(decoded2["exp"]);
            }
        }

        /// <summary>
        /// Restore a session from the auth token.
        /// </summary>
        /// <remarks>
        /// A <c>null</c> or empty authentication token will return null.
        /// </remarks>
        /// <param name="authToken">The authorization token to restore as a session.</param>
        /// <param name="refreshToken">The refresh token for the session.</param>
        /// <returns>A session.</returns>
        public static ISession Restore(string authToken, string refreshToken = null)
        {
            return string.IsNullOrEmpty(authToken) ? null : new Session(authToken, refreshToken);
        }

        private static string JwtUnpack(string jwt)
        {
            // Hack decode JSON payload from JWT.
            var payload = jwt.Split('.')[1];
            var padLength = Math.Ceiling(payload.Length / 4.0) * 4;
            payload = payload.PadRight(Convert.ToInt32(padLength), '=').Replace('-', '+').Replace('_', '/');
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        }
    }
}
