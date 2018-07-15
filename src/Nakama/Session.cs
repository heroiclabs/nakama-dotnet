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
    using System.Text;
    using System.Collections.Generic;
    using TinyJson;

    /// <inheritdoc />
    public class Session : ISession
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc />
        public string AuthToken { get; }

        /// <inheritdoc />
        public bool Created { get; }

        /// <inheritdoc />
        public long CreateTime { get; }

        /// <inheritdoc />
        public long ExpireTime { get; }

        /// <inheritdoc />
        public bool IsExpired => HasExpired(DateTime.UtcNow);

        /// <inheritdoc />
        public string Username { get; }

        /// <inheritdoc />
        public string UserId { get; }

        /// <inheritdoc />
        public bool HasExpired(DateTime dateTime)
        {
            var expireDatetime = Epoch + TimeSpan.FromSeconds(ExpireTime);
            return dateTime > expireDatetime;
        }

        private Session(string authToken, bool created)
        {
            var span = DateTime.UtcNow - Epoch;
            Created = created;
            CreateTime = span.Seconds;
            AuthToken = authToken;

            var json = JwtUnpack(authToken);
            var decoded = json.FromJson<Dictionary<string, object>>();
            ExpireTime = Convert.ToInt64(decoded["exp"]);
            Username = decoded["usn"].ToString();
            UserId = decoded["uid"].ToString();
        }

        public override string ToString()
        {
            return $"Session(Created={Created}, ExpireTime={ExpireTime}, IsExpired={IsExpired}, Username={Username}, UserId={UserId})";
        }

        /// <summary>
        /// Restore a session from an authentication token.
        /// </summary>
        /// <param name="authToken">The authentication token from a <c>Session</c>.</param>
        /// <returns>A session restored from the authentication token.</returns>
        public static ISession Restore(string authToken)
        {
            return Restore(authToken, false);
        }

        internal static ISession Restore(string authToken, bool created)
        {
            return new Session(authToken, created);
        }

        private static string JwtUnpack(string jwt)
        {
            // Hack decode JSON payload from JWT.
            var payload = jwt.Split('.')[1];

            var padLength = Math.Ceiling(payload.Length / 4.0) * 4;
            payload = payload.PadRight(Convert.ToInt32(padLength), '=');

            return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        }
    }
}
