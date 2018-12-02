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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nakama
{
    /// <inheritdoc />
    public class Client : IClient
    {
        private readonly ApiClient _apiClient;

        private readonly ClientOptions _options;

        public Client(string serverKey = ClientOptions.DefaultServerKey, string host = ClientOptions.DefaultHost,
            int port = ClientOptions.DefaultPort,
            bool secure = false) : this(new ClientOptions
        {
            EnableSsl = secure,
            Host = host,
            Port = port,
            ServerKey = serverKey
        })
        {
        }

        public Client(ClientOptions options)
        {
            // FIXME move into ClientOptions object.
            Logger = NullLogger.Instance; // dont log by default.
            Retries = 3;
            Trace = false;
            Timeout = 5000;

            options.ValidateOptions();
            _options = options.Clone();

            ServicePointManager.ServerCertificateValidationCallback = _options.ServerCertificateValidationCallback;

            // Use GZip compression with request/responses.
            var handler = new HttpClientHandler();
            if (handler.SupportsAutomaticDecompression && _options.AutomaticDecompression)
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            var httpClient = new HttpClient(handler);
            if (_options.AutomaticCompression) httpClient = new HttpClient(new GZipHttpClientHandler(handler));

            var scheme = _options.EnableSsl ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            _apiClient = new ApiClient(new UriBuilder(scheme, _options.Host, _options.Port).Uri, httpClient);
        }

        /// <inheritdoc />
        public string Host => _options.Host;

        /// <inheritdoc />
        public ILogger Logger { get; set; }

        /// <inheritdoc />
        public int Port => _options.Port;

        /// <inheritdoc />
        public int Retries { get; set; }

        /// <inheritdoc />
        public string ServerKey => _options.ServerKey;

        /// <inheritdoc />
        public bool Secure => _options.EnableSsl;

        /// <inheritdoc />
        public bool Trace { get; set; }

        /// <inheritdoc />
        public int Timeout { get; set; }

        /// <inheritdoc />
        public async Task AddFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null)
        {
            await _apiClient.AddFriendsAsync(session.AuthToken, ids, usernames);
        }

        /// <inheritdoc />
        public async Task AddGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids)
        {
            await _apiClient.AddGroupUsersAsync(session.AuthToken, groupId, ids);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateCustomAsync(string id, string username = null, bool create = true)
        {
            var request = new ApiAccountCustom {Id = id};
            var resp = await _apiClient.AuthenticateCustomAsync(ServerKey, string.Empty, request, create, username);
            return Session.Restore(resp.Token, resp.Created);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateDeviceAsync(string id, string username = null, bool create = true)
        {
            var request = new ApiAccountDevice {Id = id};
            var resp = await _apiClient.AuthenticateDeviceAsync(ServerKey, string.Empty, request, create, username);
            return Session.Restore(resp.Token, resp.Created);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateEmailAsync(string email, string password, string username = null,
            bool create = true)
        {
            var request = new ApiAccountEmail {Email = email, Password = password};
            var resp = await _apiClient.AuthenticateEmailAsync(ServerKey, string.Empty, request, create, username);
            return Session.Restore(resp.Token, resp.Created);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateFacebookAsync(string token, string username = null, bool create = true,
            bool import = true)
        {
            var request = new ApiAccountFacebook {Token = token};
            var resp = await _apiClient.AuthenticateFacebookAsync(ServerKey, string.Empty, request, create, username,
                import);
            return Session.Restore(resp.Token, resp.Created);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateGameCenterAsync(string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestampSeconds, string username = null, bool create = true)
        {
            var request = new ApiAccountGameCenter
            {
                BundleId = bundleId,
                PlayerId = playerId,
                PublicKeyUrl = publicKeyUrl,
                Salt = salt,
                Signature = signature,
                TimestampSeconds = timestampSeconds
            };
            var resp = await _apiClient.AuthenticateGameCenterAsync(ServerKey, string.Empty, request, create, username);
            return Session.Restore(resp.Token, resp.Created);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateGoogleAsync(string token, string username = null, bool create = true)
        {
            var request = new ApiAccountGoogle {Token = token};
            var resp = await _apiClient.AuthenticateGoogleAsync(ServerKey, string.Empty, request, create, username);
            return Session.Restore(resp.Token, resp.Created);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateSteamAsync(string token, string username = null, bool create = true)
        {
            var request = new ApiAccountSteam {Token = token};
            var resp = await _apiClient.AuthenticateSteamAsync(ServerKey, string.Empty, request, create, username);
            return Session.Restore(resp.Token, resp.Created);
        }

        /// <inheritdoc />
        public async Task BlockFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null)
        {
            await _apiClient.BlockFriendsAsync(session.AuthToken, ids, usernames);
        }

        /// <inheritdoc />
        public async Task<IApiGroup> CreateGroupAsync(ISession session, string name, string description = "",
            string avatarUrl = null, string langTag = null, bool open = true)
        {
            var request = new ApiCreateGroupRequest
            {
                Name = name,
                Description = description,
                AvatarUrl = avatarUrl,
                LangTag = langTag,
                Open = open
            };
            return await _apiClient.CreateGroupAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task DeleteFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null)
        {
            await _apiClient.DeleteFriendsAsync(session.AuthToken, ids, usernames);
        }

        /// <inheritdoc />
        public async Task DeleteGroupAsync(ISession session, string groupId)
        {
            await _apiClient.DeleteGroupAsync(session.AuthToken, groupId);
        }

        /// <inheritdoc />
        public async Task DeleteLeaderboardRecordAsync(ISession session, string leaderboardId)
        {
            await _apiClient.DeleteLeaderboardRecordAsync(session.AuthToken, leaderboardId);
        }

        /// <inheritdoc />
        public async Task DeleteNotificationsAsync(ISession session, IEnumerable<string> ids)
        {
            await _apiClient.DeleteNotificationsAsync(session.AuthToken, ids);
        }

        /// <inheritdoc />
        public async Task DeleteStorageObjectsAsync(ISession session, params StorageObjectId[] ids)
        {
            var objects = new List<ApiDeleteStorageObjectId>(ids.Length);
            foreach (var id in ids)
                objects.Add(new ApiDeleteStorageObjectId
                {
                    Collection = id.Collection,
                    Key = id.Key,
                    Version = id.Version
                });

            var request = new ApiDeleteStorageObjectsRequest {_objectIds = objects};
            await _apiClient.DeleteStorageObjectsAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task<IApiAccount> GetAccountAsync(ISession session)
        {
            return await _apiClient.GetAccountAsync(session.AuthToken);
        }

        /// <inheritdoc />
        public async Task<IApiUsers> GetUsersAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null, IEnumerable<string> facebookIds = null)
        {
            return await _apiClient.GetUsersAsync(session.AuthToken, ids, usernames, facebookIds);
        }

        /// <inheritdoc />
        public async Task ImportFacebookFriendsAsync(ISession session, string token, bool reset = false)
        {
            var request = new ApiAccountFacebook {Token = token};
            await _apiClient.ImportFacebookFriendsAsync(session.AuthToken, request, reset);
        }

        /// <inheritdoc />
        public async Task JoinGroupAsync(ISession session, string groupId)
        {
            await _apiClient.JoinGroupAsync(session.AuthToken, groupId);
        }

        /// <inheritdoc />
        public async Task JoinTournamentAsync(ISession session, string tournamentId)
        {
            await _apiClient.JoinTournamentAsync(session.AuthToken, tournamentId);
        }

        /// <inheritdoc />
        public async Task KickGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids)
        {
            await _apiClient.KickGroupUsersAsync(session.AuthToken, groupId, ids);
        }

        /// <inheritdoc />
        public async Task LeaveGroupAsync(ISession session, string groupId)
        {
            await _apiClient.LeaveGroupAsync(session.AuthToken, groupId);
        }

        /// <inheritdoc />
        public async Task LinkCustomAsync(ISession session, string id)
        {
            var request = new ApiAccountCustom {Id = id};
            await _apiClient.LinkCustomAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task LinkDeviceAsync(ISession session, string id)
        {
            var request = new ApiAccountDevice {Id = id};
            await _apiClient.LinkDeviceAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task LinkEmailAsync(ISession session, string email, string password)
        {
            var request = new ApiAccountEmail {Email = email, Password = password};
            await _apiClient.LinkEmailAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task LinkFacebookAsync(ISession session, string token, bool import = true)
        {
            var request = new ApiAccountFacebook {Token = token};
            await _apiClient.LinkFacebookAsync(session.AuthToken, request, import);
        }

        /// <inheritdoc />
        public async Task LinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestampSeconds)
        {
            var request = new ApiAccountGameCenter
            {
                BundleId = bundleId,
                PlayerId = playerId,
                PublicKeyUrl = publicKeyUrl,
                Salt = salt,
                Signature = signature,
                TimestampSeconds = timestampSeconds
            };
            await _apiClient.LinkGameCenterAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task LinkGoogleAsync(ISession session, string token)
        {
            var request = new ApiAccountGoogle {Token = token};
            await _apiClient.LinkGoogleAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task LinkSteamAsync(ISession session, string token)
        {
            var request = new ApiAccountSteam {Token = token};
            await _apiClient.LinkSteamAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, string channelId,
            int limit = 1, bool forward = true, string cursor = null)
        {
            return await _apiClient.ListChannelMessagesAsync(session.AuthToken, channelId, limit, forward, cursor);
        }

        /// <inheritdoc />
        public async Task<IApiFriends> ListFriendsAsync(ISession session)
        {
            return await _apiClient.ListFriendsAsync(session.AuthToken);
        }

        /// <inheritdoc />
        public async Task<IApiGroupUserList> ListGroupUsersAsync(ISession session, string groupId)
        {
            return await _apiClient.ListGroupUsersAsync(session.AuthToken, groupId);
        }

        /// <inheritdoc />
        public async Task<IApiGroupList> ListGroupsAsync(ISession session, string name = null, int limit = 1,
            string cursor = null)
        {
            return await _apiClient.ListGroupsAsync(session.AuthToken, name, cursor, limit);
        }

        /// <inheritdoc />
        public async Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAsync(ISession session, string leaderboardId,
            IEnumerable<string> ownerIds = null, int limit = 1, string cursor = null)
        {
            return await _apiClient.ListLeaderboardRecordsAsync(session.AuthToken, leaderboardId, ownerIds, limit,
                cursor);
        }

        /// <inheritdoc />
        public async Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAroundOwnerAsync(ISession session,
            string leaderboardId, string ownerId, int limit = 1)
        {
            return await _apiClient.ListLeaderboardRecordsAroundOwnerAsync(session.AuthToken, leaderboardId, ownerId,
                limit);
        }

        /// <inheritdoc />
        public async Task<IApiMatchList> ListMatchesAsync(ISession session, int min, int max, int limit,
            bool authoritative, string label, string query = null)
        {
            // Arguments are re-ordered for more natural usage than is code generated.
            return await _apiClient.ListMatchesAsync(session.AuthToken, limit, authoritative, label, min, max, query);
        }

        /// <inheritdoc />
        public async Task<IApiNotificationList> ListNotificationsAsync(ISession session, int limit = 1,
            string cacheableCursor = null)
        {
            return await _apiClient.ListNotificationsAsync(session.AuthToken, limit, cacheableCursor);
        }

        /// <inheritdoc />
        public async Task<IApiStorageObjectList> ListStorageObjects(ISession session, string collection, int limit,
            string cursor)
        {
            return await _apiClient.ListStorageObjectsAsync(session.AuthToken, collection, "", limit, cursor);
        }

        /// <inheritdoc />
        public async Task<IApiTournamentRecordList> ListTournamentRecordsAroundOwnerAsync(ISession session,
            string tournamentId, string ownerId, int limit = 1)
        {
            return await _apiClient.ListTournamentRecordsAroundOwnerAsync(session.AuthToken, tournamentId, ownerId,
                limit);
        }

        /// <inheritdoc />
        public async Task<IApiTournamentRecordList> ListTournamentRecordsAsync(ISession session, string tournamentId,
            IEnumerable<string> ownerIds = null, int limit = 1, string cursor = null)
        {
            return await _apiClient.ListTournamentRecordsAsync(session.AuthToken, tournamentId, ownerIds, limit,
                cursor);
        }

        /// <inheritdoc />
        public async Task<IApiTournamentList> ListTournamentsAsync(ISession session, int categoryStart, int categoryEnd,
            int startTime, int endTime, int limit = 1, string cursor = null)
        {
            return await _apiClient.ListTournamentsAsync(session.AuthToken, categoryStart, categoryEnd, startTime,
                endTime, limit, cursor);
        }

        /// <inheritdoc />
        public async Task<IApiUserGroupList> ListUserGroupsAsync(ISession session)
        {
            return await ListUserGroupsAsync(session, session.UserId);
        }

        /// <inheritdoc />
        public async Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, string userId)
        {
            return await _apiClient.ListUserGroupsAsync(session.AuthToken, userId);
        }

        /// <inheritdoc />
        public async Task<IApiStorageObjectList> ListUsersStorageObjectsAsync(ISession session, string collection,
            string userId, int limit, string cursor)
        {
            return await _apiClient.ListStorageObjects2Async(session.AuthToken, collection, userId, limit, cursor);
        }

        /// <inheritdoc />
        public async Task PromoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids)
        {
            await _apiClient.PromoteGroupUsersAsync(session.AuthToken, groupId, ids);
        }

        /// <inheritdoc />
        public async Task<IApiStorageObjects> ReadStorageObjectsAsync(ISession session,
            params IApiReadStorageObjectId[] ids)
        {
            var wrapper = new List<ApiReadStorageObjectId>(ids.Length);
            foreach (var id in ids)
                wrapper.Add(new ApiReadStorageObjectId
                {
                    Collection = id.Collection,
                    Key = id.Key,
                    UserId = id.UserId
                });

            var request = new ApiReadStorageObjectsRequest {_objectIds = wrapper};
            return await _apiClient.ReadStorageObjectsAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task<IApiRpc> RpcAsync(ISession session, string id, string payload)
        {
            return await _apiClient.RpcFuncAsync(session.AuthToken, id, payload);
        }

        /// <inheritdoc />
        public async Task<IApiRpc> RpcAsync(ISession session, string id)
        {
            return await _apiClient.RpcFunc2Async(session.AuthToken, id, null, null);
        }

        /// <inheritdoc />
        public async Task<IApiRpc> RpcAsync(string httpKey, string id, string payload = null)
        {
            return await _apiClient.RpcFunc2Async(null, id, payload, httpKey);
        }

        /// <inheritdoc />
        public async Task UnlinkCustomAsync(ISession session, string id)
        {
            var request = new ApiAccountCustom {Id = id};
            await _apiClient.UnlinkCustomAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task UnlinkDeviceAsync(ISession session, string id)
        {
            var request = new ApiAccountDevice {Id = id};
            await _apiClient.UnlinkDeviceAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task UnlinkEmailAsync(ISession session, string email, string password)
        {
            var request = new ApiAccountEmail {Email = email, Password = password};
            await _apiClient.UnlinkEmailAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task UnlinkFacebookAsync(ISession session, string token)
        {
            var request = new ApiAccountFacebook {Token = token};
            await _apiClient.UnlinkFacebookAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task UnlinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestampSeconds)
        {
            var request = new ApiAccountGameCenter
            {
                BundleId = bundleId,
                PlayerId = playerId,
                PublicKeyUrl = publicKeyUrl,
                Salt = salt,
                Signature = signature,
                TimestampSeconds = timestampSeconds
            };
            await _apiClient.UnlinkGameCenterAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task UnlinkGoogleAsync(ISession session, string token)
        {
            var request = new ApiAccountGoogle {Token = token};
            await _apiClient.UnlinkGoogleAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task UnlinkSteamAsync(ISession session, string token)
        {
            var request = new ApiAccountSteam {Token = token};
            await _apiClient.UnlinkSteamAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task UpdateAccountAsync(ISession session, string username, string displayName = null,
            string avatarUrl = null, string langTag = null, string location = null, string timezone = null)
        {
            var request = new ApiUpdateAccountRequest
            {
                AvatarUrl = avatarUrl,
                DisplayName = displayName,
                LangTag = langTag,
                Location = location,
                Timezone = timezone,
                Username = username
            };
            await _apiClient.UpdateAccountAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task UpdateGroupAsync(ISession session, string groupId, string name, string description = null,
            string avatarUrl = null, string langTag = null, bool open = false)
        {
            var request = new ApiUpdateGroupRequest
            {
                AvatarUrl = avatarUrl,
                Description = description,
                LangTag = langTag,
                Name = name,
                Open = open
            };
            await _apiClient.UpdateGroupAsync(session.AuthToken, groupId, request);
        }

        /// <inheritdoc />
        public async Task<IApiLeaderboardRecord> WriteLeaderboardRecordAsync(ISession session, string leaderboardId,
            long score, long subscore = 0L, string metadata = null)
        {
            var request = new WriteLeaderboardRecordRequestLeaderboardRecordWrite
            {
                Metadata = metadata,
                Score = score.ToString(),
                Subscore = subscore.ToString()
            };
            return await _apiClient.WriteLeaderboardRecordAsync(session.AuthToken, leaderboardId, request);
        }

        /// <inheritdoc />
        public async Task<IApiStorageObjectAcks> WriteStorageObjectsAsync(ISession session,
            params IApiWriteStorageObject[] objects)
        {
            var wrapper = new List<ApiWriteStorageObject>(objects.Length);
            foreach (var o in objects)
                wrapper.Add(new ApiWriteStorageObject
                {
                    Collection = o.Collection,
                    Key = o.Key,
                    PermissionRead = o.PermissionRead,
                    PermissionWrite = o.PermissionWrite,
                    Value = o.Value,
                    Version = o.Version
                });

            var request = new ApiWriteStorageObjectsRequest {_objects = wrapper};
            return await _apiClient.WriteStorageObjectsAsync(session.AuthToken, request);
        }

        /// <inheritdoc />
        public async Task<IApiLeaderboardRecord> WriteTournamentRecordAsync(ISession session, string tournamentId,
            long score, long subscore = 0L, string metadata = null)
        {
            var request = new WriteTournamentRecordRequestTournamentRecordWrite
            {
                Metadata = metadata,
                Score = score.ToString(),
                Subscore = subscore.ToString()
            };
            return await _apiClient.WriteTournamentRecordAsync(session.AuthToken, tournamentId, request);
        }

        /// <inheritdoc />
        public ISocket CreateWebSocket(int reconnect = 3)
        {
            var scheme = _options.EnableSsl ? "wss" : "ws";
            var baseUri = new UriBuilder(scheme, Host, Port);
            // FIXME implement reconnect events?
            return new WebSocketWrapper(baseUri.Uri, Logger, Timeout);
        }
    }
}
