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

        private const int DefaultTimeout = 15;

        public Client(string serverKey, bool autoRefreshSession = true) : this(serverKey, HttpRequestAdapter.WithGzip(),
            autoRefreshSession)
        {
        }

        public Client(string serverKey, IHttpAdapter adapter, bool autoRefreshSession = true) : this(DefaultScheme,
            DefaultHost, DefaultPort, serverKey,
            adapter, autoRefreshSession)
        {
        }

        public Client(string scheme, string host, int port, string serverKey, bool autoRefreshSession = true) : this(
            scheme, host, port, serverKey,
            HttpRequestAdapter.WithGzip(), autoRefreshSession)
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
            IEnumerable<string> usernames = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.AddFriendsAsync(session.AuthToken, ids, usernames);
        }

        /// <inheritdoc cref="AddGroupUsersAsync"/>
        public async Task AddGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.AddGroupUsersAsync(session.AuthToken, groupId, ids);
        }

        /// <inheritdoc cref="AuthenticateAppleAsync"/>
        public async Task<ISession> AuthenticateAppleAsync(string token, string username = null, bool create = true,
            Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateAppleAsync(ServerKey, string.Empty,
                new ApiAccountApple {Token = token, _vars = vars}, create, username);
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateCustomAsync"/>
        public async Task<ISession> AuthenticateCustomAsync(string id, string username = null, bool create = true,
            Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateCustomAsync(ServerKey, string.Empty,
                new ApiAccountCustom {Id = id, _vars = vars}, create, username);
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateDeviceAsync"/>
        public async Task<ISession> AuthenticateDeviceAsync(string id, string username = null, bool create = true,
            Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateDeviceAsync(ServerKey, string.Empty,
                new ApiAccountDevice {Id = id, _vars = vars}, create, username);
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateEmailAsync"/>
        public async Task<ISession> AuthenticateEmailAsync(string email, string password, string username = null,
            bool create = true, Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateEmailAsync(ServerKey, string.Empty,
                new ApiAccountEmail {Email = email, Password = password, _vars = vars}, create, username);
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateFacebookAsync"/>
        public async Task<ISession> AuthenticateFacebookAsync(string token, string username = null, bool create = true,
            bool import = true, Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateFacebookAsync(ServerKey, string.Empty,
                new ApiAccountFacebook {Token = token, _vars = vars}, create, username, import);
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateGameCenterAsync"/>
        public async Task<ISession> AuthenticateGameCenterAsync(string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestamp, string username = null, bool create = true,
            Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateGameCenterAsync(ServerKey, string.Empty,
                new ApiAccountGameCenter
                {
                    BundleId = bundleId,
                    PlayerId = playerId,
                    PublicKeyUrl = publicKeyUrl,
                    Salt = salt,
                    Signature = signature,
                    TimestampSeconds = timestamp,
                    _vars = vars
                }, create, username);
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateGoogleAsync"/>
        public async Task<ISession> AuthenticateGoogleAsync(string token, string username = null, bool create = true,
            Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateGoogleAsync(ServerKey, string.Empty,
                new ApiAccountGoogle {Token = token, _vars = vars}, create, username);
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="AuthenticateSteamAsync"/>
        public async Task<ISession> AuthenticateSteamAsync(string token, string username = null, bool create = true,
            bool import = true, Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateSteamAsync(ServerKey, string.Empty,
                new ApiAccountSteam {Token = token, _vars = vars}, create, username, import);
            return new Session(response.Token, response.RefreshToken, response.Created);
        }

        /// <inheritdoc cref="BanGroupUsersAsync"/>
        public async Task BanGroupUsersAsync(ISession session, string groupId, IEnumerable<string> usernames)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.BanGroupUsersAsync(session.AuthToken, groupId, usernames);
        }

        /// <inheritdoc cref="BlockFriendsAsync"/>
        public async Task BlockFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.BlockFriendsAsync(session.AuthToken, ids, usernames);
        }

        /// <inheritdoc cref="CreateGroupAsync"/>
        public async Task<IApiGroup> CreateGroupAsync(ISession session, string name, string description = "",
            string avatarUrl = null, string langTag = null, bool open = true, int maxCount = 100)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.CreateGroupAsync(session.AuthToken, new ApiCreateGroupRequest
            {
                Name = name,
                Description = description,
                AvatarUrl = avatarUrl,
                LangTag = langTag,
                Open = open,
                MaxCount = maxCount
            });
        }

        /// <inheritdoc cref="DeleteFriendsAsync"/>
        public async Task DeleteFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.DeleteFriendsAsync(session.AuthToken, ids, usernames);
        }

        /// <inheritdoc cref="DeleteGroupAsync"/>
        public async Task DeleteGroupAsync(ISession session, string groupId)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.DeleteGroupAsync(session.AuthToken, groupId);
        }

        /// <inheritdoc cref="DeleteLeaderboardRecordAsync"/>
        public async Task DeleteLeaderboardRecordAsync(ISession session, string leaderboardId)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.DeleteLeaderboardRecordAsync(session.AuthToken, leaderboardId);
        }

        /// <inheritdoc cref="DeleteNotificationsAsync"/>
        public async Task DeleteNotificationsAsync(ISession session, IEnumerable<string> ids)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.DeleteNotificationsAsync(session.AuthToken, ids);
        }

        /// <inheritdoc cref="DeleteStorageObjectsAsync"/>
        public async Task DeleteStorageObjectsAsync(ISession session, params StorageObjectId[] ids)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            var objects = new List<ApiDeleteStorageObjectId>(ids.Length);
            foreach (var id in ids)
            {
                objects.Add(new ApiDeleteStorageObjectId
                {
                    Collection = id.Collection,
                    Key = id.Key,
                    Version = id.Version
                });
            }

            await _apiClient.DeleteStorageObjectsAsync(session.AuthToken,
                new ApiDeleteStorageObjectsRequest {_objectIds = objects});
        }

        /// <inheritdoc cref="DemoteGroupUsersAsync"/>
        public async Task DemoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> usernames)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.DemoteGroupUsersAsync(session.AuthToken, groupId, usernames);
        }

        /// <inheritdoc cref="EventAsync"/>
        public async Task EventAsync(ISession session, string name, Dictionary<string, string> properties)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.EventAsync(session.AuthToken, new ApiEvent
            {
                External = true,
                Name = name,
                _properties = properties
            });
        }

        /// <inheritdoc cref="GetAccountAsync"/>
        public async Task<IApiAccount> GetAccountAsync(ISession session)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.GetAccountAsync(session.AuthToken);
        }

        /// <inheritdoc cref="GetUsersAsync"/>
        public async Task<IApiUsers> GetUsersAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null, IEnumerable<string> facebookIds = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.GetUsersAsync(session.AuthToken, ids, usernames, facebookIds);
        }

        /// <inheritdoc cref="ImportFacebookFriendsAsync"/>
        public async Task ImportFacebookFriendsAsync(ISession session, string token, bool? reset = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.ImportFacebookFriendsAsync(session.AuthToken, new ApiAccountFacebook {Token = token},
                reset);
        }

        /// <inheritdoc cref="ImportSteamFriendsAsync"/>
        public async Task ImportSteamFriendsAsync(ISession session, string token, bool? reset = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.ImportSteamFriendsAsync(session.AuthToken, new ApiAccountSteam {Token = token}, reset);
        }

        /// <inheritdoc cref="JoinGroupAsync"/>
        public async Task JoinGroupAsync(ISession session, string groupId)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.JoinGroupAsync(session.AuthToken, groupId);
        }

        /// <inheritdoc cref="JoinTournamentAsync"/>
        public async Task JoinTournamentAsync(ISession session, string tournamentId)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.JoinTournamentAsync(session.AuthToken, tournamentId);
        }

        /// <inheritdoc cref="KickGroupUsersAsync"/>
        public async Task KickGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.KickGroupUsersAsync(session.AuthToken, groupId, ids);
        }

        /// <inheritdoc cref="LeaveGroupAsync"/>
        public async Task LeaveGroupAsync(ISession session, string groupId)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.LeaveGroupAsync(session.AuthToken, groupId);
        }

        /// <inheritdoc cref="LinkAppleAsync"/>
        public async Task LinkAppleAsync(ISession session, string token)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.LinkAppleAsync(session.AuthToken, new ApiAccountApple {Token = token});
        }

        /// <inheritdoc cref="LinkCustomAsync"/>
        public async Task LinkCustomAsync(ISession session, string id)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.LinkCustomAsync(session.AuthToken, new ApiAccountCustom {Id = id});
        }

        /// <inheritdoc cref="LinkDeviceAsync"/>
        public async Task LinkDeviceAsync(ISession session, string id)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.LinkDeviceAsync(session.AuthToken, new ApiAccountDevice {Id = id});
        }

        /// <inheritdoc cref="LinkEmailAsync"/>
        public async Task LinkEmailAsync(ISession session, string email, string password)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.LinkEmailAsync(session.AuthToken,
                new ApiAccountEmail {Email = email, Password = password});
        }

        /// <inheritdoc cref="LinkFacebookAsync"/>
        public async Task LinkFacebookAsync(ISession session, string token, bool? import = true)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.LinkFacebookAsync(session.AuthToken, new ApiAccountFacebook {Token = token}, import);
        }

        /// <inheritdoc cref="LinkGameCenterAsync"/>
        public async Task LinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestamp)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.LinkGameCenterAsync(session.AuthToken,
                new ApiAccountGameCenter
                {
                    BundleId = bundleId,
                    PlayerId = playerId,
                    PublicKeyUrl = publicKeyUrl,
                    Salt = salt,
                    Signature = signature,
                    TimestampSeconds = timestamp
                });
        }

        /// <inheritdoc cref="LinkGoogleAsync"/>
        public async Task LinkGoogleAsync(ISession session, string token)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.LinkGoogleAsync(session.AuthToken, new ApiAccountGoogle {Token = token});
        }

        /// <inheritdoc cref="LinkSteamAsync"/>
        public async Task LinkSteamAsync(ISession session, string token, bool sync)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.LinkSteamAsync(session.AuthToken,
                new ApiLinkSteamRequest {Sync = sync, _account = new ApiAccountSteam {Token = token}});
        }

        /// <inheritdoc cref="ListChannelMessagesAsync(Nakama.ISession,Nakama.IChannel,int,bool,string)"/>
        public Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, IChannel channel, int limit = 1,
            bool forward = true, string cursor = null) =>
            ListChannelMessagesAsync(session, channel.Id, limit, forward, cursor);

        /// <inheritdoc cref="ListChannelMessagesAsync(Nakama.ISession,string,int,bool,string)"/>
        public async Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, string channelId,
            int limit = 1,
            bool forward = true, string cursor = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListChannelMessagesAsync(session.AuthToken, channelId, limit, forward, cursor);
        }

        /// <inheritdoc cref="ListFriendsAsync"/>
        public async Task<IApiFriendList> ListFriendsAsync(ISession session, int? state, int limit, string cursor)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListFriendsAsync(session.AuthToken, limit, state, cursor);
        }

        /// <inheritdoc cref="ListGroupUsersAsync"/>
        public async Task<IApiGroupUserList> ListGroupUsersAsync(ISession session, string groupId, int? state,
            int limit,
            string cursor)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListGroupUsersAsync(session.AuthToken, groupId, limit, state, cursor);
        }

        /// <inheritdoc cref="ListGroupsAsync"/>
        public async Task<IApiGroupList> ListGroupsAsync(ISession session, string name = null, int limit = 1,
            string cursor = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListGroupsAsync(session.AuthToken, name, cursor, limit);
        }

        /// <inheritdoc cref="ListLeaderboardRecordsAsync"/>
        public async Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAsync(ISession session, string leaderboardId,
            IEnumerable<string> ownerIds = null, long? expiry = null, int limit = 1, string cursor = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListLeaderboardRecordsAsync(session.AuthToken, leaderboardId, ownerIds, limit,
                cursor,
                expiry?.ToString());
        }

        /// <inheritdoc cref="ListLeaderboardRecordsAroundOwnerAsync"/>
        public async Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAroundOwnerAsync(ISession session,
            string leaderboardId, string ownerId, long? expiry = null, int limit = 1)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListLeaderboardRecordsAroundOwnerAsync(session.AuthToken, leaderboardId, ownerId,
                limit,
                expiry?.ToString());
        }

        /// <inheritdoc cref="ListMatchesAsync"/>
        public async Task<IApiMatchList> ListMatchesAsync(ISession session, int min, int max, int limit,
            bool authoritative,
            string label, string query)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListMatchesAsync(session.AuthToken, limit, authoritative, label, min, max, query);
        }

        /// <inheritdoc cref="ListNotificationsAsync"/>
        public async Task<IApiNotificationList> ListNotificationsAsync(ISession session, int limit = 1,
            string cacheableCursor = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListNotificationsAsync(session.AuthToken, limit, cacheableCursor);
        }

        [Obsolete("ListStorageObjects is obsolete, please use ListStorageObjectsAsync instead.", true)]
        public Task<IApiStorageObjectList> ListStorageObjects(ISession session, string collection, int limit = 1,
            string cursor = null) =>
            _apiClient.ListStorageObjectsAsync(session.AuthToken, collection, string.Empty, limit, cursor);

        /// <inheritdoc cref="ListStorageObjectsAsync"/>
        public async Task<IApiStorageObjectList> ListStorageObjectsAsync(ISession session, string collection,
            int limit = 1,
            string cursor = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListStorageObjectsAsync(session.AuthToken, collection, string.Empty, limit, cursor);
        }

        /// <inheritdoc cref="ListTournamentRecordsAroundOwnerAsync"/>
        public async Task<IApiTournamentRecordList> ListTournamentRecordsAroundOwnerAsync(ISession session,
            string tournamentId, string ownerId, long? expiry = null, int limit = 1)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListTournamentRecordsAroundOwnerAsync(session.AuthToken, tournamentId, ownerId,
                limit,
                expiry?.ToString());
        }

        /// <inheritdoc cref="ListTournamentRecordsAsync"/>
        public async Task<IApiTournamentRecordList> ListTournamentRecordsAsync(ISession session, string tournamentId,
            IEnumerable<string> ownerIds = null, long? expiry = null, int limit = 1, string cursor = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListTournamentRecordsAsync(session.AuthToken, tournamentId, ownerIds, limit, cursor,
                expiry?.ToString());
        }

        /// <inheritdoc cref="ListTournamentsAsync"/>
        public async Task<IApiTournamentList> ListTournamentsAsync(ISession session, int categoryStart, int categoryEnd,
            int? startTime = null, int? endTime = null, int limit = 1, string cursor = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListTournamentsAsync(session.AuthToken, categoryStart, categoryEnd, startTime,
                endTime, limit,
                cursor);
        }

        /// <inheritdoc cref="ListUserGroupsAsync(Nakama.ISession,int?,int,string)"/>
        public Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, int? state, int limit, string cursor) =>
            ListUserGroupsAsync(session, session.UserId, state, limit, cursor);

        /// <inheritdoc cref="ListUserGroupsAsync(Nakama.ISession,string,int?,int,string)"/>
        public async Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, string userId, int? state, int limit,
            string cursor)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListUserGroupsAsync(session.AuthToken, userId, limit, state, cursor);
        }

        /// <inheritdoc cref="ListUsersStorageObjectsAsync"/>
        public async Task<IApiStorageObjectList> ListUsersStorageObjectsAsync(ISession session, string collection,
            string userId, int limit = 1, string cursor = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ListStorageObjects2Async(session.AuthToken, collection, userId, limit, cursor);
        }

        /// <inheritdoc cref="PromoteGroupUsersAsync"/>
        public async Task PromoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.PromoteGroupUsersAsync(session.AuthToken, groupId, ids);
        }

        /// <inheritdoc cref="ReadStorageObjectsAsync"/>
        public async Task<IApiStorageObjects> ReadStorageObjectsAsync(ISession session,
            params IApiReadStorageObjectId[] ids)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            var objects = new List<ApiReadStorageObjectId>(ids.Length);
            foreach (var id in ids)
            {
                objects.Add(new ApiReadStorageObjectId
                {
                    Collection = id.Collection,
                    Key = id.Key,
                    UserId = id.UserId
                });
            }

            return await _apiClient.ReadStorageObjectsAsync(session.AuthToken,
                new ApiReadStorageObjectsRequest {_objectIds = objects});
        }

        /// <inheritdoc cref="RpcAsync(Nakama.ISession,string,string)"/>
        public async Task<IApiRpc> RpcAsync(ISession session, string id, string payload)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.RpcFuncAsync(session.AuthToken, id, payload, null);
        }

        /// <inheritdoc cref="RpcAsync(Nakama.ISession,string)"/>
        public async Task<IApiRpc> RpcAsync(ISession session, string id)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.RpcFunc2Async(session.AuthToken, id, null, null);
        }

        /// <inheritdoc cref="RpcAsync(string,string,string)"/>
        public Task<IApiRpc> RpcAsync(string httpkey, string id, string payload = null) =>
            _apiClient.RpcFunc2Async(null, id, payload, httpkey);

        /// <inheritdoc cref="SessionLogoutAsync(Nakama.ISession)"/>
        public Task SessionLogoutAsync(ISession session) => SessionLogoutAsync(session.AuthToken, session.RefreshToken);

        /// <inheritdoc cref="SessionLogoutAsync(string,string)"/>
        public Task SessionLogoutAsync(string authToken, string refreshToken) =>
            _apiClient.SessionLogoutAsync(authToken,
                new ApiSessionLogoutRequest {Token = authToken, RefreshToken = refreshToken});

        /// <inheritdoc cref="SessionRefreshAsync"/>
        public async Task<ISession> SessionRefreshAsync(ISession session, Dictionary<string, string> vars = null)
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

            var response = await _apiClient.SessionRefreshAsync(ServerKey, string.Empty,
                new ApiSessionRefreshRequest {Token = session.RefreshToken, _vars = vars});

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
        public async Task UnlinkAppleAsync(ISession session, string token)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.UnlinkAppleAsync(session.AuthToken, new ApiAccountApple {Token = token});
        }

        /// <inheritdoc cref="UnlinkCustomAsync"/>
        public async Task UnlinkCustomAsync(ISession session, string id)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.UnlinkCustomAsync(session.AuthToken, new ApiAccountCustom {Id = id});
        }

        /// <inheritdoc cref="UnlinkDeviceAsync"/>
        public async Task UnlinkDeviceAsync(ISession session, string id)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.UnlinkDeviceAsync(session.AuthToken, new ApiAccountDevice {Id = id});
        }

        /// <inheritdoc cref="UnlinkEmailAsync"/>
        public async Task UnlinkEmailAsync(ISession session, string email, string password)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.UnlinkEmailAsync(session.AuthToken,
                new ApiAccountEmail {Email = email, Password = password});
        }

        /// <inheritdoc cref="UnlinkFacebookAsync"/>
        public async Task UnlinkFacebookAsync(ISession session, string token)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.UnlinkFacebookAsync(session.AuthToken, new ApiAccountFacebook {Token = token});
        }

        /// <inheritdoc cref="UnlinkGameCenterAsync"/>
        public async Task UnlinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestamp)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.UnlinkGameCenterAsync(
                session.AuthToken,
                new ApiAccountGameCenter
                {
                    BundleId = bundleId,
                    PlayerId = playerId,
                    PublicKeyUrl = publicKeyUrl,
                    Salt = salt,
                    Signature = signature,
                    TimestampSeconds = timestamp
                });
        }

        /// <inheritdoc cref="UnlinkGoogleAsync"/>
        public async Task UnlinkGoogleAsync(ISession session, string token)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.UnlinkGoogleAsync(session.AuthToken, new ApiAccountGoogle {Token = token});
        }

        /// <inheritdoc cref="UnlinkSteamAsync"/>
        public async Task UnlinkSteamAsync(ISession session, string token)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.UnlinkSteamAsync(session.AuthToken, new ApiAccountSteam {Token = token});
        }

        /// <inheritdoc cref="UpdateAccountAsync"/>
        public async Task UpdateAccountAsync(ISession session, string username, string displayName = null,
            string avatarUrl = null, string langTag = null, string location = null, string timezone = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.UpdateAccountAsync(
                session.AuthToken, new ApiUpdateAccountRequest
                {
                    AvatarUrl = avatarUrl,
                    DisplayName = displayName,
                    LangTag = langTag,
                    Location = location,
                    Timezone = timezone,
                    Username = username
                });
        }

        /// <inheritdoc cref="UpdateGroupAsync"/>
        public async Task UpdateGroupAsync(ISession session, string groupId, string name, bool open,
            string description = null, string avatarUrl = null, string langTag = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            await _apiClient.UpdateGroupAsync(
                session.AuthToken, groupId,
                new ApiUpdateGroupRequest
                {
                    Name = name,
                    Open = open,
                    AvatarUrl = avatarUrl,
                    Description = description,
                    LangTag = langTag
                });
        }

        /// <inheritdoc cref="ValidatePurchaseAppleAsync"/>
        public async Task<IApiValidatePurchaseResponse> ValidatePurchaseAppleAsync(ISession session, string receipt)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ValidatePurchaseAppleAsync(session.AuthToken, new ApiValidatePurchaseAppleRequest
            {
                Receipt = receipt
            });
        }

        /// <inheritdoc cref="ValidatePurchaseGoogleAsync"/>
        public async Task<IApiValidatePurchaseResponse> ValidatePurchaseGoogleAsync(ISession session, string receipt)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ValidatePurchaseGoogleAsync(session.AuthToken, new ApiValidatePurchaseGoogleRequest
            {
                Purchase = receipt
            });
        }

        /// <inheritdoc cref="ValidatePurchaseHuaweiAsync"/>
        public async Task<IApiValidatePurchaseResponse> ValidatePurchaseHuaweiAsync(ISession session, string receipt, string signature)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.ValidatePurchaseHuaweiAsync(session.AuthToken, new ApiValidatePurchaseHuaweiRequest
            {
                Purchase = receipt,
                Signature = signature
            });
        }

        /// <inheritdoc cref="WriteLeaderboardRecordAsync"/>
        public async Task<IApiLeaderboardRecord> WriteLeaderboardRecordAsync(ISession session, string leaderboardId,
            long score, long subScore = 0, string metadata = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.WriteLeaderboardRecordAsync(
                session.AuthToken, leaderboardId,
                new WriteLeaderboardRecordRequestLeaderboardRecordWrite
                {
                    Metadata = metadata,
                    Score = score.ToString(),
                    Subscore = subScore.ToString()
                });
        }

        /// <inheritdoc cref="WriteStorageObjectsAsync"/>
        public async Task<IApiStorageObjectAcks> WriteStorageObjectsAsync(ISession session,
            params IApiWriteStorageObject[] objects)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
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

            return await _apiClient.WriteStorageObjectsAsync(session.AuthToken,
                new ApiWriteStorageObjectsRequest {_objects = writes});
        }

        /// <inheritdoc cref="WriteTournamentRecordAsync"/>
        public async Task<IApiLeaderboardRecord> WriteTournamentRecordAsync(ISession session, string tournamentId,
            long score,
            long subScore = 0, string metadata = null)
        {
            if (AutoRefreshSession && !string.IsNullOrEmpty(session.RefreshToken) &&
                session.HasExpired(DateTime.UtcNow.Add(DefaultExpiredTimeSpan)))
            {
                await SessionRefreshAsync(session);
            }

            return await _apiClient.WriteTournamentRecordAsync(session.AuthToken,
                tournamentId,
                new WriteTournamentRecordRequestTournamentRecordWrite
                {
                    Metadata = metadata,
                    Score = score.ToString(),
                    Subscore = subScore.ToString()
                });
        }
    }
}
