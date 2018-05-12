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
        /// A logger which can write log messages. Defaults to <c>NoopLogger</c>.
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
        /// Authenticate a user with a custom id.
        /// </summary>
        /// <param name="id">A custom identifier usually obtained from an external authentication service.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateCustomAsync(string id);

        /// <summary>
        /// Authenticate a user with a device id.
        /// </summary>
        /// <param name="id">A device identifier usually obtained from a platform API.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateDeviceAsync(string id);

        /// <summary>
        /// Authenticate a user with an email and password.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <param name="password">The password for the user.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateEmailAsync(string email, string password);

        /// <summary>
        /// Authenticate a user with a Facebook auth token.
        /// </summary>
        /// <param name="token">An OAuth access token from the Facebook SDK.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateFacebookAsync(string token);

        /// <summary>
        /// Authenticate a user with Apple Game Center.
        /// </summary>
        /// <param name="bundleId">The bundle id of the Game Center application.</param>
        /// <param name="playerId">The player id of the user in Game Center.</param>
        /// <param name="publicKeyUrl">The URL for the public encryption key.</param>
        /// <param name="salt">A random <c>NSString</c> used to compute the hash and keep it randomized.</param>
        /// <param name="signature">The verification signature data generated.</param>
        /// <param name="timestampSeconds">The date and time that the signature was created.</param>
        /// <returns></returns>
        Task<ISession> AuthenticateGameCenterAsync(string bundleId, string playerId, string publicKeyUrl, string salt,
            string signature, string timestampSeconds);

        /// <summary>
        /// Authenticate a user with a Google auth token.
        /// </summary>
        /// <param name="token">An OAuth access token from the Google SDK.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateGoogleAsync(string token);

        /// <summary>
        /// Authenticate a user with a Steam auth token.
        /// </summary>
        /// <param name="token">An authentication token from the Steam network.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateSteamAsync(string token);

        /// <summary>
        /// Delete one more or users by id or username from friends.
        /// </summary>
        /// <param name="session">The session for the user.</param>
        /// <param name="ids">The user ids to remove as friends.</param>
        /// <param name="usernames">The usernames to remove as friends.</param>
        /// <returns></returns>
        Task DeleteFriendsAsync(ISession session, IEnumerable<string> ids, IEnumerable<string> usernames = null);

        /// <summary>
        /// Fetch the user account owned by the session.
        /// </summary>
        /// <param name="session">The session for the user.</param>
        /// <returns>A task to resolve an account object.</returns>
        Task<IApiAccount> GetAccountAsync(ISession session);

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
        /// <returns>A task.</returns>
        Task LinkFacebookAsync(ISession session, string token);

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
        /// <returns></returns>
        Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, string channelId, int limit = 1,
            bool forward = true, string cursor = null);

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
        /// <param name="displayName">The new display name for the user.</param>
        /// <param name="avatarUrl">The new avatar url for the user.</param>
        /// <param name="langTag">The new language tag in BCP-47 format for the user.</param>
        /// <param name="location">The new location for the user.</param>
        /// <param name="timezone">The new timezone information for the user.</param>
        /// <returns>A task to complete the account update.</returns>
        Task UpdateAccountAsync(ISession session, string username = null, string displayName = null,
            string avatarUrl = null, string langTag = null, string location = null, string timezone = null);

        /// <summary>
        /// Create a new WebSocket from the client.
        /// </summary>
        /// <param name="session">The session for the current authenticated user.</param>
        /// <param name="appearOnline">True if the user should appear online to other users.</param>
        /// <param name="reconnect">Set the number of retries to attempt after a disconnect.</param>
        /// <returns>A socket object.</returns>
        Task<ISocket> CreateWebSocketAsync(ISession session, bool appearOnline = true, int reconnect = 3);
    }
}
