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
        /// Authenticate against the server.
        /// </summary>
        /// <param name="id">An optional user id.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a user session.</returns>
        public Task<ISession> AuthenticateAsync(
            string id,
            CancellationToken? cancellationToken = default);

        /// <summary>
        /// Log out a session, invalidate a refresh token, or log out all sessions/refresh tokens for a user.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        public Task AuthenticateLogoutAsync(
            ISession session,
            CancellationToken? cancellationToken);

        /// <summary>
        /// Refresh a user's session using a refresh token retrieved from a previous authentication request.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a user session.</returns>
        public Task<ISession> AuthenticateRefreshAsync(
            ISession session,
            CancellationToken? cancellationToken = default);

        /// <summary>
        /// Publish an event for this session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="name">The name of the event.</param>
        /// <param name="defaultProperties">The default event properties.</param>
        /// <param name="customProperties">The custom event properties.</param>
        /// <param name="timestamp">The time when the event was triggered.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        public Task EventAsync(
            ISession session,
            string name,
            Dictionary<string, string> defaultProperties,
            Dictionary<string, string> customProperties,
            string timestamp,
            CancellationToken? cancellationToken = default);

        /// <summary>
        /// Get all experiments data.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="names">Experiment names; if empty string all experiments are returned.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to all experiments that this identity is involved with.</returns>
        public Task<IApiExperimentList> GetExperimentsAsync(
            ISession session,
            IEnumerable<string> names,
            CancellationToken? cancellationToken);

        /// <summary>
        /// List all available flags for this identity.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="names"> Flag names; if empty string all flags are returned. </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to all flags available to this identity.</returns>
        public Task<IApiFlagList> GetFlagsAsync(
            ISession session,
            IEnumerable<string> names,
            CancellationToken? cancellationToken = default);

        /// <summary>
        /// List all available flags for this identity.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id"> Identity ID to enrich the current session and return a new session. Old session will no longer be usable.</param>
        /// <param name="defaultProperties">The default event properties.</param>
        /// <param name="customProperties">The custom event properties.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the session of the user.</returns>
        public Task<ISession> IdentifyAsync(
            ISession session,
            string id,
            Dictionary<string, string> defaultProperties,
            Dictionary<string, string> customProperties,
            CancellationToken? cancellationToken = default);

        /// <summary>
        /// List available live events.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="names">Live event names; if null or empty, all live events are returned.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a list of live events.</returns>
        public Task<IApiLiveEventList> GetLiveEventsAsync(
            ISession session,
            IEnumerable<string> names = null,
            CancellationToken? cancellationToken = default);

        /// <summary>
        /// List properties associated with this identity.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a list of live events.</returns>
        public Task<IApiProperties> ListPropertiesAsync(
            ISession session,
            CancellationToken? cancellationToken = default);

        /// <summary>
        /// Update properties associated with this identity.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="defaultProperties">The default event properties.</param>
        /// <param name="customProperties">The custom event properties.</param>
        /// <param name="cancellationToken">The session of the user.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        public Task UpdatePropertiesAsync(
            ISession session,
            Dictionary<string, string> defaultProperties,
            Dictionary<string, string> customProperties,
            CancellationToken? cancellationToken = default);
    }
}