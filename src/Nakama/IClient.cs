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

namespace Nakama
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// A client to interact with Nakama server.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// The host address of the server. Defaults to "127.0.0.1".
        /// </summary>
        string Host { get; }

        /// <summary>
        /// A logger which can write log messages. Defaults to <c>NullLogger</c>.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// The port number of the server. Defaults to 7350.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// The number of retries to attempt with each request with the server.
        /// </summary>
        int Retries { get; set; }

        /// <summary>
        /// The key used to authenticate with the server without a session. Defaults to "defaultkey".
        /// </summary>
        string ServerKey { get; }

        /// <summary>
        /// Set connection strings to use the secure mode with the server. Defaults to false.
        /// <remarks>
        /// The server must be configured to make use of this option. With HTTP, GRPC, and WebSockets the server must
        /// be configured with an SSL certificate or use a load balancer which performs SSL termination. For rUDP you
        /// must configure the server to expose it's IP address so it can be bundled within session tokens. See the
        /// server documentation for more information.
        /// </remarks>
        /// </summary>
        bool Secure { get; }

        /// <summary>
        /// Trace all actions performed by the client. Defaults to false.
        /// </summary>
        bool Trace { get; set; }

        /// <summary>
        /// Set the timeout on requests sent to the server.
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// Add one or more friends by id or username.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The ids of the users to add or invite as friends.</param>
        /// <param name="usernames">The usernames of the users to add as friends.</param>
        /// <returns>A task.</returns>
        Task AddFriendsAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames = null);

        /// <summary>
        /// Add one or more users to the group.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The id of the group to add users into.</param>
        /// <param name="ids">The ids of the users to add or invite to the group.</param>
        /// <returns>A task.</returns>
        Task AddGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids);

        /// <summary>
        /// Authenticate a user with a custom id.
        /// </summary>
        /// <param name="id">A custom identifier usually obtained from an external authentication service.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">True if the user should be created when authenticated.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateCustomAsync(string id, string username = null, bool create = true);

        /// <summary>
        /// Authenticate a user with a device id.
        /// </summary>
        /// <param name="id">A device identifier usually obtained from a platform API.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">True if the user should be created when authenticated.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateDeviceAsync(string id, string username = null, bool create = true);

        /// <summary>
        /// Authenticate a user with an email and password.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <param name="password">The password for the user.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">True if the user should be created when authenticated.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateEmailAsync(string email, string password, string username = null,
            bool create = true);

        /// <summary>
        /// Authenticate a user with a Facebook auth token.
        /// </summary>
        /// <param name="token">An OAuth access token from the Facebook SDK.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">True if the user should be created when authenticated.</param>
        /// <param name="import">True if the Facebook friends should be imported.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateFacebookAsync(string token, string username = null, bool create = true,
            bool import = true);

        /// <summary>
        /// Authenticate a user with Apple Game Center.
        /// </summary>
        /// <param name="bundleId">The bundle id of the Game Center application.</param>
        /// <param name="playerId">The player id of the user in Game Center.</param>
        /// <param name="publicKeyUrl">The URL for the public encryption key.</param>
        /// <param name="salt">A random <c>NSString</c> used to compute the hash and keep it randomized.</param>
        /// <param name="signature">The verification signature data generated.</param>
        /// <param name="timestampSeconds">The date and time that the signature was created.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">True if the user should be created when authenticated.</param>
        /// <returns></returns>
        Task<ISession> AuthenticateGameCenterAsync(string bundleId, string playerId, string publicKeyUrl, string salt,
            string signature, string timestampSeconds, string username = null, bool create = true);

        /// <summary>
        /// Authenticate a user with a Google auth token.
        /// </summary>
        /// <param name="token">An OAuth access token from the Google SDK.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">True if the user should be created when authenticated.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateGoogleAsync(string token, string username = null, bool create = true);

        /// <summary>
        /// Authenticate a user with a Steam auth token.
        /// </summary>
        /// <param name="token">An authentication token from the Steam network.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">True if the user should be created when authenticated.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateSteamAsync(string token, string username = null, bool create = true);

        /// <summary>
        /// Block one or more friends by id or username.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The ids of the users to block.</param>
        /// <param name="usernames">The usernames of the users to block.</param>
        /// <returns>A task.</returns>
        Task BlockFriendsAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames = null);

        /// <summary>
        /// Create a group.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="name">The name for the group.</param>
        /// <param name="description">A description for the group.</param>
        /// <param name="avatarUrl">An avatar url for the group.</param>
        /// <param name="langTag">A language tag in BCP-47 format for the group.</param>
        /// <param name="open">True if the group should have open membership.</param>
        /// <returns>A task to resolve a new group object.</returns>
        Task<IApiGroup> CreateGroupAsync(ISession session, string name, string description = "",
            string avatarUrl = null, string langTag = null, bool open = true);

        /// <summary>
        /// Delete one more or users by id or username from friends.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The user ids to remove as friends.</param>
        /// <param name="usernames">The usernames to remove as friends.</param>
        /// <returns>A task.</returns>
        Task DeleteFriendsAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames = null);

        /// <summary>
        /// Delete a group by id.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The group id to to remove.</param>
        /// <returns>A task.</returns>
        Task DeleteGroupAsync(ISession session, string groupId);

        /// <summary>
        /// Delete a leaderboard record.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="leaderboardId">The id of the leaderboard with the record to be deleted.</param>
        /// <returns>A task.</returns>
        Task DeleteLeaderboardRecordAsync(ISession session, string leaderboardId);

        /// <summary>
        /// Delete one or more notifications by id.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The notification ids to remove.</param>
        /// <returns>A task.</returns>
        Task DeleteNotificationsAsync(ISession session, IEnumerable<string> ids);

        /// <summary>
        /// Delete one or more storage objects.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The ids of the objects to delete.</param>
        /// <returns>A task.</returns>
        Task DeleteStorageObjectsAsync(ISession session, params StorageObjectId[] ids);

        /// <summary>
        /// Fetch the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <returns>A task to resolve an account object.</returns>
        Task<IApiAccount> GetAccountAsync(ISession session);

        /// <summary>
        /// Fetch one or more users by id, usernames, and Facebook ids.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids"></param>
        /// <param name="usernames"></param>
        /// <param name="facebookIds"></param>
        /// <returns>A task to resolve user objects.</returns>
        Task<IApiUsers> GetUsersAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames = null,
            IEnumerable<string> facebookIds = null);

        /// <summary>
        /// Import Facebook friends and add them to the user's account.
        /// </summary>
        /// <remarks>
        /// The server will import friends when the user authenticates with Facebook. This function can be used to be
        /// explicit with the import operation.
        /// </remarks>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An OAuth access token from the Facebook SDK.</param>
        /// <param name="reset">True if the Facebook friend import for the user should be reset.</param>
        /// <returns>A task.</returns>
        Task ImportFacebookFriendsAsync(ISession session, string token, bool reset = false);

        /// <summary>
        /// Join a group if it has open membership or request to join it.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The id of the group to join.</param>
        /// <returns>A task.</returns>
        Task JoinGroupAsync(ISession session, string groupId);

        /// <summary>
        /// Kick one or more users from the group.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The id of the group.</param>
        /// <param name="ids">The ids of the users to kick.</param>
        /// <returns>A task.</returns>
        Task KickGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids);

        /// <summary>
        /// Leave a group by id.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The id of the group to leave.</param>
        /// <returns>A task.</returns>
        Task LeaveGroupAsync(ISession session, string groupId);

        /// <summary>
        /// Link a custom id to the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">A custom identifier usually obtained from an external authentication service.</param>
        /// <returns>A task.</returns>
        Task LinkCustomAsync(ISession session, string id);

        /// <summary>
        /// Link a device id to the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">A device identifier usually obtained from a platform API.</param>
        /// <returns>A task.</returns>
        Task LinkDeviceAsync(ISession session, string id);

        /// <summary>
        /// Link an email with password to the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="email">The email address of the user.</param>
        /// <param name="password">The password for the user.</param>
        /// <returns>A task.</returns>
        Task LinkEmailAsync(ISession session, string email, string password);

        /// <summary>
        /// Link a Facebook profile to a user account.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An OAuth access token from the Facebook SDK.</param>
        /// <param name="import">True if the Facebook friends should be imported.</param>
        /// <returns>A task.</returns>
        Task LinkFacebookAsync(ISession session, string token, bool import = true);

        /// <summary>
        /// Link a Game Center profile to a user account.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="bundleId">The bundle id of the Game Center application.</param>
        /// <param name="playerId">The player id of the user in Game Center.</param>
        /// <param name="publicKeyUrl">The URL for the public encryption key.</param>
        /// <param name="salt">A random <c>NSString</c> used to compute the hash and keep it randomized.</param>
        /// <param name="signature">The verification signature data generated.</param>
        /// <param name="timestampSeconds">The date and time that the signature was created.</param>
        /// <returns>A task.</returns>
        Task LinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl, string salt,
            string signature, string timestampSeconds);

        /// <summary>
        /// Link a Google profile to a user account.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An OAuth access token from the Google SDK.</param>
        /// <returns>A task.</returns>
        Task LinkGoogleAsync(ISession session, string token);

        /// <summary>
        /// Link a Steam profile to a user account.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An authentication token from the Steam network.</param>
        /// <returns>A task.</returns>
        Task LinkSteamAsync(ISession session, string token);

        /// <summary>
        /// List messages from a chat channel.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="channelId">A channel identifier.</param>
        /// <param name="limit">The number of chat messages to list.</param>
        /// <param name="forward">Fetch messages forward from the current cursor (or the start).</param>
        /// <param name="cursor">A cursor for the current position in the messages history to list.</param>
        /// <returns>A task to resolve channel message objects.</returns>
        Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, string channelId, int limit = 1,
            bool forward = true, string cursor = null);

        /// <summary>
        /// List of friends of the current user.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <returns>A task to resolve friend objects.</returns>
        Task<IApiFriends> ListFriendsAsync(ISession session);

        /// <summary>
        /// List all users part of the group.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The id of the group.</param>
        /// <returns>A task to resolve group user objects.</returns>
        Task<IApiGroupUserList> ListGroupUsersAsync(ISession session, string groupId);

        /// <summary>
        /// List groups on the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="name">The name filter to apply to the group list.</param>
        /// <param name="limit">The number of groups to list.</param>
        /// <param name="cursor">A cursor for the current position in the groups to list.</param>
        /// <returns>A task to resolve group objects.</returns>
        Task<IApiGroupList> ListGroupsAsync(ISession session, string name = null, int limit = 1, string cursor = null);

        /// <summary>
        /// List records from a leaderboard.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="leaderboardId">The id of the leaderboard to list.</param>
        /// <param name="ownerIds">Record owners to fetch with the list of records.</param>
        /// <param name="limit">The number of records to list.</param>
        /// <param name="cursor">A cursor for the current position in the leaderboard records to list.</param>
        /// <returns>A task to resolve leaderboard record objects.</returns>
        Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAsync(ISession session, string leaderboardId,
            IEnumerable<string> ownerIds = null, int limit = 1, string cursor = null);

        /// <summary>
        /// Fetch a list of matches active on the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="min">The minimum number of match participants.</param>
        /// <param name="max">The maximum number of match participants.</param>
        /// <param name="limit">The number of matches to list.</param>
        /// <param name="authoritative"><c>True</c> to include authoritative matches.</param>
        /// <param name="label">The label to filter the match list on.</param>
        /// <returns></returns>
        Task<IApiMatchList> ListMatchesAsync(ISession session, int min, int max, int limit, bool authoritative,
            string label);

        /// <summary>
        /// List notifications for the user with an optional cursor.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="limit">The number of notifications to list.</param>
        /// <param name="cacheableCursor">A cursor for the current position in notifications to list.</param>
        /// <returns>A task to resolve notifications objects.</returns>
        Task<IApiNotificationList> ListNotificationsAsync(ISession session, int limit = 1,
            string cacheableCursor = null);

        /// <summary>
        /// List storage objects in a collection which have public read access.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="collection">The collection to list over.</param>
        /// <param name="limit">The number of objects to list.</param>
        /// <param name="cursor">A cursor to paginate over the collection.</param>
        /// <returns>A task which resolves to a storage object list.</returns>
        Task<IApiStorageObjectList> ListStorageObjects(ISession session, string collection, int limit = 1,
            string cursor = null);

        /// <summary>
        /// List of groups the current user is a member of.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <returns>A task which resolves to group objects.</returns>
        Task<IApiUserGroupList> ListUserGroupsAsync(ISession session);

        /// <summary>
        /// List groups a user is a member of.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="userId">The id of the user whose groups to list.</param>
        /// <returns>A task which resolves to group objects.</returns>
        Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, string userId);

        /// <summary>
        /// List storage objects in a collection which belong to a specific user and have public read access.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="collection">The collection to list over.</param>
        /// <param name="userId">The user ID of the user to list objects for.</param>
        /// <param name="limit">The number of objects to list.</param>
        /// <param name="cursor">A cursor to paginate over the collection.</param>
        /// <returns>A task which resolves to a storage object list.</returns>
        Task<IApiStorageObjectList> ListUsersStorageObjectsAsync(ISession session, string collection, string userId,
            int limit, string cursor);

        /// <summary>
        /// Promote one or more users in the group.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The id of the group to promote users into.</param>
        /// <param name="ids">The ids of the users to promote.</param>
        /// <returns>A task.</returns>
        Task PromoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids);

        /// <summary>
        /// Read one or more objects from the storage engine.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The objects to read.</param>
        /// <returns>A task to resolve storage objects.</returns>
        Task<IApiStorageObjects> ReadStorageObjectsAsync(ISession session, params IApiReadStorageObjectId[] ids);

        /// <summary>
        /// Execute a Lua function with an input payload on the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">The id of the function to execute on the server.</param>
        /// <param name="payload">The payload to send with the function call.</param>
        /// <returns>A task to resolve an RPC response.</returns>
        Task<IApiRpc> RpcAsync(ISession session, string id, string payload);

        /// <summary>
        /// Execute a Lua function on the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">The id of the function to execute on the server.</param>
        /// <returns>A task to resolve an RPC response.</returns>
        Task<IApiRpc> RpcAsync(ISession session, string id);

        /// <summary>
        /// Execute a Lua function on the server without a session.
        /// </summary>
        /// <param name="httpkey">The secure HTTP key used to authenticate.</param>
        /// <param name="id">The id of the function to execute on the server.</param>
        /// <param name="payload">A payload to send with the function call.</param>
        /// <returns>A task to resolve an RPC response.</returns>
        Task<IApiRpc> RpcAsync(string httpkey, string id, string payload = null);

        /// <summary>
        /// Unlink a custom id from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">A custom identifier usually obtained from an external authentication service.</param>
        /// <returns>A task.</returns>
        Task UnlinkCustomAsync(ISession session, string id);

        /// <summary>
        /// Unlink a device id from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">A device identifier usually obtained from a platform API.</param>
        /// <returns>A task.</returns>
        Task UnlinkDeviceAsync(ISession session, string id);

        /// <summary>
        /// Unlink an email with password from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="email">The email address of the user.</param>
        /// <param name="password">The password for the user.</param>
        /// <returns>A task.</returns>
        Task UnlinkEmailAsync(ISession session, string email, string password);

        /// <summary>
        /// Unlink a Facebook profile from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An OAuth access token from the Facebook SDK.</param>
        /// <returns>A task.</returns>
        Task UnlinkFacebookAsync(ISession session, string token);

        /// <summary>
        /// Unlink a Game Center profile from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="bundleId">The bundle id of the Game Center application.</param>
        /// <param name="playerId">The player id of the user in Game Center.</param>
        /// <param name="publicKeyUrl">The URL for the public encryption key.</param>
        /// <param name="salt">A random <c>NSString</c> used to compute the hash and keep it randomized.</param>
        /// <param name="signature">The verification signature data generated.</param>
        /// <param name="timestampSeconds">The date and time that the signature was created.</param>
        /// <returns>A task.</returns>
        Task UnlinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl, string salt,
            string signature, string timestampSeconds);

        /// <summary>
        /// Unlink a Google profile from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An OAuth access token from the Google SDK.</param>
        /// <returns>A task.</returns>
        Task UnlinkGoogleAsync(ISession session, string token);

        /// <summary>
        /// Unlink a Steam profile from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An authentication token from the Steam network.</param>
        /// <returns>A task.</returns>
        Task UnlinkSteamAsync(ISession session, string token);

        /// <summary>
        /// Update the current user's account on the server.
        /// </summary>
        /// <param name="session">The session for the user.</param>
        /// <param name="username">The new username for the user.</param>
        /// <param name="displayName">A new display name for the user.</param>
        /// <param name="avatarUrl">A new avatar url for the user.</param>
        /// <param name="langTag">A new language tag in BCP-47 format for the user.</param>
        /// <param name="location">A new location for the user.</param>
        /// <param name="timezone">New timezone information for the user.</param>
        /// <returns>A task to complete the account update.</returns>
        Task UpdateAccountAsync(ISession session, string username, string displayName = null,
            string avatarUrl = null, string langTag = null, string location = null, string timezone = null);

        /// <summary>
        /// Update a group.
        /// </summary>
        /// <remarks>
        /// The user must have the correct access permissions for the group.
        /// </remarks>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The id of the group to update.</param>
        /// <param name="name">A new name for the group.</param>
        /// <param name="description">A new description for the group.</param>
        /// <param name="avatarUrl">A new avatar url for the group.</param>
        /// <param name="langTag">A new language tag in BCP-47 format for the group.</param>
        /// <param name="open">True if the group should have open membership.</param>
        /// <returns>A task.</returns>
        Task UpdateGroupAsync(ISession session, string groupId, string name, string description = null,
            string avatarUrl = null, string langTag = null, bool open = false);

        /// <summary>
        /// Write a record to a leaderboard.
        /// </summary>
        /// <param name="session">The session for the user.</param>
        /// <param name="leaderboardId">The id of the leaderboard to write.</param>
        /// <param name="score">The score for the leaderboard record.</param>
        /// <param name="subscore">The subscore for the leaderboard record.</param>
        /// <param name="metadata">The metadata for the leaderboard record.</param>
        /// <returns>A task to complete the leaderboard record write.</returns>
        Task<IApiLeaderboardRecord> WriteLeaderboardRecordAsync(ISession session, string leaderboardId, long score,
            long subscore = 0L, string metadata = null);

        /// <summary>
        /// Write objects to the storage engine.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="objects">The objects to write.</param>
        /// <returns>A task to resolve the acknowledgements with writes.</returns>
        Task<IApiStorageObjectAcks> WriteStorageObjectsAsync(ISession session, params IApiWriteStorageObject[] objects);

        /// <summary>
        /// Create a new WebSocket from the client.
        /// </summary>
        /// <param name="reconnect">Set the number of retries to attempt after a disconnect.</param>
        /// <returns>A socket object.</returns>
        ISocket CreateWebSocket(int reconnect = 3);
    }
}
