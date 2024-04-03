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
    /// <inheritdoc cref="IClient" />
    public class Client : IClient
    {
        /// <summary>
        /// The default expired timespan used to check session lifetime.
        /// </summary>
        public static TimeSpan DefaultExpiredTimeSpan = TimeSpan.FromMinutes(5);

        /// <inheritdoc cref="IClient.AutoRefreshSession" />
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
        private readonly TransientExceptionDelegate _transientExceptionDelegate;

        public Client(string scheme, string host, int port, string apiKey) : this(scheme, host, port, apiKey,
            HttpRequestAdapter.WithGzip())
        {
        }

        public Client(string scheme, string host, int port, string apiKey, IHttpAdapter adapter,
            bool autoRefreshSession = true)
        {
            Host = host;
            Port = port;
            Scheme = scheme;
            ApiKey = apiKey;
            AutoRefreshSession = autoRefreshSession;
            _apiClient = new ApiClient(new UriBuilder(scheme, host, port).Uri, adapter, DefaultTimeout);

            _transientExceptionDelegate = adapter.TransientExceptionDelegate ??
                                          throw new ArgumentException(
                                              "HttpAdapter supplied null transient exception delegate.");
        }

        /// <inheritdoc cref="AuthenticateAsync" />
        public async Task<ISession> AuthenticateAsync(string id, Dictionary<string, string> defaultProperties = default, Dictionary<string, string> customProperties = default, CancellationToken? cancellationToken = default)
        {
            var resp = await _apiClient.SatoriAuthenticateAsync(ApiKey, string.Empty,
                new ApiAuthenticateRequest { Id = id, _default = defaultProperties, _custom = customProperties }, cancellationToken);
            return new Session(resp.Token, resp.RefreshToken);
        }

        /// <inheritdoc cref="AuthenticateLogoutAsync" />
        public Task AuthenticateLogoutAsync(ISession session, CancellationToken? cancellationToken = default) =>
            _apiClient.SatoriAuthenticateLogoutAsync(session.AuthToken,
                new ApiAuthenticateLogoutRequest { RefreshToken = session.RefreshToken, Token = session.AuthToken },
                cancellationToken);

        /// <inheritdoc cref="EventAsync" />
        public async Task EventAsync(ISession session, Event @event, CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            var request = new ApiEventRequest
            {
                _events = new List<ApiEvent>
                {
                    @event.ToApiEvent()
                }
            };

            await _apiClient.SatoriEventAsync(session.AuthToken, request, cancellationToken);
        }

        /// <inheritdoc cref="EventsAsync" />
        public async Task EventsAsync(ISession session, IEnumerable<Event> events,
            CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            var apiEventList = new List<ApiEvent>();
            foreach (var evt in events)
            {
                apiEventList.Add(evt.ToApiEvent());
            }

            var request = new ApiEventRequest
            {
                _events = apiEventList
            };

            await _apiClient.SatoriEventAsync(session.AuthToken, request, cancellationToken);
        }

        /// <inheritdoc cref="GetExperimentsAsync" />
        public async Task<IApiExperimentList> GetAllExperimentsAsync(ISession session, CancellationToken? cancellationToken = default)
        {
            return await GetExperimentsAsync(session, null, cancellationToken);
        }

        /// <inheritdoc cref="GetExperimentsAsync" />
        public async Task<IApiExperimentList> GetExperimentsAsync(ISession session, IEnumerable<string> names,
            CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            return await _apiClient.SatoriGetExperimentsAsync(session.AuthToken, names, cancellationToken);
        }

        /// <inheritdoc cref="GetFlagAsync(Satori.ISession,string,System.Nullable{System.Threading.CancellationToken})" />
        public async Task<IApiFlag> GetFlagAsync(ISession session, string name,
            CancellationToken? cancellationToken = default)
        {
            var resp = await GetFlagsAsync(session, new []{name}, cancellationToken);
            foreach (var flag in resp.Flags)
            {
                if (flag.Name.Equals(name))
                {
                    return flag;
                }
            }

            throw new ArgumentException($"flag '{name}' not found.");
        }

        /// <inheritdoc cref="GetFlagAsync(Satori.ISession,string,string,System.Nullable{System.Threading.CancellationToken})" />
        public Task<IApiFlag> GetFlagAsync(ISession session, string name, string defaultValue,
            CancellationToken? cancellationToken = default)
        {
            try
            {
                return GetFlagAsync(session, name, cancellationToken);
            }
            catch (ArgumentException)
            {
                return Task.FromResult<IApiFlag>(new ApiFlag
                    { Name = name, Value = defaultValue, ConditionChanged = false });
            }
            catch (ApiResponseException e)
            {
                if (_transientExceptionDelegate.Invoke(e))
                {
                    return Task.FromResult<IApiFlag>(new ApiFlag
                        { Name = name, Value = defaultValue, ConditionChanged = false });
                }

                throw;
            }
        }

        /// <inheritdoc cref="GetFlagDefaultAsync(string,string,System.Nullable{System.Threading.CancellationToken})" />
        public async Task<IApiFlag> GetFlagDefaultAsync(string name,
            CancellationToken? cancellationToken = default)
        {
            var resp = await GetFlagsDefaultAsync(new[] { name }, cancellationToken);
            foreach (var flag in resp.Flags)
            {
                if (flag.Name.Equals(name))
                {
                    return flag;
                }
            }

            throw new ArgumentException($"flag '{name}' not found.");
        }

        /// <inheritdoc cref="GetFlagDefaultAsync(string,string,string,System.Nullable{System.Threading.CancellationToken})" />
        public Task<IApiFlag> GetFlagDefaultAsync(string name, string defaultValue,
            CancellationToken? cancellationToken = default)
        {
            try
            {
                return GetFlagDefaultAsync(this.ApiKey, name, cancellationToken);
            }
            catch (ArgumentException)
            {
                return Task.FromResult<IApiFlag>(new ApiFlag
                    { Name = name, Value = defaultValue, ConditionChanged = false });
            }
            catch (ApiResponseException e)
            {
                if (_transientExceptionDelegate.Invoke(e))
                {
                    return Task.FromResult<IApiFlag>(new ApiFlag
                        { Name = name, Value = defaultValue, ConditionChanged = false });
                }

                throw;
            }
        }

        /// <inheritdoc cref="GetFlagsAsync" />
        public async Task<IApiFlagList> GetFlagsAsync(ISession session, IEnumerable<string> names,
            CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            return await _apiClient.SatoriGetFlagsAsync(session.AuthToken, string.Empty, string.Empty, names,
                cancellationToken);
        }

        /// <inheritdoc cref="GetFlagsDefaultAsync" />
        public Task<IApiFlagList> GetFlagsDefaultAsync(IEnumerable<string> names,
            CancellationToken? cancellationToken = default) =>
            _apiClient.SatoriGetFlagsAsync(string.Empty, this.ApiKey, string.Empty, names, cancellationToken);

        /// <inheritdoc cref="IdentifyAsync" />
        public async Task<ISession> IdentifyAsync(ISession session, string id,
            Dictionary<string, string> defaultProperties, Dictionary<string, string> customProperties,
            CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            var req = new ApiIdentifyRequest { Id = id, _default = defaultProperties, _custom = customProperties };
            var resp = await _apiClient.SatoriIdentifyAsync(session.AuthToken, req, cancellationToken);
            return new Session(resp.Token, resp.RefreshToken);
        }

        /// <inheritdoc cref="GetLiveEventsAsync" />
        public async Task<IApiLiveEventList> GetLiveEventsAsync(ISession session, IEnumerable<string> names = null,
            CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            return await _apiClient.SatoriGetLiveEventsAsync(session.AuthToken, names, cancellationToken);
        }

        /// <inheritdoc cref="ListPropertiesAsync" />
        public async Task<IApiProperties> ListPropertiesAsync(ISession session,
            CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            return await _apiClient.SatoriListPropertiesAsync(session.AuthToken, cancellationToken);
        }

        /// <inheritdoc cref="SessionRefreshAsync" />
        public async Task<ISession> SessionRefreshAsync(ISession session,
            CancellationToken? cancellationToken = default)
        {
            var resp = await _apiClient.SatoriAuthenticateRefreshAsync(ApiKey, string.Empty,
                new ApiAuthenticateRefreshRequest { RefreshToken = session.RefreshToken }, cancellationToken);

            if (session is Session updatedSession)
            {
                // Update session object in place if we can.
                updatedSession.Update(resp.Token, resp.RefreshToken);
                return updatedSession;
            }

            return new Session(resp.Token, resp.RefreshToken);
        }

        /// <inheritdoc cref="UpdatePropertiesAsync" />
        public async Task UpdatePropertiesAsync(ISession session, Dictionary<string, string> defaultProperties,
            Dictionary<string, string> customProperties, bool recompute = false, CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            ApiUpdatePropertiesRequest payload = new ApiUpdatePropertiesRequest {
                _default = defaultProperties,
                _custom = customProperties,
                Recompute = recompute,
            };
            await _apiClient.SatoriUpdatePropertiesAsync(session.AuthToken, payload, cancellationToken);
        }

        /// <inheritdoc cref="DeleteIdentityAsync" />
        public async Task DeleteIdentityAsync(ISession session, CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            await _apiClient.SatoriDeleteIdentityAsync(session.AuthToken, cancellationToken);
        }

        /// <inheritdoc cref="GetMessageListAsync" />
        public async Task<IApiGetMessageListResponse> GetMessageListAsync(ISession session, int limit = 1, bool forward = true, string cursor = null, CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            return await _apiClient.SatoriGetMessageListAsync(session.AuthToken, limit, forward, cursor, cancellationToken);
        }

        /// <inheritdoc cref="UpdateMessageAsync" />
        public async Task UpdateMessageAsync(ISession session, string id, string consumeTime, string readTime, CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            await _apiClient.SatoriUpdateMessageAsync(session.AuthToken, id, new ApiUpdateMessageRequest{ConsumeTime = consumeTime, ReadTime = readTime}, cancellationToken);
        }

        /// <inheritdoc cref="DeleteMessageAsync" />
        public async Task DeleteMessageAsync(ISession session, string id, CancellationToken? cancellationToken = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            await _apiClient.SatoriDeleteMessageAsync(session.AuthToken, id, cancellationToken);
        }
    }
}
