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

        /// <inheritdoc cref="IClient.ApiKey"/>
        public string ApiKey { get; }

        /// <inheritdoc cref="IClient.AutoRefreshSession" />
        public bool AutoRefreshSession { get; }

        /// <inheritdoc cref="IClient.GlobalRetryConfiguration"/>
        public RetryConfiguration GlobalRetryConfiguration { get; set; } = new RetryConfiguration(
            baseDelayMs: 500,
            jitter: RetryJitter.FullJitter,
            listener: null,
            maxRetries: 4);

        /// <inheritdoc cref="IClient.Host"/>
        public string Host { get; }

        /// <inheritdoc cref="IClient.Port"/>
        public int Port { get; }

        /// <inheritdoc cref="IClient.Scheme"/>
        public string Scheme { get; }

        /// <inheritdoc cref="IClient.ReceivedSessionUpdated"/>
        public event Action<ISession> ReceivedSessionUpdated;

        /// <inheritdoc cref="IClient.Timeout"/>
        public int Timeout
        {
            get => _apiClient.Timeout;
            set => _apiClient.Timeout = value;
        }

        /// <summary>
        /// The default timeout of the server.
        /// </summary>
        public const int DefaultTimeout = 15;

        private readonly RetryInvoker _retryInvoker;
        private readonly ApiClient _apiClient;

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
            _retryInvoker = new RetryInvoker(adapter.TransientExceptionDelegate);
        }

        /// <inheritdoc cref="AuthenticateAsync" />
        public async Task<ISession> AuthenticateAsync(string id, Dictionary<string, string> defaultProperties = default,
            Dictionary<string, string> customProperties = default, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null)
        {
            var resp = await _retryInvoker.InvokeWithRetry(() => _apiClient.SatoriAuthenticateAsync(ApiKey,
                    string.Empty,
                    new ApiAuthenticateRequest { Id = id, _default = defaultProperties, _custom = customProperties },
                    cancellationToken),
                new RetryHistory(id, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
            return new Session(resp.Token, resp.RefreshToken);
        }

        /// <inheritdoc cref="AuthenticateLogoutAsync" />
        public Task AuthenticateLogoutAsync(ISession session, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null) =>
            _retryInvoker.InvokeWithRetry(() => _apiClient.SatoriAuthenticateLogoutAsync(session.AuthToken,
                    new ApiAuthenticateLogoutRequest { RefreshToken = session.RefreshToken, Token = session.AuthToken },
                    cancellationToken),
                new RetryHistory(session.AuthToken, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));

        /// <inheritdoc cref="EventAsync" />
        public async Task EventAsync(ISession session, Event @event, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            var request = new ApiEventRequest
            {
                _events = new List<SatoriapiEvent>
                {
                    @event.ToApiEvent()
                }
            };

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.SatoriEventAsync(session.AuthToken, request, cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="EventsAsync" />
        public async Task EventsAsync(ISession session, IEnumerable<Event> events,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            var apiEventList = new List<SatoriapiEvent>();
            foreach (var evt in events)
            {
                apiEventList.Add(evt.ToApiEvent());
            }

            var request = new ApiEventRequest
            {
                _events = apiEventList
            };

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.SatoriEventAsync(session.AuthToken, request, cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="GetExperimentsAsync" />
        public Task<IApiExperimentList> GetAllExperimentsAsync(ISession session,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null) =>
            GetExperimentsAsync(session, null, null, cancellationToken, retryConfiguration);

        /// <inheritdoc cref="GetExperimentsAsync" />
        public async Task<IApiExperimentList> GetExperimentsAsync(ISession session, IEnumerable<string> names,
            IEnumerable<string> labels, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.SatoriGetExperimentsAsync(session.AuthToken, names, labels, cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="GetFlagAsync(Satori.ISession,string,System.Nullable{System.Threading.CancellationToken})" />
        public async Task<IApiFlag> GetFlagAsync(ISession session, string name,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null)
        {
            // TODO: check if one should pass one or multiple labels.
            var resp = await GetFlagsAsync(session, new[] { name }, null, cancellationToken, retryConfiguration);
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
                return GetFlagAsync(session, name, defaultValue, cancellationToken);
            }
            catch (ArgumentException)
            {
                return Task.FromResult<IApiFlag>(new ApiFlag
                    { Name = name, Value = defaultValue, ConditionChanged = false });
            }
            catch (ApiResponseException e)
            {
                if (_apiClient.HttpAdapter.TransientExceptionDelegate.Invoke(e))
                {
                    return Task.FromResult<IApiFlag>(new ApiFlag
                        { Name = name, Value = defaultValue, ConditionChanged = false });
                }

                throw;
            }
        }

        /// <inheritdoc cref="GetFlagDefaultAsync(string,System.Nullable{System.Threading.CancellationToken},System.Nullable{RetryConfiguration})" />
        public async Task<IApiFlag> GetFlagDefaultAsync(string name,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null)
        {
            var resp = await GetFlagsDefaultAsync(new[] { name }, null, cancellationToken, retryConfiguration);
            foreach (var flag in resp.Flags)
            {
                if (flag.Name.Equals(name))
                {
                    return flag;
                }
            }

            throw new ArgumentException($"flag '{name}' not found.");
        }

        /// <inheritdoc cref="GetFlagDefaultAsync(string,string,System.Nullable{System.Threading.CancellationToken})" />
        public Task<IApiFlag> GetFlagDefaultAsync(string name, string defaultValue,
            CancellationToken? cancellationToken = default)
        {
            try
            {
                return GetFlagDefaultAsync(name, defaultValue, cancellationToken);
            }
            catch (ArgumentException)
            {
                return Task.FromResult<IApiFlag>(new ApiFlag
                    { Name = name, Value = defaultValue, ConditionChanged = false });
            }
            catch (ApiResponseException e)
            {
                if (_apiClient.HttpAdapter.TransientExceptionDelegate.Invoke(e))
                {
                    return Task.FromResult<IApiFlag>(new ApiFlag
                        { Name = name, Value = defaultValue, ConditionChanged = false });
                }

                throw;
            }
        }

        /// <inheritdoc cref="GetFlagsAsync" />
        public async Task<IApiFlagList> GetFlagsAsync(ISession session, IEnumerable<string> names,
            IEnumerable<string> labels, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.SatoriGetFlagsAsync(session.AuthToken,
                    string.Empty, string.Empty, names, labels,
                    cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="GetFlagsDefaultAsync" />
        public Task<IApiFlagList> GetFlagsDefaultAsync(IEnumerable<string> names,
            IEnumerable<string> labels, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null)
        {
            return _retryInvoker.InvokeWithRetry(
                () => _apiClient.SatoriGetFlagsAsync(string.Empty, this.ApiKey, string.Empty, names, labels, cancellationToken),
                new RetryHistory(string.Empty, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="IdentifyAsync" />
        public async Task<ISession> IdentifyAsync(ISession session, string id,
            Dictionary<string, string> defaultProperties, Dictionary<string, string> customProperties,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            var req = new ApiIdentifyRequest { Id = id, _default = defaultProperties, _custom = customProperties };
            var resp = await _retryInvoker.InvokeWithRetry(
                () => _apiClient.SatoriIdentifyAsync(session.AuthToken, req, cancellationToken),
                new RetryHistory(id, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
            var session2 = new Session(resp.Token, resp.RefreshToken);

            if (session is Session updatedSession)
            {
                // Update session object in place if we can.
                updatedSession.Update(resp.Token, resp.RefreshToken);
                return updatedSession;
            }

            return session2;
        }

        /// <inheritdoc cref="GetLiveEventsAsync" />
        public async Task<IApiLiveEventList> GetLiveEventsAsync(
            ISession session,
            IEnumerable<string> names = null,
            IEnumerable<string> labels = null,
            int? pastRunCount = null,
            int? futureRunCount = null,
            string startTimeSec = null,
            string endTimeSec = null,
            CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.SatoriGetLiveEventsAsync(session.AuthToken, names, labels,
                    pastRunCount, futureRunCount, startTimeSec, endTimeSec, cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="ListPropertiesAsync" />
        public async Task<IApiProperties> ListPropertiesAsync(ISession session,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.SatoriListPropertiesAsync(session.AuthToken, cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="SessionRefreshAsync" />
        public async Task<ISession> SessionRefreshAsync(ISession session,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null)
        {
            var resp = await _retryInvoker.InvokeWithRetry(() => _apiClient.SatoriAuthenticateRefreshAsync(ApiKey,
                    string.Empty,
                    new ApiAuthenticateRefreshRequest { RefreshToken = session.RefreshToken }, cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));

            if (session is Session updatedSession)
            {
                // Update session object in place if we can.
                updatedSession.Update(resp.Token, resp.RefreshToken);
                ReceivedSessionUpdated?.Invoke(updatedSession);
                return updatedSession;
            }

            var newSession = new Session(resp.Token, resp.RefreshToken);
            ReceivedSessionUpdated?.Invoke(newSession);
            return newSession;
        }

        /// <inheritdoc cref="UpdatePropertiesAsync" />
        public async Task UpdatePropertiesAsync(ISession session, Dictionary<string, string> defaultProperties,
            Dictionary<string, string> customProperties, bool recompute = false,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken, retryConfiguration);
            }

            ApiUpdatePropertiesRequest payload = new ApiUpdatePropertiesRequest
            {
                _default = defaultProperties,
                _custom = customProperties,
                Recompute = recompute,
            };
            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.SatoriUpdatePropertiesAsync(session.AuthToken, payload, cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="DeleteIdentityAsync" />
        public async Task DeleteIdentityAsync(ISession session, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.SatoriDeleteIdentityAsync(session.AuthToken, cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="GetMessageListAsync" />
        public async Task<IApiGetMessageListResponse> GetMessageListAsync(ISession session, int limit = 1,
            bool forward = true, string cursor = null, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.SatoriGetMessageListAsync(session.AuthToken,
                    limit, forward, cursor,
                    cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="UpdateMessageAsync" />
        public async Task UpdateMessageAsync(ISession session, string id, string consumeTime, string readTime,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.SatoriUpdateMessageAsync(session.AuthToken, id,
                    new SatoriUpdateMessageBody { ConsumeTime = consumeTime, ReadTime = readTime }, cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="DeleteMessageAsync" />
        public async Task DeleteMessageAsync(ISession session, string id,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.SatoriDeleteMessageAsync(session.AuthToken, id, cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }

        /// <inheritdoc cref="GetFlagOverridesAsync" />
        public async Task<IApiFlagOverrideList> GetFlagOverridesAsync(ISession session, IEnumerable<string> names = null,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, cancellationToken);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.SatoriGetFlagOverridesAsync(session.AuthToken, string.Empty, string.Empty, names, null, cancellationToken),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, cancellationToken));
        }
    }
}
