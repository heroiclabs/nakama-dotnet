// Copyright 2019 The Nakama Authors
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
using Nakama.TinyJson;

namespace Nakama
{
    /// <inheritdoc cref="ISession"/>
    public class Session : ISession
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc cref="ISession.AuthToken"/>
        public string AuthToken { get; private set; }

        /// <inheritdoc cref="ISession.Created"/>
        public bool Created { get; }

        /// <inheritdoc cref="ISession.CreateTime"/>
        public long CreateTime { get; }

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

        /// <inheritdoc cref="ISession.Vars"/>
        public IDictionary<string, string> Vars { get; }

        /// <inheritdoc cref="ISession.Username"/>
        public string Username { get; private set; }

        /// <inheritdoc cref="ISession.UserId"/>
        public string UserId { get; private set; }

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
            var variables = "{";
            foreach (var variable in Vars)
            {
                variables = string.Concat(variables, " '", variable.Key, "': '", variable.Value, "', ");
            }
            variables = string.Concat(variables, "}");
            return
                $"Session(AuthToken='{AuthToken}', Created={Created}, CreateTime={CreateTime}, ExpireTime={ExpireTime}, RefreshToken={RefreshToken}, RefreshExpireTime={RefreshExpireTime}, Variables={variables}, Username='{Username}', UserId='{UserId}')";
        }

        internal Session(string authToken, string refreshToken, bool created)
        {
            Created = created;
            var span = DateTime.UtcNow - Epoch;
            CreateTime = span.Seconds;
            RefreshExpireTime = 0L;
            Vars = new Dictionary<string, string>();

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
            Username = decoded["usn"].ToString();
            UserId = decoded["uid"].ToString();
            if (decoded.ContainsKey("vrs") && decoded["vrs"] is Dictionary<string, object> dictionary)
            {
                foreach (var variable in dictionary)
                {
                    Vars.Add(variable.Key, variable.Value.ToString());
                }
            }

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
            return string.IsNullOrEmpty(authToken) ? null : new Session(authToken, refreshToken, false);
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
