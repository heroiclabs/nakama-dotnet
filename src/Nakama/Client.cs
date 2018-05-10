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
            var resp = await _apiClient.AuthenticateCustomAsync(ServerKey, "", body);
            return Session.Restore(resp.Token);
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
