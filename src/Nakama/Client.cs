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

using System.Collections.Generic;
using System.Threading;

namespace Nakama
{
    using System;
    using System.Threading.Tasks;
    using vtortola.WebSockets;
    using vtortola.WebSockets.Rfc6455;

    /// <inheritdoc />
    public class Client : IClient
    {
        /// <summary>
        /// The default host address used by the client.
        /// </summary>
        private const string DefaultHost = "127.0.0.1";

        /// <summary>
        /// The default port used by the client.
        /// </summary>
        private const int DefaultPort = 7350;

        /// <summary>
        /// The default server key used to authenticate with the server.
        /// </summary>
        private const string DefaultServerKey = "defaultkey";

        /// <inheritdoc />
        public string Host { get; }

        /// <inheritdoc />
        public ILogger Logger { get; set; }

        /// <inheritdoc />
        public int Port { get; }

        /// <inheritdoc />
        public int Retries { get; set; }

        /// <inheritdoc />
        public string ServerKey { get; }

        /// <inheritdoc />
        public bool Secure { get; }

        /// <inheritdoc />
        public bool Trace { get; set; }

        /// <inheritdoc />
        public int Timeout { get; set; }

        private readonly ApiClient _apiClient;
        private WebSocketClient _client;

        public Client(string serverKey = DefaultServerKey, string host = DefaultHost, int port = DefaultPort,
            bool secure = false)
        {
            ServerKey = serverKey;
            Host = host;
            Logger = new NoopLogger(); // dont log by default.
            Port = port;
            Retries = 3;
            Secure = secure;
            Trace = false;
            Timeout = 5000;

            var scheme = Secure ? "https" : "http";
            _apiClient = new ApiClient(null, new UriBuilder(scheme, Host, Port).Uri);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateCustomAsync(string id)
        {
            var body = new ApiAccountCustom {Id = id};
            var resp = await _apiClient.AuthenticateCustomAsync(ServerKey, string.Empty, body);
            return Session.Restore(resp.Token);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateDeviceAsync(string id)
        {
            var body = new ApiAccountDevice {Id = id};
            var resp = await _apiClient.AuthenticateDeviceAsync(ServerKey, string.Empty, body);
            return Session.Restore(resp.Token);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateEmailAsync(string email, string password)
        {
            var body = new ApiAccountEmail {Email = email, Password = password};
            var resp = await _apiClient.AuthenticateEmailAsync(ServerKey, string.Empty, body);
            return Session.Restore(resp.Token);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateFacebookAsync(string token)
        {
            var body = new ApiAccountFacebook {Token = token};
            var resp = await _apiClient.AuthenticateFacebookAsync(ServerKey, string.Empty, body);
            return Session.Restore(resp.Token);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateGameCenterAsync(string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestampSeconds)
        {
            var body = new ApiAccountGameCenter
            {
                BundleId = bundleId,
                PlayerId = playerId,
                PublicKeyUrl = publicKeyUrl,
                Salt = salt,
                Signature = signature,
                TimestampSeconds = timestampSeconds
            };
            var resp = await _apiClient.AuthenticateGameCenterAsync(ServerKey, string.Empty, body);
            return Session.Restore(resp.Token);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateGoogleAsync(string token)
        {
            var body = new ApiAccountGoogle {Token = token};
            var resp = await _apiClient.AuthenticateGoogleAsync(ServerKey, string.Empty, body);
            return Session.Restore(resp.Token);
        }

        /// <inheritdoc />
        public async Task<ISession> AuthenticateSteamAsync(string token)
        {
            var body = new ApiAccountSteam {Token = token};
            var resp = await _apiClient.AuthenticateSteamAsync(ServerKey, string.Empty, body);
            return Session.Restore(resp.Token);
        }

        /// <inheritdoc />
        public async Task DeleteFriendsAsync(ISession session, IEnumerable<string> ids,
            IEnumerable<string> usernames = null)
        {
            if (ids == null)
            {
                ids = new string[0];
            }
            if (usernames == null)
            {
                usernames = new string[0];
            }
            await _apiClient.DeleteFriendsAsync(session.AuthToken, ids, usernames);
        }

        /// <inheritdoc />
        public async Task<IApiAccount> GetAccountAsync(ISession session)
        {
            return await _apiClient.GetAccountAsync(session.AuthToken);
        }

        /// <inheritdoc />
        public async Task LinkCustomAsync(ISession session, string id)
        {
            var body = new ApiAccountCustom {Id = id};
            await _apiClient.LinkCustomAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task LinkDeviceAsync(ISession session, string id)
        {
            var body = new ApiAccountDevice {Id = id};
            await _apiClient.LinkDeviceAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task LinkEmailAsync(ISession session, string email, string password)
        {
            var body = new ApiAccountEmail {Email = email, Password = password};
            await _apiClient.LinkEmailAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task LinkFacebookAsync(ISession session, string token)
        {
            var body = new ApiAccountFacebook {Token = token};
            await _apiClient.LinkFacebookAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task LinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestampSeconds)
        {
            var body = new ApiAccountGameCenter
            {
                BundleId = bundleId,
                PlayerId = playerId,
                PublicKeyUrl = publicKeyUrl,
                Salt = salt,
                Signature = signature,
                TimestampSeconds = timestampSeconds
            };
            await _apiClient.LinkGameCenterAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task LinkGoogleAsync(ISession session, string token)
        {
            var body = new ApiAccountGoogle {Token = token};
            await _apiClient.LinkGoogleAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task LinkSteamAsync(ISession session, string token)
        {
            var body = new ApiAccountSteam {Token = token};
            await _apiClient.LinkSteamAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task<IApiChannelMessageList> ListChannelMessagesAsync(ISession session, string channelId,
            int limit = 1, bool forward = true, string cursor = null)
        {
            return await _apiClient.ListChannelMessagesAsync(session.AuthToken, channelId, limit, forward, cursor);
        }

        /// <inheritdoc />
        public async Task UnlinkCustomAsync(ISession session, string id)
        {
            var body = new ApiAccountCustom {Id = id};
            await _apiClient.UnlinkCustomAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task UnlinkDeviceAsync(ISession session, string id)
        {
            var body = new ApiAccountDevice {Id = id};
            await _apiClient.UnlinkDeviceAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task UnlinkEmailAsync(ISession session, string email, string password)
        {
            var body = new ApiAccountEmail {Email = email, Password = password};
            await _apiClient.UnlinkEmailAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task UnlinkFacebookAsync(ISession session, string token)
        {
            var body = new ApiAccountFacebook {Token = token};
            await _apiClient.UnlinkFacebookAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task UnlinkGameCenterAsync(ISession session, string bundleId, string playerId, string publicKeyUrl,
            string salt, string signature, string timestampSeconds)
        {
            var body = new ApiAccountGameCenter
            {
                BundleId = bundleId,
                PlayerId = playerId,
                PublicKeyUrl = publicKeyUrl,
                Salt = salt,
                Signature = signature,
                TimestampSeconds = timestampSeconds
            };
            await _apiClient.UnlinkGameCenterAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task UnlinkGoogleAsync(ISession session, string token)
        {
            var body = new ApiAccountGoogle {Token = token};
            await _apiClient.UnlinkGoogleAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task UnlinkSteamAsync(ISession session, string token)
        {
            var body = new ApiAccountSteam {Token = token};
            await _apiClient.UnlinkSteamAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task UpdateAccountAsync(ISession session, string username = null, string displayName = null,
            string avatarUrl = null, string langTag = null, string location = null, string timezone = null)
        {
            var body = new ApiUpdateAccountRequest
            {
                AvatarUrl = avatarUrl,
                DisplayName = displayName,
                LangTag = langTag,
                Location = location,
                Timezone = timezone,
                Username = username
            };
            await _apiClient.UpdateAccountAsync(session.AuthToken, body);
        }

        /// <inheritdoc />
        public async Task<ISocket> CreateWebSocketAsync(ISession session, bool appearOnline = true, int reconnect = 3)
        {
            lock (this)
            {
                if (_client == null)
                {
                    const int bufferSize = 1024 * 8; // 8KiB
                    const int bufferPoolSize = 100 * bufferSize; // 800KiB pool

                    var options = new WebSocketListenerOptions
                    {
                        SendBufferSize = bufferSize,
                        BufferManager = BufferManager.CreateBufferManager(bufferPoolSize, bufferSize),
                        Logger = new WebSocketLogger(Logger, Trace)
                    };
                    options.Standards.RegisterRfc6455();

                    options.Transports.ConfigureTcp(tcp =>
                    {
                        tcp.BacklogSize = 10;
                        tcp.ReceiveBufferSize = bufferSize;
                        tcp.SendBufferSize = bufferSize;
                        tcp.NoDelay = true;
                        tcp.DualMode = true;
                    });

                    _client = new WebSocketClient(options);
                }
            }

            var scheme = Secure ? "wss" : "ws";
            var baseAddr = new UriBuilder(scheme, Host, Port)
            {
                Path = "/ws",
                Query = string.Concat("lang=en&status=", appearOnline, "&token=", session.AuthToken)
            };

            var websocket = await _client.ConnectAsync(baseAddr.Uri, CancellationToken.None);
            return new WebSocket(websocket, reconnect);
        }
    }
}
