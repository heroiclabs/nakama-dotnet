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
        /// The key used to authenticate with the server without a session.
        /// </summary>
        string ApiKey { get; }

        /// <summary>
        /// True if the session should be refreshed with an active refresh token.
        /// </summary>
        bool AutoRefreshSession { get; }

        /// <summary>
        /// The global retry configuration. See <see cref="RetryConfiguration"/>.
        /// </summary>
        RetryConfiguration GlobalRetryConfiguration { get; set; }

        /// <summary>
        /// The host address of the server.
        /// </summary>
        string Host { get; }

        /// <summary>
        /// The logger to use with the client.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// The port number of the server.
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
        /// Received a new session after the current one has expired.
        /// </summary>
        /// <remarks>
        /// This event will only be sent when <c>SessionRefreshAsync</c> is called which also happens automatically if
        /// <c>AutoRefreshSession</c> is enabled.
        /// </remarks>
        /// <see cref="SessionRefreshAsync"/>
        /// <seealso cref="AutoRefreshSession"/>
        event Action<ISession> ReceivedSessionUpdated;

        /// <summary>
        /// Authenticate against the server.
        /// </summary>
        /// <param name="id">An optional user id.</param>
        /// <param name="defaultProperties">Optional default properties to update with this call. If not set, properties are left as they are on the server.</param>
        /// <param name="customProperties">Optional custom properties to update with this call. If not set, properties are left as they are on the server.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task which resolves to a user session.</returns>
        public Task<ISession> AuthenticateAsync(string id, Dictionary<string, string> defaultProperties = default,
            Dictionary<string, string> customProperties = default, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Log out a session, invalidate a refresh token, or log out all sessions/refresh tokens for a user.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        public Task AuthenticateLogoutAsync(ISession session, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Send an event for this session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="event">The event to send.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task object.</returns>
        public Task EventAsync(ISession session, Event @event, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Send a batch of events for this session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="events">The batch of events which will be sent.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task object.</returns>
        public Task EventsAsync(ISession session, IEnumerable<Event> events,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Get all experiments data.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task which resolves to all experiments that this identity is involved with.</returns>
        public Task<IApiExperimentList> GetAllExperimentsAsync(ISession session,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Get specific experiments data.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="names">Experiment names; if empty string, all experiments are returned based on the remaining filters.</param>
        /// <param name="labels">Label names that must be defined for each Experiment; if empty string, all experiments are returned based on the remaining filters.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task which resolves to all experiments that this identity is involved with.</returns>
        public Task<IApiExperimentList> GetExperimentsAsync(ISession session, IEnumerable<string> names, IEnumerable<string> labels,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Get a single flag for this identity.
        /// </summary>
        /// <remarks>
        /// Unlike <c>GetFlags(ISession,string,CancellationToken)</c> this method will return the default value
        /// specified and will not raise an exception if the network is unavailable.
        /// </remarks>
        /// <param name="session">The session of the user.</param>
        /// <param name="name">The name of the flag.</param>
        /// <param name="defaultValue">The default value if the server is unreachable.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a single feature flag.</returns>
        public Task<IApiFlag> GetFlagAsync(ISession session, string name, string defaultValue,
            CancellationToken? cancellationToken = default);

        /// <summary>
        /// Get a single default flag for this identity.
        /// </summary>
        /// <param name="name">The name of the flag.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task which resolves to a single default feature flag.</returns>
        public Task<IApiFlag> GetFlagDefaultAsync(string name,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Get a single default flag for this identity.
        /// </summary>
        /// <remarks>
        /// Unlike <c>GetFlagDefaultAsync(string,string,CancellationToken)</c> this method will return the default
        /// value specified and will not raise an exception if the network is unreachable.
        /// </remarks>
        /// <param name="name">The name of the flag.</param>
        /// <param name="defaultValue">The default value if the server is unreachable.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a single default feature flag.</returns>
        public Task<IApiFlag> GetFlagDefaultAsync(string name, string defaultValue,
            CancellationToken? cancellationToken = default);

        /// <summary>
        /// List all available flags for this identity.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="names">Flag names, if empty string all flags are returned.</param>
        /// <param name="labels">Label names that must be defined for each Flag; if empty string, all flags are returned based on the remaining filters.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task which resolves to all flags available to this identity.</returns>
        public Task<IApiFlagList> GetFlagsAsync(ISession session, IEnumerable<string> names,
            IEnumerable<string> labels, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// List all available default flags.
        /// </summary>
        /// <param name="names">Flag names, if empty string all flags are returned.</param>
        /// <param name="labels">Label names that must be defined for each Flag; if empty string, all flags are returned based on the remaining filters.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task which resolves to all available default flags.</returns>
        public Task<IApiFlagList> GetFlagsDefaultAsync(IEnumerable<string> names, IEnumerable<string> labels,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// List available live events.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="names">Live event names, if null or empty, all live events are returned.</param>
        /// <param name="labels">Label names that must be defined for each Live Event; if empty string, all live events are returned based on the remaining filters.</param>
        /// <param name="pastRunCount">The maximum number of past event runs to return for each live event.</param>
        /// <param name="futureRunCount">The maximum number of future event runs to return for each live event.</param>
        /// <param name="startTimeSec">Start time of the time window filter to apply.</param>
        /// <param name="endTimeSec">End time of the time window filter to apply.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task which resolves to a list of live events.</returns>
        public Task<IApiLiveEventList> GetLiveEventsAsync(
            ISession session,
            IEnumerable<string> names = null,
            IEnumerable<string> labels = null,
            int? pastRunCount = null,
            int? futureRunCount = null,
            string startTimeSec = null,
            string endTimeSec = null,
            CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null
        );
        
        /// <summary>
        /// Join an 'explicit join' live event.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">Live event id to join.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        public Task JoinLiveEventAsync(ISession session, string id = null,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Identify a session with a new ID.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">Identity ID to enrich the current session and return a new session. The old session will
        /// no longer be usable. Must be between eight and 128 characters (inclusive). Must be an alphanumeric string
        /// with only underscores and hyphens allowed. </param>
        /// <param name="defaultProperties">The default properties.</param>
        /// <param name="customProperties">The custom event properties.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task which resolves to the new session for the user.</returns>
        public Task<ISession> IdentifyAsync(ISession session, string id, Dictionary<string, string> defaultProperties,
            Dictionary<string, string> customProperties, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// List properties associated with this identity.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task which resolves to a list of live events.</returns>
        public Task<IApiProperties> ListPropertiesAsync(ISession session,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Refresh a user's session using a refresh token retrieved from a previous authentication request.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task which resolves to a user session.</returns>
        public Task<ISession> SessionRefreshAsync(ISession session, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Update properties associated with this identity.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="defaultProperties">The default properties to update.</param>
        /// <param name="customProperties">The custom properties to update.</param>
        /// <param name="recompute">Whether or not to recompute the user's audience membership immediately after property update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task object.</returns>
        public Task UpdatePropertiesAsync(ISession session, Dictionary<string, string> defaultProperties,
            Dictionary<string, string> customProperties, bool recompute, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Delete the caller's identity and associated data.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task object.</returns>
        public Task DeleteIdentityAsync(ISession session, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Get all the messages for an identity.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="limit">Max number of messages to return. Between 1 and 100.</param>
        /// <param name="forward">True if listing should be older messages to newer, false if reverse.</param>
        /// <param name="cursor">A pagination cursor, if any.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task object which resolves to a list of messages.</returns>
        public Task<IApiGetMessageListResponse> GetMessageListAsync(ISession session, int limit = 1,
            bool forward = true, string cursor = null, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Update the status of a message.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">The message's unique identifier.</param>
        /// <param name="consumeTime">The time the message was consumed by the identity.</param>
        /// <param name="readTime">The time the message was read at the client.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task object.</returns>
        public Task UpdateMessageAsync(ISession session, string id, string consumeTime, string readTime,
            CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Delete a scheduled message.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">The identifier of the message.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task object.</returns>
        public Task DeleteMessageAsync(ISession session, string id, CancellationToken? cancellationToken = default,
            RetryConfiguration retryConfiguration = null);

        /// <summary>
        /// Get all available flags and their value overrides for this identity.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="names">Live event names, if null or empty, all live events are returned.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <returns>A task object which resolves to a list all available flags and their value overrides for this identity.</returns>
        public Task<IApiFlagOverrideList> GetFlagOverridesAsync(ISession session, IEnumerable<string> names = null,
            IEnumerable<string> labels = null, CancellationToken? cancellationToken = default, RetryConfiguration retryConfiguration = null);
    }
}
