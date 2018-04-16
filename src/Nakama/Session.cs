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

    /// <inheritdoc />
    public class Session : ISession
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc />
        public string AuthToken { get; }

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
            var expireDatetime = Epoch - TimeSpan.FromSeconds(ExpireTime);
            return dateTime > expireDatetime;
        }

        private Session(string authToken)
        {
            var span = DateTime.UtcNow - Epoch;
            CreateTime = span.Seconds;
            AuthToken = authToken;

            // Hack extract fields from JSON.
            var decoded = JwtUnpack(authToken);
            var expireTime = decoded.Split('"')[2].TrimStart(':').TrimEnd(',');
            ExpireTime = Convert.ToInt64(expireTime);
            Username = decoded.Split('"')[9];
            UserId = decoded.Split('"')[5];
        }

        public override string ToString()
        {
            return $"ExpireTime: {ExpireTime}, IsExpired: {IsExpired}, Username: {Username}, UserId: {UserId}";
        }

        /// <summary>
        /// Restore a session from an authentication token.
        /// </summary>
        /// <param name="authToken">The authentication token from a <c>Session</c>.</param>
        /// <returns>A session restored from the authentication token.</returns>
        public static Session Restore(string authToken)
        {
            return new Session(authToken);
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
