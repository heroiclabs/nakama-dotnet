// Copyright 2018 The Nakama Authors
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
            baseDelay: 500,
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

        /// <inheritdoc cref="IClient.RetryJitterSeed"/>
        public int RetryJitterSeed => _retryInvoker.JitterSeed;

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
        private readonly RetryInvoker _retryInvoker = new RetryInvoker();

        private const int DefaultTimeout = 15;

        public Client(string serverKey) : this(serverKey, HttpRequestAdapter.WithGzip())
        {
        }

        public Client(string serverKey, IHttpAdapter adapter) : this(DefaultScheme,
            DefaultHost, DefaultPort, serverKey,  adapter)
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
        }

        /// <inheritdoc cref="AddFriendsAsync"/>
        public async Task AddFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await _retryInvoker.InvokeWithRetry(() => _apiClient.AddFriendsAsync(session.AuthToken, ids, usernames, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="AddGroupUsersAsync"/>
        public async Task AddGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.AddGroupUsersAsync(session.AuthToken, groupId, ids, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="AuthenticateAppleAsync"/>
        public async Task<ISession> AuthenticateAppleAsync(string token, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            var response = await  _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateAppleAsync(ServerKey, string.Empty,
                new ApiAccountApple {Token = token, _vars = vars}, create, username, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateCustomAsync"/>
        public async Task<ISession> AuthenticateCustomAsync(string id, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            var response = await  _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateCustomAsync(ServerKey, string.Empty,
                new ApiAccountCustom {Id = id, _vars = vars}, create, username, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));

            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateDeviceAsync"/>
        public async Task<ISession> AuthenticateDeviceAsync(string id, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            var response = await  _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateDeviceAsync(ServerKey, string.Empty,
                new ApiAccountDevice {Id = id, _vars = vars}, create, username, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateEmailAsync"/>
        public async Task<ISession> AuthenticateEmailAsync(string email, string password, string username = null,
            bool create = true, Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            var response = await  _retryInvoker.InvokeWithRetry(() =>_apiClient.AuthenticateEmailAsync(ServerKey, string.Empty,
                new ApiAccountEmail {Email = email, Password = password, _vars = vars}, create, username, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateFacebookAsync"/>
        public async Task<ISession> AuthenticateFacebookAsync(string token, string username = null, bool create = true,
            bool import = true, Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            var response = await  _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateFacebookAsync(ServerKey, string.Empty,
                new ApiAccountFacebook {Token = token, _vars = vars}, create, username, import, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateGameCenterAsync"/>
        public async Task<ISession> AuthenticateGameCenterAsync(string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestamp, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            var response = await  _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateGameCenterAsync(ServerKey, string.Empty,
                new ApiAccountGameCenter
                {
                    BundleId = bundleId,
                    PlayerId = playerId,
                    PublicKeyUrl = publicKeyUrl,
                    Salt = salt,
                    Signature = signature,
                    TimestampSeconds = timestamp,
                    _vars = vars
                }, create, username, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateGoogleAsync"/>
        public async Task<ISession> AuthenticateGoogleAsync(string token, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            var response = await  _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateGoogleAsync(ServerKey, string.Empty,
                new ApiAccountGoogle {Token = token, _vars = vars}, create, username, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateSteamAsync"/>
        public async Task<ISession> AuthenticateSteamAsync(string token, string username = null, bool create = true,
            bool import = true, Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            var response = await  _retryInvoker.InvokeWithRetry(() => _apiClient.AuthenticateSteamAsync(ServerKey, string.Empty,
                new ApiAccountSteam {Token = token, _vars = vars}, create, username, import, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="BanGroupUsersAsync"/>
        public async Task BanGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.BanGroupUsersAsync(session.AuthToken, groupId, ids, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="BlockFriendsAsync"/>
        public async Task BlockFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.BlockFriendsAsync(session.AuthToken, ids, usernames, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="CreateGroupAsync"/>
        public async Task<IApiGroup> CreateGroupAsync(ISession session, string name, string description = "",
            string avatarUrl = null, string langTag = null, bool open = true, int maxCount = 100, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.CreateGroupAsync(session.AuthToken, new ApiCreateGroupRequest
            {
                Name = name,
                Description = description,
                AvatarUrl = avatarUrl,
                LangTag = langTag,
                Open = open,
                MaxCount = maxCount
            }, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DeleteFriendsAsync"/>
        public async Task DeleteFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.DeleteFriendsAsync(session.AuthToken, ids, usernames, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DeleteGroupAsync"/>
        public async Task DeleteGroupAsync(ISession session, string groupId, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.DeleteGroupAsync(session.AuthToken, groupId, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DeleteLeaderboardRecordAsync"/>
        public async Task DeleteLeaderboardRecordAsync(ISession session, string leaderboardId, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.DeleteLeaderboardRecordAsync(session.AuthToken, leaderboardId, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DeleteNotificationsAsync"/>
        public async Task DeleteNotificationsAsync(ISession session, IEnumerable<string> ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.DeleteNotificationsAsync(session.AuthToken, ids, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DeleteStorageObjectsAsync"/>
        public async Task DeleteStorageObjectsAsync(ISession session, StorageObjectId[] ids = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
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


            await  _retryInvoker.InvokeWithRetry(() => _apiClient.DeleteStorageObjectsAsync(session.AuthToken,
                new ApiDeleteStorageObjectsRequest {_objectIds = objects}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="DemoteGroupUsersAsync"/>
        public async Task DemoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> usernames, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.DemoteGroupUsersAsync(session.AuthToken, groupId, usernames, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="EventAsync"/>
        public async Task EventAsync(ISession session, string name, Dictionary<string, string> properties, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.EventAsync(session.AuthToken, new ApiEvent
            {
                External = true,
                Name = name,
                _properties = properties
            }, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="GetAccountAsync"/>
        public async Task<IApiAccount> GetAccountAsync(ISession session, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.GetAccountAsync(session.AuthToken, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="GetUsersAsync"/>
        public async Task<IApiUsers> GetUsersAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null, IEnumerable<string> facebookIds = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.GetUsersAsync(session.AuthToken, ids, usernames, facebookIds, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ImportFacebookFriendsAsync"/>
        public async Task ImportFacebookFriendsAsync(ISession session, string token, bool? reset = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.ImportFacebookFriendsAsync(session.AuthToken, new ApiAccountFacebook {Token = token},
                reset, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ImportSteamFriendsAsync"/>
        public async Task ImportSteamFriendsAsync(ISession session, string token, bool? reset = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.ImportSteamFriendsAsync(session.AuthToken, new ApiAccountSteam {Token = token}, reset, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="JoinGroupAsync"/>
        public async Task JoinGroupAsync(ISession session, string groupId, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.JoinGroupAsync(session.AuthToken, groupId, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="JoinTournamentAsync"/>
        public async Task JoinTournamentAsync(ISession session, string tournamentId, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.JoinTournamentAsync(session.AuthToken, tournamentId, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="KickGroupUsersAsync"/>
        public async Task KickGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.KickGroupUsersAsync(session.AuthToken, groupId, ids, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LeaveGroupAsync"/>
        public async Task LeaveGroupAsync(ISession session, string groupId, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.LeaveGroupAsync(session.AuthToken, groupId, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkAppleAsync"/>
        public async Task LinkAppleAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.LinkAppleAsync(session.AuthToken, new ApiAccountApple {Token = token}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkCustomAsync"/>
        public async Task LinkCustomAsync(ISession session, string id, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.LinkCustomAsync(session.AuthToken, new ApiAccountCustom {Id = id}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkDeviceAsync"/>
        public async Task LinkDeviceAsync(ISession session, string id, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.LinkDeviceAsync(session.AuthToken, new ApiAccountDevice {Id = id}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkEmailAsync"/>
        public async Task LinkEmailAsync(ISession session, string email, string password, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.LinkEmailAsync(session.AuthToken,
                new ApiAccountEmail {Email = email, Password = password}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkFacebookAsync"/>
        public async Task LinkFacebookAsync(ISession session, string token, bool? import = true, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.LinkFacebookAsync(session.AuthToken, new ApiAccountFacebook {Token = token}, import, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkGameCenterAsync"/>
        public async Task LinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestamp, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.LinkGameCenterAsync(session.AuthToken,
                new ApiAccountGameCenter
                {
                    BundleId = bundleId,
                    PlayerId = playerId,
                    PublicKeyUrl = publicKeyUrl,
                    Salt = salt,
                    Signature = signature,
                    TimestampSeconds = timestamp
                }, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkGoogleAsync"/>
        public async Task LinkGoogleAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.LinkGoogleAsync(session.AuthToken, new ApiAccountGoogle {Token = token}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="LinkSteamAsync"/>
        public async Task LinkSteamAsync(ISession session, string token, bool sync, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.LinkSteamAsync(session.AuthToken,
                new ApiLinkSteamRequest {Sync = sync, _account = new ApiAccountSteam {Token = token}}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListChannelMessagesAsync(Nakama.ISession,Nakama.IChannel,int,bool,string)"/>
        public Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, IChannel channel, int limit = 1,
            bool forward = true, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default) =>
            ListChannelMessagesAsync(session, channel.Id, limit, forward, cursor);

        /// <inheritdoc cref="ListChannelMessagesAsync(Nakama.ISession,string,int,bool,string)"/>
        public async Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, string channelId,
            int limit = 1, bool forward = true, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListChannelMessagesAsync(session.AuthToken, channelId, limit, forward, cursor, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListFriendsAsync"/>
        public async Task<IApiFriendList> ListFriendsAsync(ISession session, int? state, int limit, string cursor, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListFriendsAsync(session.AuthToken, limit, state, cursor, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListGroupUsersAsync"/>
        public async Task<IApiGroupUserList> ListGroupUsersAsync(ISession session, string groupId, int? state,
            int limit, string cursor, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListGroupUsersAsync(session.AuthToken, groupId, limit, state, cursor, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListGroupsAsync"/>
        public async Task<IApiGroupList> ListGroupsAsync(ISession session, string name = null, int limit = 1,
            string cursor = null, string langTag = null, int? members = null, bool? open = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListGroupsAsync(session.AuthToken, name, cursor, limit, langTag, members, open, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListLeaderboardRecordsAsync"/>
        public async Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAsync(ISession session, string leaderboardId,
            IEnumerable<string> ownerIds = null, long? expiry = null, int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListLeaderboardRecordsAsync(session.AuthToken, leaderboardId, ownerIds, limit,
                cursor, expiry?.ToString(), canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListLeaderboardRecordsAroundOwnerAsync"/>
        public async Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAroundOwnerAsync(ISession session,
            string leaderboardId, string ownerId, long? expiry = null, int limit = 1, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListLeaderboardRecordsAroundOwnerAsync(session.AuthToken, leaderboardId, ownerId,
                limit, expiry?.ToString(), canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListMatchesAsync"/>
        public async Task<IApiMatchList> ListMatchesAsync(ISession session, int min, int max, int limit,
            bool authoritative, string label, string query, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListMatchesAsync(session.AuthToken, limit, authoritative, label, min, max, query, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListNotificationsAsync"/>
        public async Task<IApiNotificationList> ListNotificationsAsync(ISession session, int limit = 1,
            string cacheableCursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListNotificationsAsync(session.AuthToken, limit, cacheableCursor, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        [Obsolete("ListStorageObjects is obsolete, please use ListStorageObjectsAsync instead.", true)]
        public Task<IApiStorageObjectList> ListStorageObjects(ISession session, string collection, int limit = 1,
            string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default) =>
            _retryInvoker.InvokeWithRetry(() => _apiClient.ListStorageObjectsAsync(session.AuthToken, collection, string.Empty, limit, cursor, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));

        /// <inheritdoc cref="ListStorageObjectsAsync"/>
        public async Task<IApiStorageObjectList> ListStorageObjectsAsync(ISession session, string collection,
            int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListStorageObjectsAsync(session.AuthToken, collection, string.Empty, limit, cursor, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListTournamentRecordsAroundOwnerAsync"/>
        public async Task<IApiTournamentRecordList> ListTournamentRecordsAroundOwnerAsync(ISession session,
            string tournamentId, string ownerId, long? expiry = null, int limit = 1, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListTournamentRecordsAroundOwnerAsync(session.AuthToken, tournamentId, ownerId,
                limit, expiry?.ToString(), canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListTournamentRecordsAsync"/>
        public async Task<IApiTournamentRecordList> ListTournamentRecordsAsync(ISession session, string tournamentId,
            IEnumerable<string> ownerIds = null, long? expiry = null, int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListTournamentRecordsAsync(session.AuthToken, tournamentId, ownerIds, limit, cursor,
                expiry?.ToString(), canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListTournamentsAsync"/>
        public async Task<IApiTournamentList> ListTournamentsAsync(ISession session, int categoryStart, int categoryEnd,
            int? startTime = null, int? endTime = null, int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListTournamentsAsync(session.AuthToken, categoryStart, categoryEnd, startTime,
                endTime, limit, cursor, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListUserGroupsAsync(Nakama.ISession,int?,int,string)"/>
        public Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, int? state, int limit, string cursor, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default) =>
            ListUserGroupsAsync(session, session.UserId, state, limit, cursor);

        /// <inheritdoc cref="ListUserGroupsAsync(Nakama.ISession,string,int?,int,string)"/>
        public async Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, string userId, int? state, int limit,
            string cursor, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListUserGroupsAsync(session.AuthToken, userId, limit, state, cursor, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ListUsersStorageObjectsAsync"/>
        public async Task<IApiStorageObjectList> ListUsersStorageObjectsAsync(ISession session, string collection,
            string userId, int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ListStorageObjects2Async(session.AuthToken, collection, userId, limit, cursor, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="PromoteGroupUsersAsync"/>
        public async Task PromoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.PromoteGroupUsersAsync(session.AuthToken, groupId, ids, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ReadStorageObjectsAsync"/>
        public async Task<IApiStorageObjects> ReadStorageObjectsAsync(ISession session,
            IApiReadStorageObjectId[] ids = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
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

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ReadStorageObjectsAsync(session.AuthToken,
                new ApiReadStorageObjectsRequest {_objectIds = objects}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="RpcAsync(Nakama.ISession,string,string)"/>
        public async Task<IApiRpc> RpcAsync(ISession session, string id, string payload, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.RpcFuncAsync(session.AuthToken, id, payload, null, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="RpcAsync(Nakama.ISession,string)"/>
        public async Task<IApiRpc> RpcAsync(ISession session, string id, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.RpcFunc2Async(session.AuthToken, id, null, null, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="RpcAsync(string,string,string)"/>
        public Task<IApiRpc> RpcAsync(string httpkey, string id, string payload = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default) =>
             _retryInvoker.InvokeWithRetry(() => _apiClient.RpcFunc2Async(null, id, payload, httpkey, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));

        /// <inheritdoc cref="SessionLogoutAsync(Nakama.ISession)"/>
        public Task SessionLogoutAsync(ISession session, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default) => SessionLogoutAsync(session.AuthToken, session.RefreshToken, retryConfiguration, canceller);

        /// <inheritdoc cref="SessionLogoutAsync(string,string)"/>
        public Task SessionLogoutAsync(string authToken, string refreshToken, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default) =>
             _retryInvoker.InvokeWithRetry(() => _apiClient.SessionLogoutAsync(authToken,
                new ApiSessionLogoutRequest {Token = authToken, RefreshToken = refreshToken}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));

        /// <inheritdoc cref="SessionRefreshAsync"/>
        public async Task<ISession> SessionRefreshAsync(ISession session, Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            // NOTE: Warn developers to encourage them to set a suitable session and refresh token lifetime.
            if (session.Created && session.ExpireTime - session.CreateTime < 70)
            {
                Logger.WarnFormat("Session lifetime too short, please set '--session.token_expiry_sec' option. See the documentation for more info: https://heroiclabs.com/docs/install-configuration/#session");
            }

            if (session.Created && session.RefreshExpireTime - session.CreateTime < 3700)
            {
                Logger.WarnFormat("Session refresh lifetime too short, please set '--session.refresh_token_expiry_sec' option. See the documentation for more info: https://heroiclabs.com/docs/install-configuration/#session");
            }

            var response = await  _retryInvoker.InvokeWithRetry(() => _apiClient.SessionRefreshAsync(ServerKey, string.Empty,
                new ApiSessionRefreshRequest {Token = session.RefreshToken, _vars = vars}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));

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
        public async Task UnlinkAppleAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.UnlinkAppleAsync(session.AuthToken, new ApiAccountApple {Token = token}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkCustomAsync"/>
        public async Task UnlinkCustomAsync(ISession session, string id, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.UnlinkCustomAsync(session.AuthToken, new ApiAccountCustom {Id = id}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkDeviceAsync"/>
        public async Task UnlinkDeviceAsync(ISession session, string id, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.UnlinkDeviceAsync(session.AuthToken, new ApiAccountDevice {Id = id}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkEmailAsync"/>
        public async Task UnlinkEmailAsync(ISession session, string email, string password, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.UnlinkEmailAsync(session.AuthToken,
                new ApiAccountEmail {Email = email, Password = password}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkFacebookAsync"/>
        public async Task UnlinkFacebookAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.UnlinkFacebookAsync(session.AuthToken, new ApiAccountFacebook {Token = token}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkGameCenterAsync"/>
        public async Task UnlinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestamp, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.UnlinkGameCenterAsync(
                session.AuthToken,
                new ApiAccountGameCenter
                {
                    BundleId = bundleId,
                    PlayerId = playerId,
                    PublicKeyUrl = publicKeyUrl,
                    Salt = salt,
                    Signature = signature,
                    TimestampSeconds = timestamp
                }, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkGoogleAsync"/>
        public async Task UnlinkGoogleAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.UnlinkGoogleAsync(session.AuthToken, new ApiAccountGoogle {Token = token}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UnlinkSteamAsync"/>
        public async Task UnlinkSteamAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.UnlinkSteamAsync(session.AuthToken, new ApiAccountSteam {Token = token}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UpdateAccountAsync"/>
        public async Task UpdateAccountAsync(ISession session, string username, string displayName = null,
            string avatarUrl = null, string langTag = null, string location = null, string timezone = null,
            RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() => _apiClient.UpdateAccountAsync(
                session.AuthToken, new ApiUpdateAccountRequest
                {
                    AvatarUrl = avatarUrl,
                    DisplayName = displayName,
                    LangTag = langTag,
                    Location = location,
                    Timezone = timezone,
                    Username = username
                }, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="UpdateGroupAsync"/>
        public async Task UpdateGroupAsync(ISession session, string groupId, string name, bool open,
            string description = null, string avatarUrl = null, string langTag = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            await  _retryInvoker.InvokeWithRetry(() =>_apiClient.UpdateGroupAsync(
                session.AuthToken, groupId,
                new ApiUpdateGroupRequest
                {
                    Name = name,
                    Open = open,
                    AvatarUrl = avatarUrl,
                    Description = description,
                    LangTag = langTag
                }, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ValidatePurchaseAppleAsync"/>
        public async Task<IApiValidatePurchaseResponse> ValidatePurchaseAppleAsync(ISession session, string receipt, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ValidatePurchaseAppleAsync(session.AuthToken, new ApiValidatePurchaseAppleRequest
            {
                Receipt = receipt
            }, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ValidatePurchaseGoogleAsync"/>
        public async Task<IApiValidatePurchaseResponse> ValidatePurchaseGoogleAsync(ISession session, string receipt, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ValidatePurchaseGoogleAsync(session.AuthToken, new ApiValidatePurchaseGoogleRequest
            {
                Purchase = receipt
            }, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="ValidatePurchaseHuaweiAsync"/>
        public async Task<IApiValidatePurchaseResponse> ValidatePurchaseHuaweiAsync(ISession session, string receipt, string signature, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.ValidatePurchaseHuaweiAsync(session.AuthToken, new ApiValidatePurchaseHuaweiRequest
            {
                Purchase = receipt,
                Signature = signature
            }, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="WriteLeaderboardRecordAsync"/>
        public async Task<IApiLeaderboardRecord> WriteLeaderboardRecordAsync(ISession session, string leaderboardId,
            long score, long subScore = 0, string metadata = null, ApiOperator apiOperator = ApiOperator.NO_OVERRIDE, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.WriteLeaderboardRecordAsync(
                session.AuthToken, leaderboardId,
                new WriteLeaderboardRecordRequestLeaderboardRecordWrite
                {
                    Metadata = metadata,
                    Score = score.ToString(),
                    Subscore = subScore.ToString(),
                    _operator = apiOperator
                }, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="WriteStorageObjectsAsync"/>
        public async Task<IApiStorageObjectAcks> WriteStorageObjectsAsync(ISession session,
            IApiWriteStorageObject[] objects = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
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

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.WriteStorageObjectsAsync(session.AuthToken,
                new ApiWriteStorageObjectsRequest {_objects = writes}, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        /// <inheritdoc cref="WriteTournamentRecordAsync"/>
        public async Task<IApiLeaderboardRecord> WriteTournamentRecordAsync(ISession session, string tournamentId,
            long score, long subScore = 0, string metadata = null, ApiOperator apiOperator = ApiOperator.NO_OVERRIDE, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session, null, retryConfiguration, canceller);
            }

            return await  _retryInvoker.InvokeWithRetry(() => _apiClient.WriteTournamentRecordAsync(session.AuthToken,
                tournamentId,
                new WriteTournamentRecordRequestTournamentRecordWrite
                {
                    Metadata = metadata,
                    Score = score.ToString(),
                    Subscore = subScore.ToString(),
                    _operator = apiOperator
                }, canceller), new RetryHistory(retryConfiguration ?? GlobalRetryConfiguration, canceller));
        }

        #region Obsolete functions
        public Task AddFriendsAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => AddFriendsAsync(session, ids, usernames, retryConfiguration, canceller.Token);

        public Task AddGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => AddGroupUsersAsync(session, groupId, ids, retryConfiguration, canceller.Token);

        public Task<ISession> AuthenticateAppleAsync(string token, string username, bool create,
            Dictionary<string, string> vars, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => AuthenticateAppleAsync(token, username, create, vars, retryConfiguration, canceller.Token);

        public Task<ISession> AuthenticateCustomAsync(string id, string username, bool create,
            Dictionary<string, string> vars, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => AuthenticateCustomAsync(id, username, create, vars, retryConfiguration, canceller.Token);

        public Task<ISession> AuthenticateDeviceAsync(string id, string username, bool create,
            Dictionary<string, string> vars, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => AuthenticateDeviceAsync(id, username, create, vars, retryConfiguration, canceller.Token);

        public Task<ISession> AuthenticateEmailAsync(string email, string password, string username,
            bool create, Dictionary<string, string> vars, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => AuthenticateEmailAsync(email, password, username, create, vars, retryConfiguration, canceller.Token);

        public Task<ISession> AuthenticateFacebookAsync(string token, string username, bool create,
            bool import, Dictionary<string, string> vars, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => AuthenticateFacebookAsync(token, username, create, import, vars, retryConfiguration, canceller.Token);

        public Task<ISession> AuthenticateGameCenterAsync(string bundleId, string playerId, string publicKeyUrl, string salt,
            string signature, string timestamp, string username, bool create,
            Dictionary<string, string> vars, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => AuthenticateGameCenterAsync(bundleId, playerId, publicKeyUrl, salt, signature, timestamp, username, create, vars, retryConfiguration, canceller.Token);

        public Task<ISession> AuthenticateGoogleAsync(string token, string username, bool create,
            Dictionary<string, string> vars, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => AuthenticateGoogleAsync(token, username, create, vars, retryConfiguration, canceller.Token);

        public Task<ISession> AuthenticateSteamAsync(string token, string username, bool create,
            bool import, Dictionary<string, string> vars, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => AuthenticateSteamAsync(token, username, create, import, vars, retryConfiguration, canceller.Token);

        public Task BanGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => BanGroupUsersAsync(session, groupId, ids, retryConfiguration, canceller.Token);

        public Task BlockFriendsAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => BlockFriendsAsync(session, ids, usernames, retryConfiguration, canceller.Token);

        public Task<IApiGroup> CreateGroupAsync(ISession session, string name, string description,
            string avatarUrl, string langTag, bool open, int maxCount, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => CreateGroupAsync(session, name, description, avatarUrl, langTag, open, maxCount, retryConfiguration, canceller.Token);

        public Task DeleteFriendsAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => DeleteFriendsAsync(session, ids, usernames, retryConfiguration, canceller.Token);

        public Task DeleteGroupAsync(ISession session, string groupId, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => DeleteGroupAsync(session, groupId, retryConfiguration, canceller.Token);

        public Task DeleteLeaderboardRecordAsync(ISession session, string leaderboardId, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => DeleteLeaderboardRecordAsync(session, leaderboardId, retryConfiguration, canceller.Token);

        public Task DeleteNotificationsAsync(ISession session, IEnumerable<string> ids, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => DeleteNotificationsAsync(session, ids, retryConfiguration, canceller.Token);

        public Task DeleteStorageObjectsAsync(ISession session, StorageObjectId[] ids, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => DeleteStorageObjectsAsync(session, ids, retryConfiguration, canceller.Token);

        public Task DemoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> userIds, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => DemoteGroupUsersAsync(session, groupId, userIds, retryConfiguration, canceller.Token);

        public Task EventAsync(ISession session, string name, Dictionary<string, string> properties, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => EventAsync(session, name, properties, retryConfiguration, canceller.Token);

        public Task<IApiAccount> GetAccountAsync(ISession session, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => GetAccountAsync(session, retryConfiguration, canceller.Token);

        public Task<IApiUsers> GetUsersAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames,
            IEnumerable<string> facebookIds, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => GetUsersAsync(session, ids, usernames, facebookIds, retryConfiguration, canceller.Token);

        public Task ImportFacebookFriendsAsync(ISession session, string token, bool? reset, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ImportFacebookFriendsAsync(session, token, reset, retryConfiguration, canceller.Token);

        public Task ImportSteamFriendsAsync(ISession session, string token, bool? reset, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ImportSteamFriendsAsync(session, token, reset, retryConfiguration, canceller.Token);

        public Task JoinGroupAsync(ISession session, string groupId, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => JoinGroupAsync(session, groupId, retryConfiguration, canceller.Token);

        public Task JoinTournamentAsync(ISession session, string tournamentId, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => JoinTournamentAsync(session, tournamentId, retryConfiguration, canceller.Token);

        public Task KickGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => KickGroupUsersAsync(session, groupId, ids, retryConfiguration, canceller.Token);

        public Task LeaveGroupAsync(ISession session, string groupId, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => LeaveGroupAsync(session, groupId, retryConfiguration, canceller.Token);

        public Task LinkAppleAsync(ISession session, string token, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => LinkAppleAsync(session, token, retryConfiguration, canceller.Token);

        public Task LinkCustomAsync(ISession session, string id, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => LinkCustomAsync(session, id, retryConfiguration, canceller.Token);

        public Task LinkDeviceAsync(ISession session, string id, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => LinkDeviceAsync(session, id, retryConfiguration, canceller.Token);

        public Task LinkEmailAsync(ISession session, string email, string password, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => LinkEmailAsync(session, email, password, retryConfiguration, canceller.Token);

        public Task LinkFacebookAsync(ISession session, string token, bool? import, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => LinkFacebookAsync(session, token, import, retryConfiguration, canceller.Token);

        public Task LinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl, string salt,
            string signature, string timestamp, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => LinkGameCenterAsync(session, bundleId, playerId, publicKeyUrl, salt, signature, timestamp, retryConfiguration, canceller.Token);

        public Task LinkGoogleAsync(ISession session, string token, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => LinkGoogleAsync(session, token, retryConfiguration, canceller.Token);

        public Task LinkSteamAsync(ISession session, string token, bool import, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => LinkSteamAsync(session, token, import, retryConfiguration, canceller.Token);

        public Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, IChannel channel, int limit,
            bool forward, string cursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListChannelMessagesAsync(session, channel, limit, forward, cursor, retryConfiguration, canceller.Token);

        public Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, string channelId, int limit,
            bool forward, string cursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListChannelMessagesAsync(session, channelId, limit, forward, cursor, retryConfiguration, canceller.Token);

        public Task<IApiFriendList> ListFriendsAsync(ISession session, int? state, int limit, string cursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListFriendsAsync(session, state, limit, cursor, retryConfiguration, canceller.Token);

        public Task<IApiGroupUserList> ListGroupUsersAsync(ISession session, string groupId, int? state, int limit,
            string cursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListGroupUsersAsync(session, groupId, state, limit, cursor, retryConfiguration, canceller.Token);

        public Task<IApiGroupList> ListGroupsAsync(ISession session, string name, int limit, string cursor, string langTag, int? members, bool? open, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListGroupsAsync(session, name, limit, cursor, langTag, members, open, retryConfiguration, canceller.Token);

        public Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAsync(ISession session, string leaderboardId,
            IEnumerable<string> ownerIds, long? expiry, int limit, string cursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListLeaderboardRecordsAsync(session, leaderboardId, ownerIds, expiry, limit, cursor, retryConfiguration, canceller.Token);

        public Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAroundOwnerAsync(ISession session, string leaderboardId,
            string ownerId, long? expiry, int limit, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListLeaderboardRecordsAroundOwnerAsync(session, leaderboardId, ownerId, expiry, limit, retryConfiguration, canceller.Token);

        public Task<IApiMatchList> ListMatchesAsync(ISession session, int min, int max, int limit, bool authoritative,
            string label, string query, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListMatchesAsync(session, min, max, limit, authoritative, label, query, retryConfiguration, canceller.Token);

        public Task<IApiNotificationList> ListNotificationsAsync(ISession session, int limit,
            string cacheableCursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListNotificationsAsync(session, limit, cacheableCursor, retryConfiguration, canceller.Token);

        public Task<IApiStorageObjectList> ListStorageObjectsAsync(ISession session, string collection, int limit,
            string cursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListStorageObjectsAsync(session, collection, limit, cursor, retryConfiguration, canceller.Token);

        public Task<IApiTournamentRecordList> ListTournamentRecordsAroundOwnerAsync(ISession session, string tournamentId,
            string ownerId, long? expiry, int limit, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListTournamentRecordsAroundOwnerAsync(session, tournamentId, ownerId, expiry, limit, retryConfiguration, canceller.Token);

        public Task<IApiTournamentRecordList> ListTournamentRecordsAsync(ISession session, string tournamentId,
            IEnumerable<string> ownerIds, long? expiry, int limit, string cursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListTournamentRecordsAsync(session, tournamentId, ownerIds, expiry, limit, cursor, retryConfiguration, canceller.Token);

        public Task<IApiTournamentList> ListTournamentsAsync(ISession session, int categoryStart, int categoryEnd,
            int? startTime, int? endTime, int limit, string cursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListTournamentsAsync(session, categoryStart, categoryEnd, startTime, endTime, limit, cursor, retryConfiguration, canceller.Token);

        public Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, int? state, int limit,
            string cursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListUserGroupsAsync(session, state, limit, cursor, retryConfiguration, canceller.Token);

        public Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, string userId, int? state, int limit,
            string cursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListUserGroupsAsync(session, userId, state, limit, cursor, retryConfiguration, canceller.Token);

        public Task<IApiStorageObjectList> ListUsersStorageObjectsAsync(ISession session, string collection, string userId,
            int limit, string cursor, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ListUsersStorageObjectsAsync(session, collection, userId, limit, cursor, retryConfiguration, canceller.Token);

        public Task PromoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => PromoteGroupUsersAsync(session, groupId, ids, retryConfiguration, canceller.Token);

        public Task<IApiStorageObjects> ReadStorageObjectsAsync(ISession session, IApiReadStorageObjectId[] ids, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ReadStorageObjectsAsync(session, ids, retryConfiguration, canceller.Token);

        public Task<IApiRpc> RpcAsync(ISession session, string id, string payload, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => RpcAsync(session, id, payload, retryConfiguration, canceller.Token);

        public Task<IApiRpc> RpcAsync(ISession session, string id, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => RpcAsync(session, id, retryConfiguration, canceller.Token);

        public Task<IApiRpc> RpcAsync(string httpKey, string id, string payload, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => RpcAsync(httpKey, id, payload, retryConfiguration, canceller.Token);

        public Task SessionLogoutAsync(ISession session, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => SessionLogoutAsync(session, retryConfiguration, canceller.Token);

        public Task SessionLogoutAsync(string authToken, string refreshToken, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => SessionLogoutAsync(authToken, refreshToken, retryConfiguration, canceller.Token);

        public Task<ISession> SessionRefreshAsync(ISession session, Dictionary<string, string> vars, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => SessionRefreshAsync(session, vars, retryConfiguration, canceller.Token);

        public Task UnlinkAppleAsync(ISession session, string token, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => UnlinkAppleAsync(session, token, retryConfiguration, canceller.Token);

        public Task UnlinkCustomAsync(ISession session, string id, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => UnlinkCustomAsync(session, id, retryConfiguration, canceller.Token);

        public Task UnlinkDeviceAsync(ISession session, string id, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => UnlinkDeviceAsync(session, id, retryConfiguration, canceller.Token);

        public Task UnlinkEmailAsync(ISession session, string email, string password, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => UnlinkEmailAsync(session, email, password, retryConfiguration, canceller.Token);

        public Task UnlinkFacebookAsync(ISession session, string token, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => UnlinkFacebookAsync(session, token, retryConfiguration, canceller.Token);

        public Task UnlinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl, string salt,
            string signature, string timestamp, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => UnlinkGameCenterAsync(session, bundleId, playerId, publicKeyUrl, salt, signature, timestamp, retryConfiguration, canceller.Token);

        public Task UnlinkGoogleAsync(ISession session, string token, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => UnlinkGoogleAsync(session, token, retryConfiguration, canceller.Token);

        public Task UnlinkSteamAsync(ISession session, string token, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => UnlinkSteamAsync(session, token, retryConfiguration, canceller.Token);

        public Task UpdateAccountAsync(ISession session, string username, string displayName,
            string avatarUrl, string langTag, string location, string timezone, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => UpdateAccountAsync(session, username, displayName, avatarUrl, langTag, location, timezone, retryConfiguration, canceller.Token);

        public Task UpdateGroupAsync(ISession session, string groupId, string name, bool open, string description,
            string avatarUrl, string langTag, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => UpdateGroupAsync(session, groupId, name, open, description, avatarUrl, langTag, retryConfiguration, canceller.Token);

        public Task<IApiValidatePurchaseResponse> ValidatePurchaseAppleAsync(ISession session, string receipt, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ValidatePurchaseAppleAsync(session, receipt, retryConfiguration, canceller.Token);

        public Task<IApiValidatePurchaseResponse> ValidatePurchaseGoogleAsync(ISession session, string receipt, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ValidatePurchaseGoogleAsync(session, receipt, retryConfiguration, canceller.Token);

        public Task<IApiValidatePurchaseResponse> ValidatePurchaseHuaweiAsync(ISession session, string receipt,
            string signature, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => ValidatePurchaseHuaweiAsync(session, receipt, signature, retryConfiguration, canceller.Token);

        public Task<IApiLeaderboardRecord> WriteLeaderboardRecordAsync(ISession session, string leaderboardId, long score,
            long subScore, string metadata, ApiOperator apiOperator, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => WriteLeaderboardRecordAsync(session, leaderboardId, score, subScore, metadata, apiOperator, retryConfiguration, canceller.Token);

        public Task<IApiStorageObjectAcks> WriteStorageObjectsAsync(ISession session, IApiWriteStorageObject[] objects, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => WriteStorageObjectsAsync(session, objects, retryConfiguration, canceller.Token);

        public Task<IApiLeaderboardRecord> WriteTournamentRecordAsync(ISession session, string tournamentId, long score,
            long subScore, string metadata, ApiOperator apiOperator, RetryConfiguration retryConfiguration, CancellationTokenSource canceller)
            => WriteTournamentRecordAsync(session, tournamentId, score, subScore, metadata, apiOperator, retryConfiguration, canceller.Token);
        #endregion
    }
}
