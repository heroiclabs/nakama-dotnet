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
    using System.Collections.Concurrent;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using vtortola.WebSockets;
    using vtortola.WebSockets.Rfc6455;
    using TinyJson;

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
        public Action<IApiChannelMessage> OnChannelMessage { private get; set; }

        /// <inheritdoc />
        public Action<IChannelPresenceEvent> OnChannelPresence { get; set; }

        /// <inheritdoc />
        public Action OnConnect { get; set; }

        /// <inheritdoc />
        public Action OnDisconnect { get; set; }

        /// <inheritdoc />
        public Action<Exception> OnError { get; set; }

        /// <inheritdoc />
        public Action<IMatchmakerMatched> OnMatchmakerMatched { get; set; }

        /// <inheritdoc />
        public Action<IMatchState> OnMatchState { get; set; }

        /// <inheritdoc />
        public Action<IMatchPresenceEvent> OnMatchPresence { get; set; }

        /// <inheritdoc />
        public Action<IApiNotification> OnNotification { get; set; }

        /// <inheritdoc />
        public Action<IStatusPresenceEvent> OnStatusPresence { get; set; }

        /// <inheritdoc />
        public Action<IStreamPresenceEvent> OnStreamPresence { get; set; }

        /// <inheritdoc />
        public Action<IStreamState> OnStreamState { get; set; }

        private readonly Uri _baseUri;
        private readonly WebSocketClient _client;
        private vtortola.WebSockets.WebSocket _listener;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<WebSocketMessageEnvelope>> _messageReplies;
        private readonly BufferBlock<string> _sendBuffer;

        public WebSocket(Uri baseUri, int reconnect, ILogger logger, bool trace)
        {
            _baseUri = baseUri;

            // FIXME make configurable.
            const int bufferSize = 1024 * 8; // 8KiB
            const int bufferPoolSize = 100 * bufferSize; // 800KiB pool

            var options = new WebSocketListenerOptions
            {
                BufferManager = BufferManager.CreateBufferManager(bufferPoolSize, bufferSize),
                Logger = new WebSocketLogger(Logger, Trace),
                // FIXME make configurable.
                PingTimeout = TimeSpan.FromSeconds(10),
                SendBufferSize = bufferSize,
                PingMode = PingMode.BandwidthSaving
            };
            options.Standards.RegisterRfc6455();

            options.Transports.ConfigureTcp(tcp =>
            {
                tcp.BacklogSize = 32;
                tcp.ReceiveBufferSize = bufferSize;
                tcp.SendBufferSize = bufferSize;
                tcp.NoDelay = true;
                // Dual mode needed for IPv6 support.
                tcp.DualMode = true;
            });

            _client = new WebSocketClient(options);
            _messageReplies = new ConcurrentDictionary<string, TaskCompletionSource<WebSocketMessageEnvelope>>();
            _sendBuffer = new BufferBlock<string>(new DataflowBlockOptions
            {
                // FIXME make configurable.
                BoundedCapacity = 16
            });
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
            OnError = e => Logger.ErrorFormat("Socket error: {0}", e.Message);
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
        public async Task Connect(ISession session, CancellationToken ct = default(CancellationToken),
            bool appearOnline = false,
            int connectTimeout = 5000)
        {
            if (_listener != null)
            {
                await _listener.CloseAsync();
                _listener.Dispose();
                _listener = null;
            }

            var addr = new UriBuilder(_baseUri)
            {
                Path = "/ws",
                Query = string.Concat("lang=en&status=", appearOnline, "&token=", session.AuthToken)
            };

            var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(connectTimeout), ct);
            var connectTask = _client.ConnectAsync(addr.Uri, ct);

            // Limit time (msec) allowed for connect attempts.
            if (await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false) == timeoutTask)
            {
                throw new TimeoutException($"Socket connect timed out after '{connectTimeout}' milliseconds.");
            }

            _listener = await connectTask.ConfigureAwait(false);
            OnConnect();

            var t = new Thread(() =>
            {
                ReadSocketAsync(ct);
                WriteSocketAsync(ct);
            });
            t.Start();
        }

        /// <inheritdoc />
        public async Task DisconnectAsync(bool dispatch = true)
        {
            if (_listener != null)
            {
                await _listener.CloseAsync();
            }
            if (dispatch) OnDisconnect();
        }

        public void Dispose()
        {
            _listener?.Dispose();
            OnDisconnect();
        }

        /// <inheritdoc />
        public async Task<IChannel> SendAsync(ChannelJoinMessage message, int sendTimeout = 5000)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                ChannelJoin = message
            };
            var response = await SendAsync(envelope, sendTimeout);
            return response.Channel;
        }

        /// <inheritdoc />
        public async Task<IChannelMessageAck> SendAsync(ChannelSendMessage message, int sendTimeout = 5000)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                ChannelSend = message
            };
            var response = await SendAsync(envelope, sendTimeout);
            return response.ChannelMessageAck;
        }

        /// <inheritdoc />
        public async Task<IApiRpc> SendAsync(RpcMessage message, int sendTimeout = 5000)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Rpc = new ApiRpc
                {
                    Id = message.Id,
                    Payload = message.Payload
                }
            };
            var response = await SendAsync(envelope, sendTimeout);
            return response.Rpc;
        }

        private async Task<WebSocketMessageEnvelope> SendAsync(WebSocketMessageEnvelope message, int sendTimeout = 5000)
        {
            if (_listener == null || !_listener.IsConnected)
            {
                throw new InvalidOperationException("Socket is not connected.");
            }

            var cid = Guid.NewGuid().ToString();
            message.Cid = cid;
            await _sendBuffer.SendAsync(message.ToJson()).ConfigureAwait(false);

            var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(sendTimeout));
            var completer = new TaskCompletionSource<WebSocketMessageEnvelope>();
            _messageReplies[cid] = completer;
            var resultTask = completer.Task;

            if (await Task.WhenAny(resultTask, timeoutTask).ConfigureAwait(false) == timeoutTask)
            {
                throw new TimeoutException($"Socket send timed out after '{sendTimeout}' milliseconds.");
            }

            return await resultTask.ConfigureAwait(false);
        }

        private async void ReadSocketAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _listener.IsConnected)
                {
                    var readStream = await _listener.ReadMessageAsync(ct).ConfigureAwait(false);
                    if (readStream == null)
                    {
                        continue; // NOTE does stream need to be consumed?
                    }

                    using (var reader = new StreamReader(readStream, Encoding.UTF8))
                    {
                        var message = await reader.ReadToEndAsync().ConfigureAwait(false);
                        Console.WriteLine(message);
                        var envelope = message.FromJson<WebSocketMessageEnvelope>();
                        Console.WriteLine("ENVELOPE - " + envelope);
                        if (envelope.Cid != null)
                        {
                            // Handle message response.
                            var cid = envelope.Cid;
                            TaskCompletionSource<WebSocketMessageEnvelope> completer;
                            _messageReplies.TryRemove(cid, out completer);
                            if (completer == null)
                            {
                                if (Trace) Logger.InfoFormat("No task completer for message: '{0}'", cid);
                                continue;
                            }
                            completer.SetResult(envelope);
                        }
                        else if (envelope.ChannelMessage != null)
                        {
                            Console.WriteLine("CHANNEL MESSAGE - " + envelope.ChannelMessage);
                            Console.WriteLine("EVENT HANDLER - " + OnChannelMessage);
                            OnChannelMessage(envelope.ChannelMessage);
                            Console.WriteLine("EVENT HANDLER ENDED");
                        }
                        else if (envelope.ChannelPresenceEvent != null)
                        {
                            Console.WriteLine("CHANNEL PRESENCE EVENT - " + envelope.ChannelPresenceEvent);
                            OnChannelPresence(envelope.ChannelPresenceEvent);
                        }
                        else if (envelope.MatchmakerMatched != null)
                        {
                            OnMatchmakerMatched(envelope.MatchmakerMatched);
                        }
                        else if (envelope.MatchPresenceEvent != null)
                        {
                            OnMatchPresence(envelope.MatchPresenceEvent);
                        }
                        else if (envelope.MatchState != null)
                        {
                            OnMatchState(envelope.MatchState);
                        }
                        else if (envelope.Notifications != null)
                        {
                            foreach (var notification in envelope.Notifications)
                            {
                                OnNotification(notification);
                            }
                        }
                        else if (envelope.StatusPresenceEvent != null)
                        {
                            OnStatusPresence(envelope.StatusPresenceEvent);
                        }
                        else if (envelope.StreamPresenceEvent != null)
                        {
                            OnStreamPresence(envelope.StreamPresenceEvent);
                        }
                        else if (envelope.StreamState != null)
                        {
                            OnStreamState(envelope.StreamState);
                        }
                        else
                        {
                            if (Trace) Logger.InfoFormat("Socket received unrecognised message: '{0}'", message);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _listener.Dispose();
                _listener = null;
            }
        }

        private async void WriteSocketAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _listener.IsConnected)
                {
                    var message = await _sendBuffer.ReceiveAsync(ct).ConfigureAwait(false);
                    using (var output = _listener.CreateMessageWriter(WebSocketMessageType.Text))
                    using (var writer = new StreamWriter(output, Encoding.ASCII)) // FIXME do we need ASCII encoded?
                    {
                        if (Trace)
                        {
                            Logger.DebugFormat("Socket write message: '{0}'", message);
                        }
                        await writer.WriteAsync(message).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }
    }
}
