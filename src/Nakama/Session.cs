/**
 * Copyright 2019 The Nakama Authors
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

using System;
using System.Collections.Generic;
using Nakama.TinyJson;

namespace Nakama
{
    /// <inheritdoc cref="ISession"/>
    public class Session : ISession
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc cref="ISession.AuthToken"/>
        public string AuthToken { get; }

        /// <inheritdoc cref="ISession.Created"/>
        public bool Created { get; }

        /// <inheritdoc cref="ISession.CreateTime"/>
        public long CreateTime { get; }

        /// <inheritdoc cref="ISession.ExpireTime"/>
        public long ExpireTime { get; }

        /// <inheritdoc cref="ISession.IsExpired"/>
        public bool IsExpired => HasExpired(DateTime.UtcNow);

        /// <inheritdoc cref="ISession.Properties"/>
        public IDictionary<string, string> Properties { get; }

        /// <inheritdoc cref="ISession.Username"/>
        public string Username { get; }

        /// <inheritdoc cref="ISession.UserId"/>
        public string UserId { get; }

        /// <inheritdoc cref="ISession.HasExpired"/>
        public bool HasExpired(DateTime offset)
        {
            var expireDatetime = Epoch + TimeSpan.FromSeconds(ExpireTime);
            return offset > expireDatetime;
        }

        public override string ToString()
        {
            return
                $"Session(AuthToken='{AuthToken}', Created={Created}, CreateTime={CreateTime}, ExpireTime={ExpireTime}, Username='{Username}', UserId='{UserId}')";
        }

        internal Session(string authToken, bool created)
        {
            AuthToken = authToken;
            Created = created;
            var span = DateTime.UtcNow - Epoch;
            CreateTime = span.Seconds;
            Properties = new Dictionary<string, string>(); // TODO

            var json = JwtUnpack(authToken);
            var decoded = json.FromJson<Dictionary<string, object>>();
            ExpireTime = Convert.ToInt64(decoded["exp"]);
            Username = decoded["usn"].ToString();
            UserId = decoded["uid"].ToString();
        }

        /// <summary>
        /// Restore a session from the auth token.
        /// </summary>
        /// <param name="authToken">The authentication token to restore as a session.</param>
        /// <returns>A session.</returns>
        public static ISession Restore(string authToken)
        {
            return new Session(authToken, false);
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
