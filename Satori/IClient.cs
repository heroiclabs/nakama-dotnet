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
using System.Threading;
using System.Threading.Tasks;

namespace Satori
{
    /// <summary>
    /// A client for the API in Satori server.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// The host address of the server. Defaults to "127.0.0.1".
        /// </summary>
        string Host { get; }

        /// <summary>
        /// The port number of the server. Defaults to 7350.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// The protocol scheme used to connect with the server. Must be either "http" or "https".
        /// </summary>
        string Scheme { get; }

        /// <summary>
        /// Set the timeout in seconds on requests sent to the server.
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// Authenticate against the server.
        /// </summary>
        public async Task<IApiSession> AuthenticateAsync(
            string basicAuthUsername,
            string basicAuthPassword,
            ApiAuthenticateRequest body,
            CancellationToken? cancellationToken);

        /// <summary>
        /// Log out a session, invalidate a refresh token, or log out all sessions/refresh tokens for a user.
        /// </summary>
        public async Task AuthenticateLogoutAsync(
            string bearerToken,
            ApiAuthenticateLogoutRequest body,
            CancellationToken? cancellationToken);

        /// <summary>
        /// Refresh a user's session using a refresh token retrieved from a previous authentication request.
        /// </summary>
        public async Task<IApiSession> AuthenticateRefreshAsync(
            string basicAuthUsername,
            string basicAuthPassword,
            ApiAuthenticateRefreshRequest body,
            CancellationToken? cancellationToken);

        /// <summary>
        /// Publish an event for this session.
        /// </summary>
        public async Task EventAsync(
            string bearerToken,
            ApiEventRequest body,
            CancellationToken? cancellationToken);


        /// <summary>
        /// Get or list all available experiments for this identity.
        /// </summary>
        public async Task<IApiExperimentList> GetExperimentsAsync(
            string bearerToken,
            IEnumerable<string> names,
            CancellationToken? cancellationToken);

        /// <summary>
        /// List all available flags for this identity.
        /// </summary>
        public async Task<IApiFlagList> GetFlagsAsync(
            string bearerToken,
            IEnumerable<string> names,
            CancellationToken? cancellationToken);


        /// <summary>
        /// Enrich/replace the current session with new identifier.
        /// </summary>
        public async Task<IApiSession> IdentifyAsync(
            string bearerToken,
            ApiIdentifyRequest body,
            CancellationToken? cancellationToken);


        /// <summary>
        /// List available live events.
        /// </summary>
        public async Task<IApiLiveEventList> GetLiveEventsAsync(
            string bearerToken,
            IEnumerable<string> names,
            CancellationToken? cancellationToken);

        /// <summary>
        /// List properties associated with this identity.
        /// </summary>
        public async Task<IApiProperties> ListPropertiesAsync(
            string bearerToken,
            CancellationToken? cancellationToken);


        /// <summary>
        /// Update identity properties.
        /// </summary>
        public async Task UpdatePropertiesAsync(
            string bearerToken,
            ApiUpdatePropertiesRequest body,
            CancellationToken? cancellationToken);
	}
}