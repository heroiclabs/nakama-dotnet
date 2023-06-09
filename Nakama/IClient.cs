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
    /// <summary>
    /// A client for the API in Nakama server.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// True if the session should be refreshed with an active refresh token.
        /// </summary>
        bool AutoRefreshSession { get; }

        /// <summary>
        /// The global retry configuration. See <see cref="RetryConfiguration"/>.
        /// </summary>
        RetryConfiguration GlobalRetryConfiguration { get; set; }

        /// <summary>
        /// The host address of the server. Defaults to "127.0.0.1".
        /// </summary>
        string Host { get; }

        /// <summary>
        /// The port number of the server. Defaults to 7350.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// The protocol scheme used to connect with the server. Must be either "http" or "https".
        /// </summary>
        string Scheme { get; }

        /// <summary>
        /// The key used to authenticate with the server without a session. Defaults to "defaultkey".
        /// </summary>
        string ServerKey { get; }

        /// <summary>
        /// Set the timeout in seconds on requests sent to the server.
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// The logger to use with the client.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// Add one or more friends by id or username.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The ids of the users to add or invite as friends.</param>
        /// <param name="usernames">The usernames of the users to add as friends.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task AddFriendsAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Add one or more users to the group.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The id of the group to add users into.</param>
        /// <param name="ids">The ids of the users to add or invite to the group.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task AddGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Authenticate a user with an Apple ID against the server.
        /// </summary>
        /// <param name="username">A username used to create the user.</param>
        /// <param name="token">The ID token received from Apple to validate.</param>
        /// <param name="vars">Extra information that will be bundled in the session token.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a session object.</returns>
        Task<ISession> AuthenticateAppleAsync(string token, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Authenticate a user with a custom id.
        /// </summary>
        /// <param name="id">A custom identifier usually obtained from an external authentication service.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">If the user should be created when authenticated.</param>
        /// <param name="vars">Extra information that will be bundled in the session token.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a session object.</returns>
        Task<ISession> AuthenticateCustomAsync(string id, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Authenticate a user with a device id.
        /// </summary>
        /// <param name="id">A device identifier usually obtained from a platform API.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">If the user should be created when authenticated.</param>
        /// <param name="vars">Extra information that will be bundled in the session token.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a session object.</returns>
        Task<ISession> AuthenticateDeviceAsync(string id, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Authenticate a user with an email and password.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <param name="password">The password for the user.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">If the user should be created when authenticated.</param>
        /// <param name="vars">Extra information that will be bundled in the session token.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a session object.</returns>
        Task<ISession> AuthenticateEmailAsync(string email, string password, string username = null,
            bool create = true, Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Authenticate a user with a Facebook auth token.
        /// </summary>
        /// <param name="token">An OAuth access token from the Facebook SDK.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">If the user should be created when authenticated.</param>
        /// <param name="import">If the Facebook friends should be imported.</param>
        /// <param name="vars">Extra information that will be bundled in the session token.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a session object.</returns>
        Task<ISession> AuthenticateFacebookAsync(string token, string username = null, bool create = true,
            bool import = true, Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Authenticate a user with Apple Game Center.
        /// </summary>
        /// <param name="bundleId">The bundle id of the Game Center application.</param>
        /// <param name="playerId">The player id of the user in Game Center.</param>
        /// <param name="publicKeyUrl">The URL for the public encryption key.</param>
        /// <param name="salt">A random <c>NSString</c> used to compute the hash and keep it randomized.</param>
        /// <param name="signature">The verification signature data generated.</param>
        /// <param name="timestamp">The date and time that the signature was created.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">If the user should be created when authenticated.</param>
        /// <param name="vars">Extra information that will be bundled in the session token.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a session object.</returns>
        Task<ISession> AuthenticateGameCenterAsync(string bundleId, string playerId, string publicKeyUrl, string salt,
            string signature, string timestamp, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Authenticate a user with a Google auth token.
        /// </summary>
        /// <param name="token">An OAuth access token from the Google SDK.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">If the user should be created when authenticated.</param>
        /// <param name="vars">Extra information that will be bundled in the session token.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a session object.</returns>
        Task<ISession> AuthenticateGoogleAsync(string token, string username = null, bool create = true,
            Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Authenticate a user with a Steam auth token.
        /// </summary>
        /// <param name="token">An authentication token from the Steam network.</param>
        /// <param name="username">A username used to create the user. May be <c>null</c>.</param>
        /// <param name="create">If the user should be created when authenticated.</param>
        /// <param name="vars">Extra information that will be bundled in the session token.</param>
        /// <param name="import">If the Steam friends should be imported.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a session object.</returns>
        Task<ISession> AuthenticateSteamAsync(string token, string username = null, bool create = true,
            bool import = true, Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Ban a set of users from a group.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The group to ban the users from.</param>
        /// <param name="ids">The ids of the users to ban.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task BanGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Block one or more friends by id or username.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The ids of the users to block.</param>
        /// <param name="usernames">The usernames of the users to block.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task BlockFriendsAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Create a group.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="name">The name for the group.</param>
        /// <param name="description">A description for the group.</param>
        /// <param name="avatarUrl">An avatar url for the group.</param>
        /// <param name="langTag">A language tag in BCP-47 format for the group.</param>
        /// <param name="open">If the group should have open membership.</param>
        /// <param name="maxCount">The maximum number of members allowed.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a new group object.</returns>
        Task<IApiGroup> CreateGroupAsync(ISession session, string name, string description = "",
            string avatarUrl = null, string langTag = null, bool open = true, int maxCount = 100, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Delete the current user's account. Note that this will invalidate your session, requiring you to reauthenticate.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task DeleteAccountAsync(ISession session, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Delete one more or users by id or username from friends.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The user ids to remove as friends.</param>
        /// <param name="usernames">The usernames to remove as friends.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task DeleteFriendsAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Delete a group by id.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The group id to to remove.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task DeleteGroupAsync(ISession session, string groupId, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Delete a leaderboard record.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="leaderboardId">The id of the leaderboard with the record to be deleted.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task DeleteLeaderboardRecordAsync(ISession session, string leaderboardId, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Delete one or more notifications by id.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The notification ids to remove.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task DeleteNotificationsAsync(ISession session, IEnumerable<string> ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Delete one or more storage objects.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The ids of the objects to delete.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task DeleteStorageObjectsAsync(ISession session, StorageObjectId[] ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Demote a set of users in a group to the next role down.
        /// <param name="groupId">The group to demote users in.</param>
        /// <param name="userIds">The users to demote.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <remarks>Members who are already at the lowest rank will be skipped.</remarks>
        /// <returns>A task which represents the asynchronous operation.</returns>
        /// </summary>
        Task DemoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> userIds, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Submit an event for processing in the server's registered runtime custom events handler.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="name">The name of the event.</param>
        /// <param name="properties">The properties of the event.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task EventAsync(ISession session, string name, Dictionary<string, string> properties, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Fetch the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the account object.</returns>
        Task<IApiAccount> GetAccountAsync(ISession session, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Get the subscription represented by the provided product id.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="productId">The product id.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the subscription.</returns>
        Task<IApiValidatedSubscription> GetSubscriptionAsync(ISession session, string productId, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Fetch one or more users by id, usernames, and Facebook ids.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The IDs of the users to retrieve.</param>
        /// <param name="usernames">The usernames of the users to retrieve.</param>
        /// <param name="facebookIds">The facebook IDs of the users to retrieve.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a collection of user objects.</returns>
        Task<IApiUsers> GetUsersAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames = null,
            IEnumerable<string> facebookIds = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Import Facebook friends and add them to the user's account.
        /// </summary>
        /// <remarks>
        /// The server will import friends when the user authenticates with Facebook. This function can be used to be
        /// explicit with the import operation.
        /// </remarks>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An OAuth access token from the Facebook SDK.</param>
        /// <param name="reset">If the Facebook friend import for the user should be reset.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task ImportFacebookFriendsAsync(ISession session, string token, bool? reset = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Import Steam friends and add them to the user's account.
        /// </summary>
        /// <remarks>
        /// The server will import friends when the user authenticates with Steam. This function can be used to be
        /// explicit with the import operation.
        /// </remarks>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An access token from Steam.</param>
        /// <param name="reset">If the Steam friend import for the user should be reset.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task ImportSteamFriendsAsync(ISession session, string token, bool? reset = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Join a group if it has open membership or request to join it.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The ID of the group to join.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task JoinGroupAsync(ISession session, string groupId, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Join a tournament by ID.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="tournamentId">The ID of the tournament to join.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task JoinTournamentAsync(ISession session, string tournamentId, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Kick one or more users from the group.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="ids">The IDs of the users to kick.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task KickGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Leave a group by ID.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The ID of the group to leave.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LeaveGroupAsync(ISession session, string groupId, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Link an Apple ID to the social profiles on the current user's account.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">The ID token received from Apple to validate.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LinkAppleAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Link a custom ID to the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">A custom identifier usually obtained from an external authentication service.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LinkCustomAsync(ISession session, string id, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Link a device ID to the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">A device identifier usually obtained from a platform API.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LinkDeviceAsync(ISession session, string id, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Link an email with password to the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="email">The email address of the user.</param>
        /// <param name="password">The password for the user.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LinkEmailAsync(ISession session, string email, string password, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Link a Facebook profile to a user account.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An OAuth access token from the Facebook SDK.</param>
        /// <param name="import">If the Facebook friends should be imported.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LinkFacebookAsync(ISession session, string token, bool? import = true, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Link a Game Center profile to a user account.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="bundleId">The bundle ID of the Game Center application.</param>
        /// <param name="playerId">The player ID of the user in Game Center.</param>
        /// <param name="publicKeyUrl">The URL for the public encryption key.</param>
        /// <param name="salt">A random <c>NSString</c> used to compute the hash and keep it randomized.</param>
        /// <param name="signature">The verification signature data generated.</param>
        /// <param name="timestamp">The date and time that the signature was created.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl, string salt,
            string signature, string timestamp, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Link a Google profile to a user account.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <param name="token">An OAuth access token from the Google SDK.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LinkGoogleAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Link a Steam profile to a user account.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An authentication token from the Steam network.</param>
        /// <param name="import">If the Steam friends should be imported.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LinkSteamAsync(ISession session, string token, bool import, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List messages from a chat channel.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="channel">The chat channel object.</param>
        /// <param name="limit">The number of chat messages to list.</param>
        /// <param name="forward">Fetch messages forward from the current cursor (or the start, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default).</param>
        /// <param name="cursor">A cursor for the current position in the messages history to list.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the channel message list object.</returns>
        Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, IChannel channel, int limit = 1,
            bool forward = true, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List messages from a chat channel.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="channelId">A channel identifier.</param>
        /// <param name="limit">The number of chat messages to list.</param>
        /// <param name="forward">Fetch messages forward from the current cursor (or the start, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default).</param>
        /// <param name="cursor">A cursor for the current position in the messages history to list.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the channel message list object.</returns>
        Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, string channelId, int limit = 1,
            bool forward = true, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List of friends of the current user.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="state">Filter by friendship state.</param>
        /// <param name="limit">The number of friends to list.</param>
        /// <param name="cursor">A cursor for the current position in the friends list.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the friend objects.</returns>
        Task<IApiFriendList> ListFriendsAsync(ISession session, int? state = null, int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List all users part of the group.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="state">Filter by group membership state.</param>
        /// <param name="limit">The number of groups to list.</param>
        /// <param name="cursor">A cursor for the current position in the group listing.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the group user objects.</returns>
        Task<IApiGroupUserList> ListGroupUsersAsync(ISession session, string groupId, int? state = null, int limit = 1,
            string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List groups on the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="name">The name filter to apply to the group list.</param>
        /// <param name="limit">The number of groups to list.</param>
        /// <param name="cursor">A cursor for the current position in the groups to list.</param>
        /// <param name="langTag">The language tag filter to apply to the group list.</param>
        /// <param name="members">The number of group members filter to apply to the group list.</param>
        /// <param name="open">The open/closed filter to apply to the group list.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task to resolve group objects.</returns>
        Task<IApiGroupList> ListGroupsAsync(ISession session, string name = null, int limit = 1, string cursor = null, string langTag = null, int? members = null, bool? open = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List records from a leaderboard.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="leaderboardId">The ID of the leaderboard to list.</param>
        /// <param name="ownerIds">Record owners to fetch with the list of records.</param>
        /// <param name="expiry">Expiry in seconds (since epoch) to begin fetching records from. Optional. 0 means from current time.</param>
        /// <param name="limit">The number of records to list.</param>
        /// <param name="cursor">A cursor for the current position in the leaderboard records to list.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the leaderboard record objects.</returns>
        Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAsync(ISession session, string leaderboardId,
            IEnumerable<string> ownerIds = null, long? expiry = null, int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List leaderboard records that belong to a user.
        /// </summary>
        /// <param name="session">The session for the user.</param>
        /// <param name="leaderboardId">The ID of the leaderboard to list.</param>
        /// <param name="ownerId">The ID of the user to list around.</param>
        /// <param name="expiry">Expiry in seconds (since epoch) to begin fetching records from. Optional. 0 means from current time.</param>
        /// <param name="limit">The limit of the listings.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the leaderboard record objects.</returns>
        Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAroundOwnerAsync(ISession session, string leaderboardId,
            string ownerId, long? expiry = null, int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Fetch a list of matches active on the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="min">The minimum number of match participants.</param>
        /// <param name="max">The maximum number of match participants.</param>
        /// <param name="limit">The number of matches to list.</param>
        /// <param name="authoritative">If authoritative matches should be included.</param>
        /// <param name="label">The label to filter the match list on.</param>
        /// <param name="query">A query for the matches to filter.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the match list object.</returns>
        Task<IApiMatchList> ListMatchesAsync(ISession session, int min, int max, int limit, bool authoritative,
            string label, string query, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List notifications for the user with an optional cursor.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="limit">The number of notifications to list.</param>
        /// <param name="cacheableCursor">A cursor for the current position in notifications to list.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task to resolve notifications objects.</returns>
        Task<IApiNotificationList> ListNotificationsAsync(ISession session, int limit = 1,
            string cacheableCursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        [Obsolete("ListStorageObjects is obsolete, please use ListStorageObjectsAsync instead.", false)]
        Task<IApiStorageObjectList> ListStorageObjects(ISession session, string collection, int limit = 1,
            string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List storage objects in a collection which have public read access.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="collection">The collection to list over.</param>
        /// <param name="limit">The number of objects to list. Maximum 100.</param>
        /// <param name="cursor">A cursor to paginate over the collection. May be null.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the storage object list.</returns>
        Task<IApiStorageObjectList> ListStorageObjectsAsync(ISession session, string collection, int limit = 1,
            string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List the user's subscriptions.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="limit">The number of subscriptions to list.</param>
        /// <param name="cursor">An optional cursor for the next page of subscriptions.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the subscription list.</returns>
        Task<IApiSubscriptionList> ListSubscriptionsAsync(ISession session, int limit, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List tournament records around the owner.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="tournamentId">The ID of the tournament.</param>
        /// <param name="ownerId">The ID of the owner to pivot around.</param>
        /// <param name="expiry">Expiry in seconds (since epoch) to begin fetching records from.</param>
        /// <param name="limit">The number of records to list.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the tournament record list object.</returns>
        Task<IApiTournamentRecordList> ListTournamentRecordsAroundOwnerAsync(ISession session, string tournamentId,
            string ownerId, long? expiry = null, int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List records from a tournament.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="tournamentId">The ID of the tournament.</param>
        /// <param name="ownerIds">The IDs of the record owners to return in the result.</param>
        /// <param name="expiry">Expiry in seconds (since epoch) to begin fetching records from.</param>
        /// <param name="limit">The number of records to list.</param>
        /// <param name="cursor">An optional cursor for the next page of tournament records.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the list of tournament records.</returns>
        Task<IApiTournamentRecordList> ListTournamentRecordsAsync(ISession session, string tournamentId,
            IEnumerable<string> ownerIds = null, long? expiry = null, int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List current or upcoming tournaments.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="categoryStart">The start of the category of tournaments to include.</param>
        /// <param name="categoryEnd">The end of the category of tournaments to include.</param>
        /// <param name="startTime">The start time of the tournaments. (UNIX timestamp, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default). If null, tournaments will not be filtered by start time.</param>
        /// <param name="endTime">The end time of the tournaments. (UNIX timestamp, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default). If null, tournaments will not be filtered by end time.</param>
        /// <param name="limit">The number of tournaments to list.</param>
        /// <param name="cursor">An optional cursor for the next page of tournaments.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the list of tournament objects.</returns>
        Task<IApiTournamentList> ListTournamentsAsync(ISession session, int categoryStart, int categoryEnd,
            int? startTime = null, int? endTime = null, int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List of groups the current user is a member of.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="state">Filter by group membership state.</param>
        /// <param name="limit">The number of records to list.</param>
        /// <param name="cursor">A cursor for the current position in the listing.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the group list object.</returns>
        Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, int? state = null, int limit = 1,
            string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List groups a user is a member of.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="userId">The ID of the user whose groups to list.</param>
        /// <param name="state">Filter by group membership state.</param>
        /// <param name="limit">The number of records to list.</param>
        /// <param name="cursor">A cursor for the current position in the listing.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the group list object.</returns>
        Task<IApiUserGroupList> ListUserGroupsAsync(ISession session, string userId, int? state = null, int limit = 1,
            string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// List storage objects in a collection which belong to a specific user and have public read access.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="collection">The collection to list over.</param>
        /// <param name="userId">The user ID of the user to list objects for.</param>
        /// <param name="limit">The number of objects to list.</param>
        /// <param name="cursor">A cursor to paginate over the collection.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the storage object list.</returns>
        Task<IApiStorageObjectList> ListUsersStorageObjectsAsync(ISession session, string collection, string userId,
            int limit = 1, string cursor = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Promote one or more users in the group.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The ID of the group to promote users into.</param>
        /// <param name="ids">The IDs of the users to promote.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task PromoteGroupUsersAsync(ISession session, string groupId, IEnumerable<string> ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Read one or more objects from the storage engine.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ids">The objects to read.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the storage batch object.</returns>
        Task<IApiStorageObjects> ReadStorageObjectsAsync(ISession session, IApiReadStorageObjectId[] ids, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Execute a function with an input payload on the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">The ID of the function to execute on the server.</param>
        /// <param name="payload">The payload to send with the function call.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the RPC response.</returns>
        Task<IApiRpc> RpcAsync(ISession session, string id, string payload, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Execute a function on the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">The ID of the function to execute on the server.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the RPC response.</returns>
        Task<IApiRpc> RpcAsync(ISession session, string id, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Execute a function on the server without a session.
        /// </summary>
        /// <remarks>
        /// This function is usually used with server side code. DO NOT USE client side.
        /// </remarks>
        /// <param name="httpKey">The secure HTTP key used to authenticate.</param>
        /// <param name="id">The id of the function to execute on the server.</param>
        /// <param name="payload">A payload to send with the function call.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task to resolve an RPC response.</returns>
        Task<IApiRpc> RpcAsync(string httpKey, string id, string payload, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Execute a function on the server without a session.
        /// </summary>
        /// <remarks>
        /// This function is usually used with server side code. DO NOT USE client side.
        /// </remarks>
        /// <param name="httpKey">The secure HTTP key used to authenticate.</param>
        /// <param name="id">The id of the function to execute on the server.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task to resolve an RPC response.</returns>
        Task<IApiRpc> RpcAsync(string httpKey, string id, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Log out a session which invalidates the authorization and refresh token.
        /// </summary>
        /// <param name="session">The session to logout.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task SessionLogoutAsync(ISession session, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Log out a session which optionally invalidates the authorization and/or refresh tokens.
        /// </summary>
        /// <param name="authToken">The authorization token to invalidate, may be <c>null</c>.</param>
        /// <param name="refreshToken">The refresh token to invalidate, may be <c>null</c>.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task SessionLogoutAsync(string authToken, string refreshToken, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Refresh the session unless the current refresh token has expired. If vars are specified they will replace
        /// what is currently stored inside the session token.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="vars">Extra information which should be bundled inside the session token.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a new session object.</returns>
        Task<ISession> SessionRefreshAsync(ISession session, Dictionary<string, string> vars = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Remove the Apple ID from the social profiles on the current user's account.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">The ID token received from Apple.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to a new session object.</returns>
        Task UnlinkAppleAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Unlink a custom ID from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">A custom identifier usually obtained from an external authentication service.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UnlinkCustomAsync(ISession session, string id, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Unlink a device ID from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="id">A device identifier usually obtained from a platform API.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UnlinkDeviceAsync(ISession session, string id, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Unlink an email with password from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="email">The email address of the user.</param>
        /// <param name="password">The password for the user.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UnlinkEmailAsync(ISession session, string email, string password, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Unlink a Facebook profile from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An OAuth access token from the Facebook SDK.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UnlinkFacebookAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Unlink a Game Center profile from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="bundleId">The bundle ID of the Game Center application.</param>
        /// <param name="playerId">The player ID of the user in Game Center.</param>
        /// <param name="publicKeyUrl">The URL for the public encryption key.</param>
        /// <param name="salt">A random <c>NSString</c> used to compute the hash and keep it randomized.</param>
        /// <param name="signature">The verification signature data generated.</param>
        /// <param name="timestamp">The date and time that the signature was created.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UnlinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl, string salt,
            string signature, string timestamp, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Unlink a Google profile from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An OAuth access token from the Google SDK.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UnlinkGoogleAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Unlink a Steam profile from the user account owned by the session.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="token">An authentication token from the Steam network.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UnlinkSteamAsync(ISession session, string token, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

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
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UpdateAccountAsync(ISession session, string username, string displayName = null,
            string avatarUrl = null, string langTag = null, string location = null, string timezone = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Update a group.
        /// </summary>
        /// <remarks>
        /// The user must have the correct access permissions for the group.
        /// </remarks>
        /// <param name="session">The session of the user.</param>
        /// <param name="groupId">The ID of the group to update.</param>
        /// <param name="name">A new name for the group.</param>
        /// <param name="open">If the group should have open membership.</param>
        /// <param name="description">A new description for the group.</param>
        /// <param name="avatarUrl">A new avatar url for the group.</param>
        /// <param name="langTag">A new language tag in BCP-47 format for the group.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UpdateGroupAsync(ISession session, string groupId, string name, bool open, string description = null,
            string avatarUrl = null, string langTag = null, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Validate a purchase receipt against the Apple App Store.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="receipt">The purchase receipt to be validated.</param>
        /// <param name="persist">Whether or not to track the receipt in the Nakama database.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the validated list of purchase receipts.</returns>
        Task<IApiValidatePurchaseResponse> ValidatePurchaseAppleAsync(ISession session, string receipt, bool persist = true, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Validate a purchase receipt against the Google Play Store.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="receipt">The purchase receipt to be validated.</param>
        /// <param name="persist">Whether or not to track the receipt in the Nakama database.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the validated list of purchase receipts.</returns>
        Task<IApiValidatePurchaseResponse> ValidatePurchaseGoogleAsync(ISession session, string receipt, bool persist = true, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Validate a purchase receipt against the Huawei AppGallery.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="receipt">The purchase receipt to be validated.</param>
        /// <param name="signature">The signature of the purchase receipt.</param>
        /// <param name="persist">Whether or not to track the receipt in the Nakama database.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the validated list of purchase receipts.</returns>
        Task<IApiValidatePurchaseResponse> ValidatePurchaseHuaweiAsync(ISession session, string receipt,
            string signature, bool persist = true, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Validate an Apple subscription receipt.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="receipt">The receipt to validate.</param>
        /// <param name="persist">Whether or not to persist the receipt to Nakama's database.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns> A task which resolves to the subscription validation response. </returns>
        Task<IApiValidateSubscriptionResponse> ValidateSubscriptionAppleAsync(ISession session, string receipt, bool persist = true, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Validate a Google subscription receipt.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="receipt">The receipt to validate.</param>
        /// <param name="persist">Whether or not to persist the receipt to Nakama's database.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns> A task which resolves to the subscription validation response. </returns>
        Task<IApiValidateSubscriptionResponse> ValidateSubscriptionGoogleAsync(ISession session, string receipt, bool persist = true, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Write a record to a leaderboard.
        /// </summary>
        /// <param name="session">The session for the user.</param>
        /// <param name="leaderboardId">The ID of the leaderboard to write.</param>
        /// <param name="score">The score for the leaderboard record.</param>
        /// <param name="subScore">The sub score for the leaderboard record.</param>
        /// <param name="metadata">The metadata for the leaderboard record.</param>
        /// <param name="operator"> The operator for the record that can be used to override the one set in the leaderboard.
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the leaderboard record object written.</returns>
        Task<IApiLeaderboardRecord> WriteLeaderboardRecordAsync(ISession session, string leaderboardId, long score,
            long subScore = 0L, string metadata = null, ApiOperator apiOperator = ApiOperator.NO_OVERRIDE, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Write objects to the storage engine.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="objects">The objects to write.</param>
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the storage write acknowledgements.</returns>
        Task<IApiStorageObjectAcks> WriteStorageObjectsAsync(ISession session, IApiWriteStorageObject[] objects, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);

        /// <summary>
        /// Write a record to a tournament.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="tournamentId">The ID of the tournament to write.</param>
        /// <param name="score">The score of the tournament record.</param>
        /// <param name="subScore">The sub score for the tournament record.</param>
        /// <param name="metadata">The metadata for the tournament record.</param>
        /// <param name="operator"> The operator for the record that can be used to override the one set in the tournament.
        /// <param name="retryConfiguration">The retry configuration. See <see cref="RetryConfiguration"/></param>
        /// <param name="canceller">The <see cref="CancellationToken"/> that can be used to cancel the request while mid-flight.</param>
        /// <returns>A task which resolves to the tournament record object written.</returns>
        Task<IApiLeaderboardRecord> WriteTournamentRecordAsync(ISession session, string tournamentId, long score,
            long subScore = 0L, string metadata = null, ApiOperator apiOperator = ApiOperator.NO_OVERRIDE, RetryConfiguration retryConfiguration = null, CancellationToken canceller = default);
	}
}