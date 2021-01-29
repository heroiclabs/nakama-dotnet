/**
 * Copyright 2018 The Nakama Authors
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

        public Client(string serverKey) : this(serverKey, HttpRequestAdapter.WithGzip())
        {
        }

        public Client(string serverKey, IHttpAdapter adapter) : this(DefaultScheme, DefaultHost, DefaultPort, serverKey,
            adapter)
        {
        }

        public Client(string scheme, string host, int port, string serverKey) : this(scheme, host, port, serverKey,
            HttpRequestAdapter.WithGzip())
        {
        }

        public Client(string scheme, string host, int port, string serverKey, IHttpAdapter adapter)
        {
            Host = host;
            Port = port;
            Scheme = scheme;
            ServerKey = serverKey;
            Timeout = 15;

            _apiClient = new ApiClient(new UriBuilder(scheme, host, port).Uri, adapter, Timeout);
            Logger = NullLogger.Instance; // must set logger last.
        }

        /// <inheritdoc cref="AuthenticateAppleAsync"/>
        public async Task<ISession> AuthenticateAppleAsync(string token, string username = null, bool create = true, Dictionary<string, string> vars = null) {
            var response = await _apiClient.AuthenticateAppleAsync(ServerKey, string.Empty, new ApiAccountApple {Token = token, _vars = vars}, create, username);
            return new Session(response.Token, response.Created);
        }

        /// <inheritdoc cref="AddFriendsAsync"/>
        public Task AddFriendsAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames = null) =>
            _apiClient.AddFriendsAsync(session.AuthToken, ids, usernames);

        /// <inheritdoc cref="AddGroupUsersAsync"/>
        public Task AddGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids) =>
            _apiClient.AddGroupUsersAsync(session.AuthToken, groupId, ids);

        /// <inheritdoc cref="AuthenticateCustomAsync"/>
        public async Task<ISession> AuthenticateCustomAsync(string id, string username = null, bool create = true,
            Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateCustomAsync(ServerKey, string.Empty,
                new ApiAccountCustom {Id = id, _vars = vars}, create, username);
            return new Session(response.Token, response.Created);
        }

        /// <inheritdoc cref="AuthenticateDeviceAsync"/>
        public async Task<ISession> AuthenticateDeviceAsync(string id, string username = null, bool create = true,
            Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateDeviceAsync(ServerKey, string.Empty,
                new ApiAccountDevice {Id = id, _vars = vars}, create, username);
            return new Session(response.Token, response.Created);
        }

        /// <inheritdoc cref="AuthenticateEmailAsync"/>
        public async Task<ISession> AuthenticateEmailAsync(string email, string password, string username = null,
            bool create = true, Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateEmailAsync(ServerKey, string.Empty,
                new ApiAccountEmail {Email = email, Password = password, _vars = vars}, create, username);
            return new Session(response.Token, response.Created);
        }

        /// <inheritdoc cref="AuthenticateFacebookAsync"/>
        public async Task<ISession> AuthenticateFacebookAsync(string token, string username = null, bool create = true,
            bool import = true, Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateFacebookAsync(ServerKey, string.Empty,
                new ApiAccountFacebook {Token = token, _vars = vars}, create, username, import);
            return new Session(response.Token, response.Created);
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
            return new Session(response.Token, response.Created);
        }

        /// <inheritdoc cref="AuthenticateGoogleAsync"/>
        public async Task<ISession> AuthenticateGoogleAsync(string token, string username = null, bool create = true,
            Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateGoogleAsync(ServerKey, string.Empty,
                new ApiAccountGoogle {Token = token, _vars = vars}, create, username);
            return new Session(response.Token, response.Created);
        }

        /// <inheritdoc cref="AuthenticateSteamAsync"/>
        public async Task<ISession> AuthenticateSteamAsync(string token, string username = null, bool create = true,
            Dictionary<string, string> vars = null)
        {
            var response = await _apiClient.AuthenticateSteamAsync(ServerKey, string.Empty,
                new ApiAccountSteam {Token = token, _vars = vars}, create, username);
            return new Session(response.Token, response.Created);
        }

        /// <inheritdoc cref="BanGroupUsersAsync"/>
        public Task BanGroupUsersAsync(ISession session, string groupId,
            IEnumerable<string> usernames) => _apiClient.BanGroupUsersAsync(session.AuthToken, groupId, usernames);

        /// <inheritdoc cref="BlockFriendsAsync"/>
        public Task BlockFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null) => _apiClient.BlockFriendsAsync(session.AuthToken, ids, usernames);

        /// <inheritdoc cref="CreateGroupAsync"/>
        public Task<IApiGroup> CreateGroupAsync(ISession session, string name, string description = "",
            string avatarUrl = null, string langTag = null, bool open = true, int maxCount = 100) =>
            _apiClient.CreateGroupAsync(session.AuthToken, new ApiCreateGroupRequest
            {
                Name = name,
                Description = description,
                AvatarUrl = avatarUrl,
                LangTag = langTag,
                Open = open,
                MaxCount = maxCount
            });

        /// <inheritdoc cref="DeleteFriendsAsync"/>
        public Task DeleteFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null) => _apiClient.DeleteFriendsAsync(session.AuthToken, ids, usernames);

        /// <inheritdoc cref="DeleteGroupAsync"/>
        public Task DeleteGroupAsync(ISession session, string groupId) =>
            _apiClient.DeleteGroupAsync(session.AuthToken, groupId);

        /// <inheritdoc cref="DeleteLeaderboardRecordAsync"/>
        public Task DeleteLeaderboardRecordAsync(ISession session, string leaderboardId) =>
            _apiClient.DeleteLeaderboardRecordAsync(session.AuthToken, leaderboardId);

        /// <inheritdoc cref="DeleteNotificationsAsync"/>
        public Task DeleteNotificationsAsync(ISession session, IEnumerable<string> ids) =>
            _apiClient.DeleteNotificationsAsync(session.AuthToken, ids);

        /// <inheritdoc cref="DeleteStorageObjectsAsync"/>
        public Task DeleteStorageObjectsAsync(ISession session, params StorageObjectId[] ids)
        {
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

            return _apiClient.DeleteStorageObjectsAsync(session.AuthToken,
                new ApiDeleteStorageObjectsRequest {_objectIds = objects});
        }

        /// <inheritdoc cref="DemoteGroupUsersAsync"/>
        public Task DemoteGroupUsersAsync(ISession session, string groupId,
            IEnumerable<string> usernames) => _apiClient.DemoteGroupUsersAsync(session.AuthToken, groupId, usernames);

         /// <inheritdoc cref="EventAsync"/>
        public Task EventAsync(ISession session, string name, Dictionary<string, string> properties) => _apiClient.EventAsync(session.AuthToken, new ApiEvent{
            External = true,
            Name = name,
            _properties = properties
        });

        /// <inheritdoc cref="GetAccountAsync"/>
        public Task<IApiAccount> GetAccountAsync(ISession session) => _apiClient.GetAccountAsync(session.AuthToken);

        /// <inheritdoc cref="GetUsersAsync"/>
        public Task<IApiUsers> GetUsersAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null, IEnumerable<string> facebookIds = null) =>
            _apiClient.GetUsersAsync(session.AuthToken, ids, usernames, facebookIds);

        /// <inheritdoc cref="ImportFacebookFriendsAsync"/>
        public Task ImportFacebookFriendsAsync(ISession session, string token, bool? reset = null) =>
            _apiClient.ImportFacebookFriendsAsync(session.AuthToken, new ApiAccountFacebook {Token = token}, reset);

        /// <inheritdoc cref="JoinGroupAsync"/>
        public Task JoinGroupAsync(ISession session, string groupId) =>
            _apiClient.JoinGroupAsync(session.AuthToken, groupId);

        /// <inheritdoc cref="JoinTournamentAsync"/>
        public Task JoinTournamentAsync(ISession session, string tournamentId) =>
            _apiClient.JoinTournamentAsync(session.AuthToken, tournamentId);

        /// <inheritdoc cref="KickGroupUsersAsync"/>
        public Task KickGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids) =>
            _apiClient.KickGroupUsersAsync(session.AuthToken, groupId, ids);

        /// <inheritdoc cref="LeaveGroupAsync"/>
        public Task LeaveGroupAsync(ISession session, string groupId) =>
            _apiClient.LeaveGroupAsync(session.AuthToken, groupId);

        /// <inheritdoc cref="LinkAppleAsync"/>
        public Task LinkAppleAsync(ISession session, string token) =>
            _apiClient.LinkAppleAsync(session.AuthToken, new ApiAccountApple {Token = token});

        /// <inheritdoc cref="LinkCustomAsync"/>
        public Task LinkCustomAsync(ISession session, string id) =>
            _apiClient.LinkCustomAsync(session.AuthToken, new ApiAccountCustom {Id = id});

        /// <inheritdoc cref="LinkDeviceAsync"/>
        public Task LinkDeviceAsync(ISession session, string id) =>
            _apiClient.LinkDeviceAsync(session.AuthToken, new ApiAccountDevice {Id = id});

        /// <inheritdoc cref="LinkEmailAsync"/>
        public Task LinkEmailAsync(ISession session, string email, string password) =>
            _apiClient.LinkEmailAsync(session.AuthToken, new ApiAccountEmail {Email = email, Password = password});

        /// <inheritdoc cref="LinkFacebookAsync"/>
        public Task LinkFacebookAsync(ISession session, string token, bool? import = true) =>
            _apiClient.LinkFacebookAsync(session.AuthToken, new ApiAccountFacebook {Token = token}, import);

        /// <inheritdoc cref="LinkGameCenterAsync"/>
        public Task LinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestamp) => _apiClient.LinkGameCenterAsync(session.AuthToken,
            new ApiAccountGameCenter
            {
                BundleId = bundleId,
                PlayerId = playerId,
                PublicKeyUrl = publicKeyUrl,
                Salt = salt,
                Signature = signature,
                TimestampSeconds = timestamp
            });

        /// <inheritdoc cref="LinkGoogleAsync"/>
        public Task LinkGoogleAsync(ISession session, string token) =>
            _apiClient.LinkGoogleAsync(session.AuthToken, new ApiAccountGoogle {Token = token});

        /// <inheritdoc cref="LinkSteamAsync"/>
        public Task LinkSteamAsync(ISession session, string token) =>
            _apiClient.LinkSteamAsync(session.AuthToken, new ApiAccountSteam {Token = token});

        /// <inheritdoc cref="ListChannelMessagesAsync(Nakama.ISession,Nakama.IChannel,int,bool,string)"/>
        public Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, IChannel channel, int limit = 1,
            bool forward = true, string cursor = null) =>
            ListChannelMessagesAsync(session, channel.Id, limit, forward, cursor);

        /// <inheritdoc cref="ListChannelMessagesAsync(Nakama.ISession,string,int,bool,string)"/>
        public Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, string channelId, int limit = 1,
            bool forward = true, string cursor = null) =>
            _apiClient.ListChannelMessagesAsync(session.AuthToken, channelId, limit, forward, cursor);

        /// <inheritdoc cref="ListFriendsAsync"/>
        public Task<IApiFriendList> ListFriendsAsync(ISession session, int? state, int limit, string cursor) =>
            _apiClient.ListFriendsAsync(session.AuthToken, limit, state, cursor);

        /// <inheritdoc cref="ListGroupUsersAsync"/>
        public Task<IApiGroupUserList> ListGroupUsersAsync(ISession session, string groupId, int? state, int limit,
            string cursor) =>
            _apiClient.ListGroupUsersAsync(session.AuthToken, groupId, limit, state, cursor);

        /// <inheritdoc cref="ListGroupsAsync"/>
        public Task<IApiGroupList> ListGroupsAsync(ISession session, string name = null, int limit = 1,
            string cursor = null) => _apiClient.ListGroupsAsync(session.AuthToken, name, cursor, limit);

        /// <inheritdoc cref="ListLeaderboardRecordsAsync"/>
        public Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAsync(ISession session, string leaderboardId,
            IEnumerable<string> ownerIds = null, long? expiry = null, int limit = 1, string cursor = null) =>
            _apiClient.ListLeaderboardRecordsAsync(session.AuthToken, leaderboardId, ownerIds, limit, cursor,
                expiry?.ToString());

        /// <inheritdoc cref="ListLeaderboardRecordsAroundOwnerAsync"/>
        public Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAroundOwnerAsync(ISession session,
            string leaderboardId, string ownerId, long? expiry = null, int limit = 1) =>
            _apiClient.ListLeaderboardRecordsAroundOwnerAsync(session.AuthToken, leaderboardId, ownerId, limit,
                expiry?.ToString());

        /// <inheritdoc cref="ListMatchesAsync"/>
        public Task<IApiMatchList> ListMatchesAsync(ISession session, int min, int max, int limit, bool authoritative,
            string label, string query) =>
            _apiClient.ListMatchesAsync(session.AuthToken, limit, authoritative, label, min, max, query);

        /// <inheritdoc cref="ListNotificationsAsync"/>
        public Task<IApiNotificationList> ListNotificationsAsync(ISession session, int limit = 1,
            string cacheableCursor = null) =>
            _apiClient.ListNotificationsAsync(session.AuthToken, limit, cacheableCursor);

        [Obsolete("ListStorageObjects is obsolete, please use ListStorageObjectsAsync instead.", false)]
        public Task<IApiStorageObjectList> ListStorageObjects(ISession session, string collection, int limit = 1,
            string cursor = null) =>
            _apiClient.ListStorageObjectsAsync(session.AuthToken, collection, string.Empty, limit, cursor);

        /// <inheritdoc cref="ListStorageObjectsAsync"/>
        public Task<IApiStorageObjectList> ListStorageObjectsAsync(ISession session, string collection, int limit = 1,
            string cursor = null) =>
            _apiClient.ListStorageObjectsAsync(session.AuthToken, collection, string.Empty, limit, cursor);

        /// <inheritdoc cref="ListTournamentRecordsAroundOwnerAsync"/>
        public Task<IApiTournamentRecordList> ListTournamentRecordsAroundOwnerAsync(ISession session,
            string tournamentId, string ownerId, long? expiry = null, int limit = 1) =>
            _apiClient.ListTournamentRecordsAroundOwnerAsync(session.AuthToken, tournamentId, ownerId, limit,
                expiry?.ToString());

        /// <inheritdoc cref="ListTournamentRecordsAsync"/>
        public Task<IApiTournamentRecordList> ListTournamentRecordsAsync(ISession session, string tournamentId,
            IEnumerable<string> ownerIds = null, long? expiry = null, int limit = 1, string cursor = null) =>
            _apiClient.ListTournamentRecordsAsync(session.AuthToken, tournamentId, ownerIds, limit, cursor,
                expiry?.ToString());

        /// <inheritdoc cref="ListTournamentsAsync"/>
        public Task<IApiTournamentList> ListTournamentsAsync(ISession session, int categoryStart, int categoryEnd,
            int startTime, int endTime, int limit = 1, string cursor = null) =>
            _apiClient.ListTournamentsAsync(session.AuthToken, categoryStart, categoryEnd, startTime, endTime, limit,
                cursor);

        /// <inheritdoc cref="ListUserGroupsAsync(Nakama.ISession,int?,int,string)"/>
        public Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, int? state, int limit, string cursor) =>
            ListUserGroupsAsync(session, session.UserId, state, limit, cursor);

        /// <inheritdoc cref="ListUserGroupsAsync(Nakama.ISession,string,int?,int,string)"/>
        public Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, string userId, int? state, int limit,
            string cursor) =>
            _apiClient.ListUserGroupsAsync(session.AuthToken, userId, limit, state, cursor);

        /// <inheritdoc cref="ListUsersStorageObjectsAsync"/>
        public Task<IApiStorageObjectList> ListUsersStorageObjectsAsync(ISession session, string collection,
            string userId, int limit = 1, string cursor = null) =>
            _apiClient.ListStorageObjects2Async(session.AuthToken, collection, userId, limit, cursor);

        /// <inheritdoc cref="PromoteGroupUsersAsync"/>
        public Task PromoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids) =>
            _apiClient.PromoteGroupUsersAsync(session.AuthToken, groupId, ids);

        /// <inheritdoc cref="ReadStorageObjectsAsync"/>
        public Task<IApiStorageObjects> ReadStorageObjectsAsync(ISession session, params IApiReadStorageObjectId[] ids)
        {
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

            return _apiClient.ReadStorageObjectsAsync(session.AuthToken,
                new ApiReadStorageObjectsRequest {_objectIds = objects});
        }

        /// <inheritdoc cref="RpcAsync(Nakama.ISession,string,string)"/>
        public Task<IApiRpc> RpcAsync(ISession session, string id, string payload) =>
            _apiClient.RpcFuncAsync(session.AuthToken, id, payload, null);

        /// <inheritdoc cref="RpcAsync(Nakama.ISession,string)"/>
        public Task<IApiRpc> RpcAsync(ISession session, string id) =>
            _apiClient.RpcFunc2Async(session.AuthToken, id, null, null);

        /// <inheritdoc cref="RpcAsync(string,string,string)"/>
        public Task<IApiRpc> RpcAsync(string httpkey, string id, string payload = null) =>
            _apiClient.RpcFunc2Async(null, id, payload, httpkey);

        public override string ToString()
        {
            return $"Client(Host='{Host}', Port={Port}, Scheme='{Scheme}', ServerKey='{ServerKey}', Timeout={Timeout})";
        }

        /// <inheritdoc cref="UnlinkAppleAsync"/>
        public Task UnlinkAppleAsync(ISession session, string token) =>
            _apiClient.UnlinkAppleAsync(session.AuthToken, new ApiAccountApple {Token = token});

        /// <inheritdoc cref="UnlinkCustomAsync"/>
        public Task UnlinkCustomAsync(ISession session, string id) =>
            _apiClient.UnlinkCustomAsync(session.AuthToken, new ApiAccountCustom {Id = id});

        /// <inheritdoc cref="UnlinkDeviceAsync"/>
        public Task UnlinkDeviceAsync(ISession session, string id) =>
            _apiClient.UnlinkDeviceAsync(session.AuthToken, new ApiAccountDevice {Id = id});

        /// <inheritdoc cref="UnlinkEmailAsync"/>
        public Task UnlinkEmailAsync(ISession session, string email, string password) =>
            _apiClient.UnlinkEmailAsync(session.AuthToken, new ApiAccountEmail {Email = email, Password = password});

        /// <inheritdoc cref="UnlinkFacebookAsync"/>
        public Task UnlinkFacebookAsync(ISession session, string token) =>
            _apiClient.UnlinkFacebookAsync(session.AuthToken, new ApiAccountFacebook {Token = token});

        /// <inheritdoc cref="UnlinkGameCenterAsync"/>
        public Task UnlinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestamp) => _apiClient.UnlinkGameCenterAsync(
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

        /// <inheritdoc cref="UnlinkGoogleAsync"/>
        public Task UnlinkGoogleAsync(ISession session, string token) =>
            _apiClient.UnlinkGoogleAsync(session.AuthToken, new ApiAccountGoogle {Token = token});

        /// <inheritdoc cref="UnlinkSteamAsync"/>
        public Task UnlinkSteamAsync(ISession session, string token) =>
            _apiClient.UnlinkSteamAsync(session.AuthToken, new ApiAccountSteam {Token = token});

        /// <inheritdoc cref="UpdateAccountAsync"/>
        public Task UpdateAccountAsync(ISession session, string username, string displayName = null,
            string avatarUrl = null, string langTag = null, string location = null, string timezone = null) =>
            _apiClient.UpdateAccountAsync(
                session.AuthToken, new ApiUpdateAccountRequest
                {
                    AvatarUrl = avatarUrl,
                    DisplayName = displayName,
                    LangTag = langTag,
                    Location = location,
                    Timezone = timezone,
                    Username = username
                });

        /// <inheritdoc cref="UpdateGroupAsync"/>
        public Task UpdateGroupAsync(ISession session, string groupId, string name, bool open,
            string description = null,
            string avatarUrl = null, string langTag = null) => _apiClient.UpdateGroupAsync(
            session.AuthToken, groupId,
            new ApiUpdateGroupRequest
            {
                Name = name,
                Open = open,
                AvatarUrl = avatarUrl,
                Description = description,
                LangTag = langTag
            });

        /// <inheritdoc cref="WriteLeaderboardRecordAsync"/>
        public Task<IApiLeaderboardRecord> WriteLeaderboardRecordAsync(ISession session, string leaderboardId,
            long score, long subScore = 0, string metadata = null) => _apiClient.WriteLeaderboardRecordAsync(
            session.AuthToken, leaderboardId,
            new WriteLeaderboardRecordRequestLeaderboardRecordWrite
            {
                Metadata = metadata,
                Score = score.ToString(),
                Subscore = subScore.ToString()
            });

        /// <inheritdoc cref="WriteStorageObjectsAsync"/>
        public Task<IApiStorageObjectAcks> WriteStorageObjectsAsync(ISession session,
            params IApiWriteStorageObject[] objects)
        {
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

            return _apiClient.WriteStorageObjectsAsync(session.AuthToken,
                new ApiWriteStorageObjectsRequest {_objects = writes});
        }

        /// <inheritdoc cref="WriteTournamentRecordAsync"/>
        public Task<IApiLeaderboardRecord> WriteTournamentRecordAsync(ISession session, string tournamentId, long score,
            long subScore = 0, string metadata = null) => _apiClient.WriteTournamentRecordAsync(session.AuthToken,
            tournamentId,
            new WriteTournamentRecordRequestTournamentRecordWrite
            {
                Metadata = metadata,
                Score = score.ToString(),
                Subscore = subScore.ToString()
            });
    }
}
