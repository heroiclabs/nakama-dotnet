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
    /// <inheritdoc cref="IClient"/>
    public class Client : IClient
    {
        /// <summary>
        /// The default expired timespan used to check session lifetime.
        /// </summary>
        public static TimeSpan DefaultExpiredTimeSpan = TimeSpan.FromMinutes(5);

        /// <inheritdoc cref="IClient"/>
        public bool AutoRefreshSession { get; }

        /// <summary>
        /// The host address of the server.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// The port number of the server.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// The protocol scheme used to connect with the server. Must be either "http" or "https".
        /// </summary>
        public string Scheme { get; }

        /// <summary>
        /// The key used to authenticate with the server without a session.
        /// </summary>
        public string ApiKey { get; }

        /// <summary>
        /// Set the timeout in seconds on requests sent to the server.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// The default timeout of the server.
        /// </summary>
        public const int DefaultTimeout = 15;

        private readonly ApiClient _apiClient;

        public Client(string scheme, string host, int port, string apiKey) : this(scheme, host, port, apiKey, HttpRequestAdapter.WithGzip(), true)
        {
        }

        public Client(string scheme, string host, int port, string apiKey, IHttpAdapter adapter, bool autoRefreshSession = true)
        {
            Host = host;
            Port = port;
            Scheme = scheme;
            ApiKey = apiKey;
            AutoRefreshSession = autoRefreshSession;
            _apiClient = new ApiClient(new UriBuilder(scheme, host, port).Uri, adapter, DefaultTimeout);
        }

        /// <inheritdoc cref="IClient.AuthenticateAsync"/>
        public async Task<ISession> AuthenticateAsync(
            string id,
            CancellationToken? cancellationToken = null)
            {
                var response = await _apiClient.SatoriAuthenticateAsync(ApiKey, string.Empty, new ApiAuthenticateRequest{Id = id}, cancellationToken);
                return new Session(response.Token, response.RefreshToken);
            }

        /// <inheritdoc cref="IClient.AuthenticateLogoutAsync"/>
        public Task AuthenticateLogoutAsync(
            ISession session,
            CancellationToken? cancellationToken = default)
            {
                return _apiClient.SatoriAuthenticateLogoutAsync(session.AuthToken, new ApiAuthenticateLogoutRequest{RefreshToken = session.RefreshToken, Token = session.AuthToken}, cancellationToken);
            }

        /// <inheritdoc cref="IClient.SessionRefreshAsync"/>
        public async Task<ISession> SessionRefreshAsync(
            ISession session,
            CancellationToken? cancellationToken = default)
        {
            var response = await _apiClient.SatoriAuthenticateRefreshAsync(ApiKey, string.Empty, new ApiAuthenticateRefreshRequest{RefreshToken = session.RefreshToken}, cancellationToken);

            if (session is Session updatedSession)
            {
                // Update session object in place if we can.
                updatedSession.Update(response.Token, response.RefreshToken);
                return updatedSession;
            }

            return new Session(response.Token, response.RefreshToken);
        }

        /// <inheritdoc cref="IClient.EventAsync"/>
        public async Task SendEventAsync(
            ISession session,
            Event @event,
            CancellationToken? cancellationToken = null)
            {
                if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                    session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
                {
                    await SessionRefreshAsync(session, cancellationToken);
                }

                var request = new ApiEventRequest{
                    _events = new List<ApiEvent>{
                        @event.ToApiEvent()
                    }
                };

                await _apiClient.SatoriEventAsync(session.AuthToken, request, cancellationToken);
            }

                    /// <inheritdoc cref="IClient.EventAsync"/>
        public async Task SendEventsAsync(
            ISession session,
            IEnumerable<Event> events,
            CancellationToken? cancellationToken = null)
            {
                if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                    session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
                {
                    await SessionRefreshAsync(session, cancellationToken);
                }

                var apiEventList = new List<ApiEvent>();

                foreach (Event e in events)
                {
                    apiEventList.Add(e.ToApiEvent());
                }

                var request = new ApiEventRequest{
                    _events = apiEventList
                };

                await _apiClient.SatoriEventAsync(session.AuthToken, request, cancellationToken);
            }

        /// <inheritdoc cref="IClient.GetExperimentsAsync"/>
        public async Task<IApiExperimentList> GetExperimentsAsync(
            ISession session,
            IEnumerable<string> names = null,
            CancellationToken? cancellationToken = default)
            {
                if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                    session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
                {
                    await SessionRefreshAsync(session, cancellationToken);
                }

                return await _apiClient.SatoriGetExperimentsAsync(session.AuthToken, names, cancellationToken);
            }

        /// <inheritdoc cref="IClient.GetFlagsAsync"/>
        public async Task<IApiFlagList> GetFlagsAsync(
            ISession session,
            IEnumerable<string> names = null,
            CancellationToken? cancellationToken = default)
            {
                if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                    session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
                {
                    await SessionRefreshAsync(session, cancellationToken);
                }

                return await _apiClient.SatoriGetFlagsAsync(session.AuthToken, string.Empty, string.Empty, names, cancellationToken);
            }

        /// <inheritdoc cref="IClient.GetFlagsDefaultAsync"/>
        public Task<IApiFlagList> GetFlagsDefaultAsync(
            string apiKey,
            IEnumerable<string> names = null,
            CancellationToken? cancellationToken = default)
            {
                return _apiClient.SatoriGetFlagsAsync(string.Empty, apiKey, string.Empty, names, cancellationToken);
            }

        /// <inheritdoc cref="IClient.IdentifyAsync"/>
        public async Task<ISession> IdentifyAsync(
            ISession session,
            string id,
            Dictionary<string, string> defaultProperties,
            Dictionary<string, string> customProperties,
            CancellationToken? cancellationToken = default)
            {
                if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                    session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
                {
                    await SessionRefreshAsync(session, cancellationToken);
                }

                var properties = new ApiProperties{_default = default, _custom = customProperties};
                var request = new ApiIdentifyRequest{Id = id, _default = defaultProperties, _custom = customProperties};
                var response = await _apiClient.SatoriIdentifyAsync(session.AuthToken, request, cancellationToken);
                return new Session(response.Token, response.RefreshToken);
            }


        /// <inheritdoc cref="IClient.GetLiveEventsAsync"/>
        public async Task<IApiLiveEventList> GetLiveEventsAsync(
            ISession session,
            IEnumerable<string> names = null,
            CancellationToken? cancellationToken = default)
            {
                if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                    session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
                {
                    await SessionRefreshAsync(session, cancellationToken);
                }

                return await _apiClient.SatoriGetLiveEventsAsync(session.AuthToken, names, cancellationToken);
            }

        /// <inheritdoc cref="IClient.ListPropertiesAsync"/>
        public async Task<IApiProperties> ListPropertiesAsync(
            ISession session,
            CancellationToken? cancellationToken = default)
            {
                if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                    session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
                {
                    await SessionRefreshAsync(session, cancellationToken);
                }

                return await _apiClient.SatoriListPropertiesAsync(session.AuthToken, cancellationToken);
            }

        /// <inheritdoc cref="IClient.UpdatePropertiesAsync"/>
        public async Task UpdatePropertiesAsync(
            ISession session,
            Dictionary<string, string> defaultProperties,
            Dictionary<string, string> customProperties,
            CancellationToken? cancellationToken = default)
            {
                if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                    session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
                {
                    await SessionRefreshAsync(session, cancellationToken);
                }

                await _apiClient.SatoriUpdatePropertiesAsync(session.AuthToken,
                new ApiUpdatePropertiesRequest{
                    _default = defaultProperties,
                    _custom = customProperties
                },  cancellationToken);
            }
	}
}
