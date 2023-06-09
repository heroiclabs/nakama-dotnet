// Copyright 2022 The Nakama Authors
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

namespace Nakama
{
    /// <inheritdoc cref="IClient"/>
    public class Client : IClient
    {
        /// <summary>
        /// The default host address of the server.
        /// </summary>
        public const string DefaultHost = "127.0.0.1";

        /// <summary>
        /// The default protocol scheme for the socket connection.
        /// </summary>
        public const string DefaultScheme = "http";

        /// <summary>
        /// The default port number of the server.
        /// </summary>
        public const int DefaultPort = 7350;

        /// <summary>
        /// The default expired timespan used to check session lifetime.
        /// </summary>
        public static TimeSpan DefaultExpiredTimeSpan = TimeSpan.FromMinutes(5);

        /// <inheritdoc cref="IClient.AutoRefreshSession"/>
        public bool AutoRefreshSession { get; }

        /// <inheritdoc cref="IClient.GlobalRetryConfiguration"/>
        public RetryConfiguration GlobalRetryConfiguration { get; set; } = new RetryConfiguration(
            baseDelayMs: 500,
            jitter: RetryJitter.FullJitter,
            listener: null,
            maxRetries: 4);

        /// <inheritdoc cref="IClient.Host"/>
        public string Host { get; }

        /// <summary>
        /// The logger to use with the client.
        /// </summary>
        public ILogger Logger
        {
            get => _logger;
            set
            {
                _apiClient.HttpAdapter.Logger = value;
                _logger = value;
            }
        }

        /// <inheritdoc cref="IClient.Port"/>
        public int Port { get; }

        /// <inheritdoc cref="IClient.Scheme"/>
        public string Scheme { get; }

        /// <inheritdoc cref="IClient.ServerKey"/>
        public string ServerKey { get; }

        /// <inheritdoc cref="IClient.Timeout"/>
        public int Timeout
        {
            get => _apiClient.Timeout;
            set => _apiClient.Timeout = value;
        }

        private readonly ApiClient _apiClient;
        private ILogger _logger;
        private readonly RetryInvoker _retryInvoker;

        private const int DefaultTimeout = 15;

        /// There is a bug in Unity's WebGL implementation that prevents the proper invocation of constructors with more
        /// than four parameters. For this reason, avoid defining constructors that do this.
        public Client(string serverKey) : this(serverKey, HttpRequestAdapter.WithGzip())
        {
        }

        public Client(string serverKey, IHttpAdapter adapter) : this(DefaultScheme,
            DefaultHost, DefaultPort, serverKey, adapter)
        {
        }

        public Client(string scheme, string host, int port, string serverKey) : this(
            scheme, host, port, serverKey, HttpRequestAdapter.WithGzip())
        {
        }

        public Client(string scheme, string host, int port, string serverKey, IHttpAdapter adapter,
            bool autoRefreshSession = true)
        {
            AutoRefreshSession = autoRefreshSession;
            Host = host;
            Port = port;
            Scheme = scheme;
            ServerKey = serverKey;
            _apiClient = new ApiClient(new UriBuilder(scheme, host, port).Uri, adapter, DefaultTimeout);
            Logger = NullLogger.Instance; // must set logger last.

            _retryInvoker = new RetryInvoker(adapter.TransientExceptionDelegate);
        }

        public Client(Uri uri, string serverKey) : this(
            uri, serverKey, HttpRequestAdapter.WithGzip())
        {
        }

        public Client(Uri uri, string serverKey, IHttpAdapter adapter, bool autoRefreshSession = true)
        {
            AutoRefreshSession = autoRefreshSession;
            Host = uri.Host;
            Port = uri.Port;
            Scheme = uri.Scheme;
            ServerKey = serverKey;
            _apiClient = new ApiClient(uri, adapter, DefaultTimeout);
            Logger = NullLogger.Instance; // must set logger last.

            _retryInvoker = new RetryInvoker(adapter.TransientExceptionDelegate);
        }

        /// <inheritdoc cref="AddFriendsAsync"/>
        public async Task AddFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.AddFriendsAsync(session.AuthToken, ids, usernames, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="AddGroupUsersAsync"/>
        public async Task AddGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.AddGroupUsersAsync(session.AuthToken, groupId, ids, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="AuthenticateAppleAsync"/>
        public async Task<ISession> AuthenticateAppleAsync(string token, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateAppleAsync(ServerKey,
                    string.Empty,
                    new ApiAccountApple { Token = token, _vars = vars }, create, username, canceller),
                new RetryHistory(token, retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateCustomAsync"/>
        public async Task<ISession> AuthenticateCustomAsync(string id, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateCustomAsync(ServerKey,
                    string.Empty,
                    new ApiAccountCustom { Id = id, _vars = vars }, create, username, canceller),
                new RetryHistory(id, retryConfiguration ?? GlobalRetryConfiguration, canceller));

            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateDeviceAsync"/>
        public async Task<ISession> AuthenticateDeviceAsync(string id, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateDeviceAsync(ServerKey,
                    string.Empty,
                    new ApiAccountDevice { Id = id, _vars = vars }, create, username, canceller),
                new RetryHistory(id, retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateEmailAsync"/>
        public async Task<ISession> AuthenticateEmailAsync(string email, string password, string username = null,
            bool create = true, Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateEmailAsync(ServerKey,
                    string.Empty,
                    new ApiAccountEmail { Email = email, Password = password, _vars = vars }, create, username,
                    canceller),
                new RetryHistory(email, retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateFacebookAsync"/>
        public async Task<ISession> AuthenticateFacebookAsync(string token, string username = null, bool create = true,
            bool import = true, Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateFacebookAsync(ServerKey,
                    string.Empty,
                    new ApiAccountFacebook { Token = token, _vars = vars }, create, username, import, canceller),
                new RetryHistory(token, retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateGameCenterAsync"/>
        public async Task<ISession> AuthenticateGameCenterAsync(string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestamp, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateGameCenterAsync(ServerKey,
                    string.Empty,
                    new ApiAccountGameCenter
                    {
                        BundleId = bundleId,
                        PlayerId = playerId,
                        PublicKeyUrl = publicKeyUrl,
                        Salt = salt,
                        Signature = signature,
                        TimestampSeconds = timestamp,
                        _vars = vars
                    }, create, username, canceller),
                new RetryHistory(bundleId, retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateGoogleAsync"/>
        public async Task<ISession> AuthenticateGoogleAsync(string token, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateGoogleAsync(ServerKey,
                    string.Empty,
                    new ApiAccountGoogle { Token = token, _vars = vars }, create, username, canceller),
                new RetryHistory(token, retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateSteamAsync"/>
        public async Task<ISession> AuthenticateSteamAsync(string token, string username = null, bool create = true,
            bool import = true, Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateSteamAsync(ServerKey,
                    string.Empty,
                    new ApiAccountSteam { Token = token, _vars = vars }, create, username, import, canceller),
                new RetryHistory(token, retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="BanGroupUsersAsync"/>
        public async Task BanGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.BanGroupUsersAsync(session.AuthToken, groupId, ids, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="BlockFriendsAsync"/>
        public async Task BlockFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.BlockFriendsAsync(session.AuthToken, ids, usernames, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="CreateGroupAsync"/>
        public async Task<IApiGroup> CreateGroupAsync(ISession session, string name, string description = "",
            string avatarUrl = null, string langTag = null, bool open = true, int maxCount = 100,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.CreateGroupAsync(session.AuthToken,
                new ApiCreateGroupRequest
                {
                    Name = name,
                    Description = description,
                    AvatarUrl = avatarUrl,
                    LangTag = langTag,
                    Open = open,
                    MaxCount = maxCount
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DeleteAccountAsync"/>
        public async Task DeleteAccountAsync(ISession session, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.DeleteAccountAsync(session.AuthToken, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DeleteFriendsAsync"/>
        public async Task DeleteFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.DeleteFriendsAsync(session.AuthToken, ids, usernames, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DeleteGroupAsync"/>
        public async Task DeleteGroupAsync(ISession session, string groupId,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.DeleteGroupAsync(session.AuthToken, groupId, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DeleteLeaderboardRecordAsync"/>
        public async Task DeleteLeaderboardRecordAsync(ISession session, string leaderboardId,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.DeleteLeaderboardRecordAsync(session.AuthToken, leaderboardId, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DeleteNotificationsAsync"/>
        public async Task DeleteNotificationsAsync(ISession session, IEnumerable<string> ids,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.DeleteNotificationsAsync(session.AuthToken, ids, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DeleteStorageObjectsAsync"/>
        public async Task DeleteStorageObjectsAsync(ISession session, StorageObjectId[] ids = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            var objects = new List<ApiDeleteStorageObjectId>();

            if (ids != null)
            {
                foreach (var id in ids)
                {
                    objects.Add(new ApiDeleteStorageObjectId
                    {
                        Collection = id.Collection,
                        Key = id.Key,
                        Version = id.Version
                    });
                }
            }


            await _retryInvoker.InvokeWithRetry(() => _apiClient.DeleteStorageObjectsAsync(session.AuthToken,
                    new ApiDeleteStorageObjectsRequest { _objectIds = objects }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DemoteGroupUsersAsync"/>
        public async Task DemoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> usernames,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.DemoteGroupUsersAsync(session.AuthToken, groupId, usernames, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="EventAsync"/>
        public async Task EventAsync(ISession session, string name, Dictionary<string, string> properties,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.EventAsync(session.AuthToken, new ApiEvent
            {
                External = true,
                Name = name,
                _properties = properties
            }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="GetAccountAsync"/>
        public async Task<IApiAccount> GetAccountAsync(ISession session, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.GetAccountAsync(session.AuthToken, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="GetSubscriptionAsync"/>
        public async Task<IApiValidatedSubscription> GetSubscriptionAsync(ISession session, string productId,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.GetSubscriptionAsync(session.AuthToken, productId, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="GetUsersAsync"/>
        public async Task<IApiUsers> GetUsersAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null, IEnumerable<string> facebookIds = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.GetUsersAsync(session.AuthToken, ids, usernames, facebookIds, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ImportFacebookFriendsAsync"/>
        public async Task ImportFacebookFriendsAsync(ISession session, string token, bool? reset = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.ImportFacebookFriendsAsync(session.AuthToken,
                    new ApiAccountFacebook { Token = token },
                    reset, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ImportSteamFriendsAsync"/>
        public async Task ImportSteamFriendsAsync(ISession session, string token, bool? reset = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.ImportSteamFriendsAsync(session.AuthToken, new ApiAccountSteam { Token = token },
                    reset, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="JoinGroupAsync"/>
        public async Task JoinGroupAsync(ISession session, string groupId, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.JoinGroupAsync(session.AuthToken, groupId, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="JoinTournamentAsync"/>
        public async Task JoinTournamentAsync(ISession session, string tournamentId,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.JoinTournamentAsync(session.AuthToken, tournamentId, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="KickGroupUsersAsync"/>
        public async Task KickGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.KickGroupUsersAsync(session.AuthToken, groupId, ids, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LeaveGroupAsync"/>
        public async Task LeaveGroupAsync(ISession session, string groupId,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.LeaveGroupAsync(session.AuthToken, groupId, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkAppleAsync"/>
        public async Task LinkAppleAsync(ISession session, string token, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.LinkAppleAsync(session.AuthToken, new ApiAccountApple { Token = token }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkCustomAsync"/>
        public async Task LinkCustomAsync(ISession session, string id, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.LinkCustomAsync(session.AuthToken, new ApiAccountCustom { Id = id }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkDeviceAsync"/>
        public async Task LinkDeviceAsync(ISession session, string id, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.LinkDeviceAsync(session.AuthToken, new ApiAccountDevice { Id = id }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkEmailAsync"/>
        public async Task LinkEmailAsync(ISession session, string email, string password,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.LinkEmailAsync(session.AuthToken,
                    new ApiAccountEmail { Email = email, Password = password }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkFacebookAsync"/>
        public async Task LinkFacebookAsync(ISession session, string token, bool? import = true,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.LinkFacebookAsync(session.AuthToken, new ApiAccountFacebook { Token = token }, import,
                    canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkGameCenterAsync"/>
        public async Task LinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestamp, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.LinkGameCenterAsync(session.AuthToken,
                new ApiAccountGameCenter
                {
                    BundleId = bundleId,
                    PlayerId = playerId,
                    PublicKeyUrl = publicKeyUrl,
                    Salt = salt,
                    Signature = signature,
                    TimestampSeconds = timestamp
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkGoogleAsync"/>
        public async Task LinkGoogleAsync(ISession session, string token, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.LinkGoogleAsync(session.AuthToken, new ApiAccountGoogle { Token = token }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkSteamAsync"/>
        public async Task LinkSteamAsync(ISession session, string token, bool sync,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.LinkSteamAsync(session.AuthToken,
                    new ApiLinkSteamRequest { Sync = sync, _account = new ApiAccountSteam { Token = token } },
                    canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListChannelMessagesAsync(Nakama.ISession,Nakama.IChannel,int,bool,string,RetryConfiguration,CancellationToken)"/>
        public Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, IChannel channel, int limit = 1,
            bool forward = true, string cursor = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default) =>
            ListChannelMessagesAsync(session, channel.Id, limit, forward, cursor, retryConfiguration, canceller);

        /// <inheritdoc cref="ListChannelMessagesAsync(Nakama.ISession,string,int,bool,string,RetryConfiguration,CancellationToken)"/>
        public async Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, string channelId,
            int limit = 1, bool forward = true, string cursor = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.ListChannelMessagesAsync(session.AuthToken, channelId, limit, forward, cursor,
                    canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListFriendsAsync"/>
        public async Task<IApiFriendList> ListFriendsAsync(ISession session, int? state, int limit, string cursor,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.ListFriendsAsync(session.AuthToken, limit, state, cursor, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListGroupUsersAsync"/>
        /// <remarks>
        /// The returned <c>IApiGroupUserList</c> is automatically updated to reflect username changes for the
        /// current user.
        /// </remarks>
        public async Task<IApiGroupUserList> ListGroupUsersAsync(ISession session, string groupId, int? state,
            int limit, string cursor, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            var response = await _retryInvoker.InvokeWithRetry(
                () => _apiClient.ListGroupUsersAsync(session.AuthToken, groupId, limit, state, cursor, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));

            foreach (var groupUser in response.GroupUsers)
            {
                if (session.UserId.Equals(groupUser.User.Id) && groupUser.User is ApiUser u)
                {
                    u.Username = session.Username;
                }
            }

            return response;
        }

        /// <inheritdoc cref="ListGroupsAsync"/>
        public async Task<IApiGroupList> ListGroupsAsync(ISession session, string name = null, int limit = 1,
            string cursor = null, string langTag = null, int? members = null, bool? open = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.ListGroupsAsync(session.AuthToken, name, cursor, limit, langTag, members, open,
                    canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListLeaderboardRecordsAsync"/>
        /// <remarks>
        /// The returned <c>IApiLeaderboardRecordList</c> is automatically updated to reflect username changes for the
        /// current user.
        /// </remarks>
        public async Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAsync(ISession session, string leaderboardId,
            IEnumerable<string> ownerIds = null, long? expiry = null, int limit = 1, string cursor = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.ListLeaderboardRecordsAsync(
                    session.AuthToken, leaderboardId, ownerIds, limit,
                    cursor, expiry?.ToString(), canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));

            foreach (var record in response.Records)
            {
                if (session.UserId.Equals(record.OwnerId) && record is ApiLeaderboardRecord r)
                {
                    r.Username = session.Username;
                }
            }

            foreach (var record in response.OwnerRecords)
            {
                if (session.UserId.Equals(record.OwnerId) && record is ApiLeaderboardRecord r)
                {
                    r.Username = session.Username;
                }
            }

            return response;
        }

        /// <inheritdoc cref="ListLeaderboardRecordsAroundOwnerAsync"/>
        /// <remarks>
        /// The returned <c>IApiLeaderboardRecordList</c> is automatically updated to reflect username changes for the
        /// current user.
        /// </remarks>
        public async Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAroundOwnerAsync(ISession session,
            string leaderboardId, string ownerId, long? expiry = null, int limit = 1, string cursor = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.ListLeaderboardRecordsAroundOwnerAsync(
                    session.AuthToken, leaderboardId, ownerId,
                    limit, expiry?.ToString(), cursor, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));

            foreach (var record in response.Records)
            {
                if (session.UserId.Equals(record.OwnerId) && record is ApiLeaderboardRecord r)
                {
                    r.Username = session.Username;
                }
            }

            foreach (var record in response.OwnerRecords)
            {
                if (session.UserId.Equals(record.OwnerId) && record is ApiLeaderboardRecord r)
                {
                    r.Username = session.Username;
                }
            }

            return response;
        }

        /// <inheritdoc cref="ListMatchesAsync"/>
        public async Task<IApiMatchList> ListMatchesAsync(ISession session, int min, int max, int limit,
            bool authoritative, string label, string query, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.ListMatchesAsync(session.AuthToken, limit, authoritative, label, min, max, query,
                    canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListNotificationsAsync"/>
        /// <remarks>
        /// The returned <c>IApiNotificationList</c> is automatically updated to reflect username changes for the
        /// current user.
        /// </remarks>
        public async Task<IApiNotificationList> ListNotificationsAsync(ISession session, int limit = 1,
            string cacheableCursor = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            var response = await _retryInvoker.InvokeWithRetry(
                () => _apiClient.ListNotificationsAsync(session.AuthToken, limit, cacheableCursor, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));

            foreach (var notification in response.Notifications)
            {
                if (!session.Username.Equals(notification.SenderId) && notification is ApiNotification n)
                {
                    n.SenderId = session.UserId;
                }
            }

            return response;
        }

        [Obsolete("ListStorageObjects is obsolete, please use ListStorageObjectsAsync instead.", true)]
        public Task<IApiStorageObjectList> ListStorageObjects(ISession session, string collection, int limit = 1,
            string cursor = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default) =>
            _retryInvoker.InvokeWithRetry(
                () => _apiClient.ListStorageObjectsAsync(session.AuthToken, collection, string.Empty, limit, cursor,
                    canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));

        /// <inheritdoc cref="ListStorageObjectsAsync"/>
        public async Task<IApiStorageObjectList> ListStorageObjectsAsync(ISession session, string collection,
            int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.ListStorageObjectsAsync(session.AuthToken, collection, string.Empty, limit, cursor,
                    canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListSubscriptionsAsync"/>
        public async Task<IApiSubscriptionList> ListSubscriptionsAsync(ISession session, int limit,
            string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.ListSubscriptionsAsync(session.AuthToken,
                    new ApiListSubscriptionsRequest { Cursor = cursor, Limit = limit }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListTournamentRecordsAroundOwnerAsync"/>
        /// <remarks>
        /// The returned <c>IApiTournamentRecordList</c> is automatically updated to reflect username changes for the
        /// current user.
        /// </remarks>
        public async Task<IApiTournamentRecordList> ListTournamentRecordsAroundOwnerAsync(ISession session,
            string tournamentId, string ownerId, long? expiry = null, int limit = 1, string cursor = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.ListTournamentRecordsAroundOwnerAsync(
                    session.AuthToken, tournamentId, ownerId,
                    limit, expiry?.ToString(), cursor, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));

            foreach (var record in response.Records)
            {
                if (session.UserId.Equals(record.OwnerId) && record is ApiLeaderboardRecord r)
                {
                    r.Username = session.Username;
                }
            }

            foreach (var record in response.OwnerRecords)
            {
                if (session.UserId.Equals(record.OwnerId) && record is ApiLeaderboardRecord r)
                {
                    r.Username = session.Username;
                }
            }

            return response;
        }

        /// <inheritdoc cref="ListTournamentRecordsAsync"/>
        /// <remarks>
        /// The returned <c>IApiTournamentRecordList</c> is automatically updated to reflect username changes for the
        /// current user.
        /// </remarks>
        public async Task<IApiTournamentRecordList> ListTournamentRecordsAsync(ISession session, string tournamentId,
            IEnumerable<string> ownerIds = null, long? expiry = null, int limit = 1, string cursor = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.ListTournamentRecordsAsync(
                    session.AuthToken, tournamentId, ownerIds, limit, cursor,
                    expiry?.ToString(), canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));

            foreach (var record in response.Records)
            {
                if (session.UserId.Equals(record.OwnerId) && record is ApiLeaderboardRecord r)
                {
                    r.Username = session.Username;
                }
            }

            foreach (var record in response.OwnerRecords)
            {
                if (session.UserId.Equals(record.OwnerId) && record is ApiLeaderboardRecord r)
                {
                    r.Username = session.Username;
                }
            }

            return response;
        }

        /// <inheritdoc cref="ListTournamentsAsync"/>
        public async Task<IApiTournamentList> ListTournamentsAsync(ISession session, int categoryStart, int categoryEnd,
            int? startTime = null, int? endTime = null, int limit = 1, string cursor = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.ListTournamentsAsync(session.AuthToken,
                    categoryStart, categoryEnd, startTime,
                    endTime, limit, cursor, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListUserGroupsAsync(Nakama.ISession,int?,int,string,RetryConfiguration,CancellationToken)"/>
        public Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, int? state, int limit, string cursor,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default) =>
            ListUserGroupsAsync(session, session.UserId, state, limit, cursor);

        /// <inheritdoc cref="ListUserGroupsAsync(Nakama.ISession,string,int?,int,string,RetryConfiguration,CancellationToken)"/>
        public async Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, string userId, int? state, int limit,
            string cursor, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.ListUserGroupsAsync(session.AuthToken, userId, limit, state, cursor, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListUsersStorageObjectsAsync"/>
        public async Task<IApiStorageObjectList> ListUsersStorageObjectsAsync(ISession session, string collection,
            string userId, int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.ListStorageObjects2Async(session.AuthToken, collection, userId, limit, cursor,
                    canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="PromoteGroupUsersAsync"/>
        public async Task PromoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.PromoteGroupUsersAsync(session.AuthToken, groupId, ids, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ReadStorageObjectsAsync"/>
        public async Task<IApiStorageObjects> ReadStorageObjectsAsync(ISession session,
            IApiReadStorageObjectId[] ids = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            var objects = new List<ApiReadStorageObjectId>();

            if (ids != null)
            {
                foreach (var id in ids)
                {
                    objects.Add(new ApiReadStorageObjectId
                    {
                        Collection = id.Collection,
                        Key = id.Key,
                        UserId = id.UserId
                    });
                }
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.ReadStorageObjectsAsync(session.AuthToken,
                    new ApiReadStorageObjectsRequest { _objectIds = objects }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="RpcAsync(Nakama.ISession,string,string,RetryConfiguration,CancellationToken)"/>
        public async Task<IApiRpc> RpcAsync(ISession session, string id, string payload,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.RpcFuncAsync(session.AuthToken, string.Empty, string.Empty, id, payload, null, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="RpcAsync(Nakama.ISession,string,RetryConfiguration,CancellationToken)"/>
        public async Task<IApiRpc> RpcAsync(ISession session, string id, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(
                () => _apiClient.RpcFunc2Async(session.AuthToken, string.Empty, string.Empty, id, null, null, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="RpcAsync(string,string,string,RetryConfiguration,CancellationToken)"/>
        public Task<IApiRpc> RpcAsync(string httpkey, string id, string payload,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default) =>
            _retryInvoker.InvokeWithRetry(() => _apiClient.RpcFuncAsync(null, string.Empty, string.Empty, id, payload, httpkey, canceller),
                new RetryHistory(id, retryConfiguration ?? GlobalRetryConfiguration, canceller));

        /// <inheritdoc cref="RpcAsync(string,string,RetryConfiguration,CancellationToken)"/>
        public Task<IApiRpc> RpcAsync(string httpkey, string id, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default) =>
            _retryInvoker.InvokeWithRetry(() => _apiClient.RpcFunc2Async(null, string.Empty, string.Empty, id, null, httpkey, canceller),
                new RetryHistory(id, retryConfiguration ?? GlobalRetryConfiguration, canceller));

        /// <inheritdoc cref="SessionLogoutAsync(Nakama.ISession,RetryConfiguration,CancellationToken)"/>
        public Task SessionLogoutAsync(ISession session, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default) => SessionLogoutAsync(session.AuthToken, session.RefreshToken,
            retryConfiguration, canceller);

        /// <inheritdoc cref="SessionLogoutAsync(string,string,RetryConfiguration,CancellationToken)"/>
        public Task SessionLogoutAsync(string authToken, string refreshToken,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default) =>
            _retryInvoker.InvokeWithRetry(() => _apiClient.SessionLogoutAsync(authToken,
                    new ApiSessionLogoutRequest { Token = authToken, RefreshToken = refreshToken }, canceller),
                new RetryHistory(authToken, retryConfiguration ?? GlobalRetryConfiguration, canceller));

        /// <inheritdoc cref="SessionRefreshAsync"/>
        public async Task<ISession> SessionRefreshAsync(ISession session, Dictionary<string, string> vars = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            // NOTE: Warn developers to encourage them to set a suitable session and refresh token lifetime.
            if (session.Created && session.ExpireTime - session.CreateTime < 70)
            {
                Logger.WarnFormat(
                    "Session lifetime too short, please set '--session.token_expiry_sec' option. See the documentation for more info: https://heroiclabs.com/docs/install-configuration/#session");
            }

            if (session.Created && session.RefreshExpireTime - session.CreateTime < 3700)
            {
                Logger.WarnFormat(
                    "Session refresh lifetime too short, please set '--session.refresh_token_expiry_sec' option. See the documentation for more info: https://heroiclabs.com/docs/install-configuration/#session");
            }

            var response = await _retryInvoker.InvokeWithRetry(() => _apiClient.SessionRefreshAsync(ServerKey,
                    string.Empty,
                    new ApiSessionRefreshRequest { Token = session.RefreshToken, _vars = vars }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));

            if (session is Session updatedSession)
            {
                // Update session object in place if we can.
                updatedSession.Update(response.Token, response.RefreshToken);
                return updatedSession;
            }

            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        public override string ToString()
        {
            return $"Client(Host='{Host}', Port={Port}, Scheme='{Scheme}', ServerKey='{ServerKey}', Timeout={Timeout})";
        }

        /// <inheritdoc cref="UnlinkAppleAsync"/>
        public async Task UnlinkAppleAsync(ISession session, string token, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.UnlinkAppleAsync(session.AuthToken, new ApiAccountApple { Token = token }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkCustomAsync"/>
        public async Task UnlinkCustomAsync(ISession session, string id, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.UnlinkCustomAsync(session.AuthToken, new ApiAccountCustom { Id = id }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkDeviceAsync"/>
        public async Task UnlinkDeviceAsync(ISession session, string id, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.UnlinkDeviceAsync(session.AuthToken, new ApiAccountDevice { Id = id }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkEmailAsync"/>
        public async Task UnlinkEmailAsync(ISession session, string email, string password,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.UnlinkEmailAsync(session.AuthToken,
                    new ApiAccountEmail { Email = email, Password = password }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkFacebookAsync"/>
        public async Task UnlinkFacebookAsync(ISession session, string token,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.UnlinkFacebookAsync(session.AuthToken, new ApiAccountFacebook { Token = token },
                    canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkGameCenterAsync"/>
        public async Task UnlinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestamp, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.UnlinkGameCenterAsync(
                session.AuthToken,
                new ApiAccountGameCenter
                {
                    BundleId = bundleId,
                    PlayerId = playerId,
                    PublicKeyUrl = publicKeyUrl,
                    Salt = salt,
                    Signature = signature,
                    TimestampSeconds = timestamp
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkGoogleAsync"/>
        public async Task UnlinkGoogleAsync(ISession session, string token,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.UnlinkGoogleAsync(session.AuthToken, new ApiAccountGoogle { Token = token },
                    canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkSteamAsync"/>
        public async Task UnlinkSteamAsync(ISession session, string token, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(
                () => _apiClient.UnlinkSteamAsync(session.AuthToken, new ApiAccountSteam { Token = token }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UpdateAccountAsync"/>
        /// <remarks>
        /// The current user's username will be automatically refreshed in their authorized <c>ISession</c> if the
        /// username field is updated to become different.
        /// </remarks>
        public async Task UpdateAccountAsync(ISession session, string username, string displayName = null,
            string avatarUrl = null, string langTag = null, string location = null, string timezone = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.UpdateAccountAsync(
                session.AuthToken, new ApiUpdateAccountRequest
                {
                    AvatarUrl = avatarUrl,
                    DisplayName = displayName,
                    LangTag = langTag,
                    Location = location,
                    Timezone = timezone,
                    Username = username
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));

            if (!username.Equals(session.Username))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }
        }

        /// <inheritdoc cref="UpdateGroupAsync"/>
        public async Task UpdateGroupAsync(ISession session, string groupId, string name, bool open,
            string description = null, string avatarUrl = null, string langTag = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.UpdateGroupAsync(
                session.AuthToken, groupId,
                new ApiUpdateGroupRequest
                {
                    Name = name,
                    Open = open,
                    AvatarUrl = avatarUrl,
                    Description = description,
                    LangTag = langTag
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ValidatePurchaseAppleAsync"/>
        public async Task<IApiValidatePurchaseResponse> ValidatePurchaseAppleAsync(ISession session, string receipt,
            bool persist = true, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.ValidatePurchaseAppleAsync(session.AuthToken,
                new ApiValidatePurchaseAppleRequest
                {
                    Receipt = receipt,
                    Persist = persist
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ValidatePurchaseGoogleAsync"/>
        public async Task<IApiValidatePurchaseResponse> ValidatePurchaseGoogleAsync(ISession session, string receipt,
            bool persist = true, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.ValidatePurchaseGoogleAsync(session.AuthToken,
                new ApiValidatePurchaseGoogleRequest
                {
                    Purchase = receipt,
                    Persist = persist
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ValidatePurchaseHuaweiAsync"/>
        public async Task<IApiValidatePurchaseResponse> ValidatePurchaseHuaweiAsync(ISession session, string receipt,
            string signature, bool persist = true, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.ValidatePurchaseHuaweiAsync(session.AuthToken,
                new ApiValidatePurchaseHuaweiRequest
                {
                    Purchase = receipt,
                    Signature = signature,
                    Persist = persist
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        public async Task<IApiValidateSubscriptionResponse> ValidateSubscriptionAppleAsync(ISession session,
            string receipt, bool persist = true, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.ValidateSubscriptionAppleAsync(
                session.AuthToken, new ApiValidateSubscriptionAppleRequest
                {
                    Receipt = receipt,
                    Persist = persist
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        public async Task<IApiValidateSubscriptionResponse> ValidateSubscriptionGoogleAsync(ISession session,
            string receipt, bool persist = true, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.ValidateSubscriptionGoogleAsync(
                session.AuthToken, new ApiValidateSubscriptionGoogleRequest
                {
                    Receipt = receipt,
                    Persist = persist
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="WriteLeaderboardRecordAsync"/>
        public async Task<IApiLeaderboardRecord> WriteLeaderboardRecordAsync(ISession session, string leaderboardId,
            long score, long subScore = 0, string metadata = null, ApiOperator apiOperator = ApiOperator.NO_OVERRIDE,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.WriteLeaderboardRecordAsync(
                session.AuthToken, leaderboardId,
                new WriteLeaderboardRecordRequestLeaderboardRecordWrite
                {
                    Metadata = metadata,
                    Score = score.ToString(),
                    Subscore = subScore.ToString(),
                    _operator = apiOperator
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="WriteStorageObjectsAsync"/>
        public async Task<IApiStorageObjectAcks> WriteStorageObjectsAsync(ISession session,
            IApiWriteStorageObject[] objects = null, RetryConfiguration retryConfiguration = null,
            CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            var writes = new List<ApiWriteStorageObject>(objects.Length);
            foreach (var obj in objects)
            {
                writes.Add(new ApiWriteStorageObject
                {
                    Collection = obj.Collection,
                    Key = obj.Key,
                    PermissionRead = obj.PermissionRead,
                    PermissionWrite = obj.PermissionWrite,
                    Value = obj.Value,
                    Version = obj.Version
                });
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.WriteStorageObjectsAsync(session.AuthToken,
                    new ApiWriteStorageObjectsRequest { _objects = writes }, canceller),
                new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="WriteTournamentRecordAsync"/>
        public async Task<IApiLeaderboardRecord> WriteTournamentRecordAsync(ISession session, string tournamentId,
            long score, long subScore = 0, string metadata = null, ApiOperator apiOperator = ApiOperator.NO_OVERRIDE,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await _retryInvoker.InvokeWithRetry(() => _apiClient.WriteTournamentRecordAsync(session.AuthToken,
                tournamentId,
                new WriteTournamentRecordRequestTournamentRecordWrite
                {
                    Metadata = metadata,
                    Score = score.ToString(),
                    Subscore = subScore.ToString(),
                    _operator = apiOperator
                }, canceller), new RetryHistory(session, retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }
    }
}
