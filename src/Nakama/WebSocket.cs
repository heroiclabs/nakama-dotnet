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
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using vtortola.WebSockets;
    using vtortola.WebSockets.Rfc6455;

    /// <summary>
    /// A socket which uses the WebSocket protocol to interact with Nakama server.
    /// </summary>
    internal class WebSocket : ISocket
    {
        /// <inheritdoc />
        public SocketProtocol Protocol { get; }

        /// <inheritdoc />
        public int Reconnect { get; set; }

        /// <inheritdoc />
        public ILogger Logger { get; set; }

        /// <inheritdoc />
        public bool Trace { get; set; }

        /// <inheritdoc />
        public Action<IApiChannelMessage> OnChannelMessage { get; set; }

        /// <inheritdoc />
        public Action<IChannelPresenceEvent> OnChannelPresence { get; set; }

        /// <inheritdoc />
        public Action OnConnect { get; set; }

        /// <inheritdoc />
        public Action OnDisconnect { get; set; }

        /// <inheritdoc />
        public Action OnError { get; set; }

        /// <inheritdoc />
        public Action<IMatchState> OnMatchState { get; set; }

        /// <inheritdoc />
        public Action<IMatchPresenceEvent> OnMatchPresence { get; set; }

        /// <inheritdoc />
        public Action<IMatchmakerMatched> OnMatchmakerMatched { get; set; }

        /// <inheritdoc />
        public Action<IApiNotification> OnNotification { get; set; }

        /// <inheritdoc />
        public Action<IStatusPresenceEvent> OnStatusPresence { get; set; }

        /// <inheritdoc />
        public Action<IStreamPresenceEvent> OnStreamPresence { get; set; }

        /// <inheritdoc />
        public Action<IStreamState> OnStreamState { get; set; }

        private readonly Uri _baseUri;

        private vtortola.WebSockets.WebSocket _listener;

        public WebSocket(Uri baseUri, int reconnect, ILogger logger, bool trace)
        {
            _baseUri = baseUri;
            Logger = logger;
            OnChannelMessage = message =>
            {
                if (Trace)
                {
                    Logger.DebugFormat("Channel message received: {0}", message);
                }
            };
            OnChannelPresence = _event =>
            {
                if (Trace)
                {
                    Logger.DebugFormat("Channel presence received: {0}", _event);
                }
            };
            OnConnect = () =>
            {
                if (Trace)
                {
                    Logger.Debug("Socket connected.");
                }
            };
            OnDisconnect = () =>
            {
                if (Trace)
                {
                    Logger.Debug("Socket disconnected.");
                }
            };
            // FIXME needs error reason.
            OnError = () => Logger.Error("Socket error.");
            OnMatchmakerMatched = matched =>
            {
                if (Trace)
                {
                    Logger.DebugFormat("Matchmaker matched received: {0}", matched);
                }
            };
            OnMatchPresence = _event =>
            {
                if (Trace)
                {
                    Logger.DebugFormat("Match presence received: {0}", _event);
                }
            };
            OnMatchState = state =>
            {
                if (Trace)
                {
                    Logger.DebugFormat("Match state received: {0}", state);
                }
            };
            OnNotification = notification =>
            {
                if (Trace)
                {
                    Logger.DebugFormat("Notification received: {0}", notification);
                }
            };
            OnStatusPresence = _event =>
            {
                if (Trace)
                {
                    Logger.DebugFormat("Status presence received: {0}", _event);
                }
            };
            OnStreamPresence = _event =>
            {
                if (Trace)
                {
                    Logger.DebugFormat("Stream presence received: {0}", _event);
                }
            };
            OnStreamState = state =>
            {
                if (Trace)
                {
                    Logger.DebugFormat("Stream state received: {0}", state);
                }
            };
            Protocol = SocketProtocol.WebSocket;
            Reconnect = reconnect;
            Trace = trace;
        }

        /// <inheritdoc />
        public async Task<ISession> ConnectAsync(ISession session, bool appearOnline = false)
        {
            return await ConnectAsync(session, appearOnline, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<ISession> ConnectAsync(ISession session, bool appearOnline, CancellationToken ct)
        {
            // FIXME make configurable.
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
                tcp.BacklogSize = 32;
                tcp.ReceiveBufferSize = bufferSize;
                tcp.SendBufferSize = bufferSize;
                tcp.NoDelay = true;
                tcp.DualMode = true;
            });

            var client = new WebSocketClient(options);
            var addr = new UriBuilder(_baseUri)
            {
                Path = "/ws",
                Query = string.Concat("lang=en&status=", appearOnline, "&token=", session.AuthToken)
            };

            if (_listener != null)
            {
                await _listener.CloseAsync();
                _listener = null;
            }
            _listener = await client.ConnectAsync(addr.Uri, ct);

            return session;
        }

        /// <inheritdoc />
        public async Task DisconnectAsync(bool dispatch = true)
        {
            await _listener.CloseAsync().ContinueWith((task, o) =>
            {
                if (dispatch) OnDisconnect.Invoke();
            }, null);
        }
    }
}
