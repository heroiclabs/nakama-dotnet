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
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using vtortola.WebSockets;
    using vtortola.WebSockets.Rfc6455;
    using TinyJson;

    /// <summary>
    /// A socket which uses the WebSocket protocol to interact with Nakama server.
    /// </summary>
    internal class WebSocket : ISocket
    {
        /// <inheritdoc />
        public ILogger Logger { get; set; }

        /// <inheritdoc />
        public SocketProtocol Protocol { get; }

        /// <inheritdoc />
        public int Reconnect { get; set; }

        /// <inheritdoc />
        public int TimeoutMs { get; set; }

        /// <inheritdoc />
        public bool Trace { get; set; }

        /// <inheritdoc />
        public event EventHandler<IApiChannelMessage> OnChannelMessage;

        /// <inheritdoc />
        public event EventHandler<IChannelPresenceEvent> OnChannelPresence;

        /// <inheritdoc />
        public event EventHandler OnConnect;

        /// <inheritdoc />
        public event EventHandler OnDisconnect;

        /// <inheritdoc />
        public event EventHandler<Exception> OnError;

        /// <inheritdoc />
        public event EventHandler<IMatchmakerMatched> OnMatchmakerMatched;

        /// <inheritdoc />
        public event EventHandler<IMatchState> OnMatchState;

        /// <inheritdoc />
        public event EventHandler<IMatchPresenceEvent> OnMatchPresence;

        /// <inheritdoc />
        public event EventHandler<IApiNotification> OnNotification;

        /// <inheritdoc />
        public event EventHandler<IStatusPresenceEvent> OnStatusPresence;

        /// <inheritdoc />
        public event EventHandler<IStreamPresenceEvent> OnStreamPresence;

        /// <inheritdoc />
        public event EventHandler<IStreamState> OnStreamState;

        private readonly Uri _baseUri;
        private readonly WebSocketClient _client;
        private vtortola.WebSockets.WebSocket _listener;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<WebSocketMessageEnvelope>> _messageReplies;

        private readonly AsyncQueue<string> _sendBuffer;

        public WebSocket(Uri baseUri, int timeoutMs, int reconnect, ILogger logger, bool trace)
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
                PingTimeout = Timeout.InfiniteTimeSpan,
                SendBufferSize = bufferSize,
                PingMode = PingMode.Manual
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
            _sendBuffer = new AsyncQueue<string>();
            Logger = logger;
            OnError += (_, exception) => Logger.ErrorFormat("Socket error: {0}", exception.Message);
            if (Trace)
            {
                OnChannelMessage += (_, message) => Logger.DebugFormat("Channel message received: {0}", message);
                OnChannelPresence += (_, presence) => Logger.DebugFormat("Channel presence received: {0}", presence);
                OnConnect += (_, args) => Logger.Debug("Socket connected.");
                OnDisconnect += (_, args) => Logger.Debug("Socket disconnected.");
                OnMatchmakerMatched += (_, matched) => Logger.DebugFormat("Matchmaker matched received: {0}", matched);
                OnMatchPresence += (_, presence) => Logger.DebugFormat("Match presence received: {0}", presence);
                OnMatchState += (_, state) => Logger.DebugFormat("Match state received: {0}", state);
                OnNotification += (_, notification) => Logger.DebugFormat("Notification received: {0}", notification);
                OnStatusPresence += (_, presence) => Logger.DebugFormat("Status presence received: {0}", presence);
                OnStreamPresence += (_, presence) => Logger.DebugFormat("Stream presence received: {0}", presence);
                OnStreamState += (_, state) => Logger.DebugFormat("Stream state received: {0}", state);
            }

            Protocol = SocketProtocol.WebSocket;
            Reconnect = reconnect;
            TimeoutMs = timeoutMs;
            Trace = trace;
        }

        /// <inheritdoc />
        public async Task<IMatchmakerTicket> AddMatchmakerAsync(string query = "*", int minCount = 2, int maxCount = 8,
            Dictionary<string, string> stringProperties = null, Dictionary<string, double> numericProperties = null)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                MatchmakerAdd = new MatchmakerAddMessage
                {
                    MaxCount = maxCount,
                    MinCount = minCount,
                    NumericProperties = numericProperties ?? new Dictionary<string, double>(),
                    StringProperties = stringProperties ?? new Dictionary<string, string>(),
                    Query = query
                }
            };
            var response = await SendAsync(envelope).ConfigureAwait(false);
            return response.MatchmakerTicket;
        }

        /// <inheritdoc />
        public async Task ConnectAsync(ISession session, CancellationToken ct = default(CancellationToken),
            bool appearOnline = false, int connectTimeout = 5000)
        {
            if (_listener != null)
            {
                await _listener.CloseAsync();
                _messageReplies.Clear();
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
            ReadSocketAsync(ct);
            WriteSocketAsync(ct);
        }

        /// <inheritdoc />
        public async Task DisconnectAsync(bool dispatch = true)
        {
            if (_listener != null)
            {
                await _listener.CloseAsync().ConfigureAwait(false);
            }

            if (dispatch)
            {
                OnDisconnect?.Invoke(this, EventArgs.Empty);
            }

            _messageReplies.Clear();
        }

        public void Dispose()
        {
            _messageReplies.Clear();
            _listener?.Dispose();
            OnDisconnect?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public async Task<IMatch> CreateMatchAsync()
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                MatchCreate = new MatchCreateMessage()
            };
            var response = await SendAsync(envelope).ConfigureAwait(false);
            return response.Match;
        }

        /// <inheritdoc />
        public async Task<IChannel> JoinChatAsync(string target, ChannelType type, bool persistence = false,
            bool hidden = false)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                ChannelJoin = new ChannelJoinMessage
                {
                    Hidden = hidden,
                    Persistence = persistence,
                    Target = target,
                    Type = (int) type
                }
            };
            var response = await SendAsync(envelope).ConfigureAwait(false);
            return response.Channel;
        }

        /// <inheritdoc />
        public async Task<IStatus> FollowUsersAsync(IEnumerable<string> userIds)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                StatusFollow = new StatusFollowMessage
                {
                    UserIds = new List<string>(userIds)
                }
            };
            var response = await SendAsync(envelope);
            return response.Status;
        }

        /// <inheritdoc />
        public async Task<IMatch> JoinMatchAsync(IMatchmakerMatched matched)
        {
            var message = new MatchJoinMessage();
            if (matched.Token != null)
            {
                message.Token = matched.Token;
            }
            else
            {
                message.MatchId = matched.MatchId;
            }

            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                MatchJoin = message
            };
            var response = await SendAsync(envelope).ConfigureAwait(false);
            return response.Match;
        }

        /// <inheritdoc />
        public async Task<IMatch> JoinMatchAsync(string matchId)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                MatchJoin = new MatchJoinMessage
                {
                    MatchId = matchId
                }
            };
            var response = await SendAsync(envelope).ConfigureAwait(false);
            return response.Match;
        }

        /// <inheritdoc />
        public async Task LeaveChatAsync(IChannel channel) => await LeaveChatAsync(channel.Id);

        /// <inheritdoc />
        public async Task LeaveChatAsync(string channelId)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                ChannelLeave = new ChannelLeaveMessage
                {
                    ChannelId = channelId
                }
            };
            await SendAsync(envelope).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task LeaveMatchAsync(IMatch match) => await LeaveMatchAsync(match.Id);

        /// <inheritdoc />
        public async Task LeaveMatchAsync(string matchId)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                MatchLeave = new MatchLeaveMessage
                {
                    MatchId = matchId
                }
            };
            await SendAsync(envelope).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IChannelMessageAck> RemoveChatMessageAsync(IChannel channel, string messageId) =>
            await RemoveChatMessageAsync(channel.Id, messageId);

        /// <inheritdoc />
        public async Task<IChannelMessageAck> RemoveChatMessageAsync(string channelId, string messageId)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                ChannelMessageRemove = new ChannelRemoveMessage
                {
                    ChannelId = channelId,
                    MessageId = messageId
                }
            };
            var response = await SendAsync(envelope).ConfigureAwait(false);
            return response.ChannelMessageAck;
        }

        /// <inheritdoc />
        public async Task RemoveMatchmakerAsync(IMatchmakerTicket matchmakerTicket) =>
            await RemoveMatchmakerAsync(matchmakerTicket.Ticket);

        /// <inheritdoc />
        public async Task RemoveMatchmakerAsync(string ticket)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                MatchmakerRemove = new MatchmakerRemoveMessage
                {
                    Ticket = ticket
                }
            };
            await SendAsync(envelope).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IApiRpc> RpcAsync(string id, string content)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                Rpc = new ApiRpc
                {
                    Id = id,
                    Payload = content
                }
            };
            var response = await SendAsync(envelope).ConfigureAwait(false);
            return response.Rpc;
        }

        /// <inheritdoc />
        public async Task SendMatchStateAsync(string matchId, long opCode, string state,
            IEnumerable<IUserPresence> presences = null) =>
            await SendMatchStateAsync(matchId, opCode, Encoding.UTF8.GetBytes(state), presences);

        /// <inheritdoc />
        public async Task SendMatchStateAsync(string matchId, long opCode, byte[] state,
            IEnumerable<IUserPresence> presences = null)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                MatchStateSend = new MatchSendMessage
                {
                    MatchId = matchId,
                    OpCode = Convert.ToString(opCode),
                    Presences = presences as List<UserPresence>,
                    State = Convert.ToBase64String(state)
                }
            };
            await SendAsync(envelope).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void SendMatchState(string matchId, long opCode, string state,
            IEnumerable<IUserPresence> presences = null) =>
            SendMatchState(matchId, opCode, Encoding.UTF8.GetBytes(state), presences);

        /// <inheritdoc />
        public async void SendMatchState(string matchId, long opCode, byte[] state,
            IEnumerable<IUserPresence> presences = null)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                MatchStateSend = new MatchSendMessage
                {
                    MatchId = matchId,
                    OpCode = Convert.ToString(opCode),
                    Presences = presences as List<UserPresence>,
                    State = Convert.ToBase64String(state)
                }
            };
            try
            {
                await SendAsync(envelope).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, e);
            }
        }

        /// <inheritdoc />
        public async Task UnfollowUsersAsync(IEnumerable<string> userIds)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                StatusUnfollow = new StatusUnfollowMessage
                {
                    UserIds = new List<string>(userIds)
                }
            };
            await SendAsync(envelope);
        }

        /// <inheritdoc />
        public async Task<IChannelMessageAck>
            UpdateChatMessageAsync(IChannel channel, string messageId, string content) =>
            await UpdateChatMessageAsync(channel.Id, messageId, content);

        /// <inheritdoc />
        public async Task<IChannelMessageAck> UpdateChatMessageAsync(string channelId, string messageId, string content)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                ChannelMessageUpdate = new ChannelUpdateMessage
                {
                    ChannelId = channelId,
                    MessageId = messageId,
                    Content = content
                }
            };
            var response = await SendAsync(envelope).ConfigureAwait(false);
            return response.ChannelMessageAck;
        }

        /// <inheritdoc />
        public async Task UpdateStatusAsync(string status)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                StatusUpdate = new StatusUpdateMessage
                {
                    Status = status
                }
            };
            await SendAsync(envelope);
        }

        /// <inheritdoc />
        public async Task<IChannelMessageAck> WriteChatMessageAsync(IChannel channel, string content) =>
            await WriteChatMessageAsync(channel.Id, content);

        /// <inheritdoc />
        public async Task<IChannelMessageAck> WriteChatMessageAsync(string channelId, string content)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = Guid.NewGuid().ToString(),
                ChannelMessageSend = new ChannelSendMessage
                {
                    ChannelId = channelId,
                    Content = content
                }
            };
            var response = await SendAsync(envelope).ConfigureAwait(false);
            return response.ChannelMessageAck;
        }

        private async Task<WebSocketMessageEnvelope> SendAsync(WebSocketMessageEnvelope message)
        {
            if (_listener == null || !_listener.IsConnected)
            {
                throw new InvalidOperationException("Socket is not connected.");
            }

            _sendBuffer.Enqueue(message.ToJson());
            if (string.IsNullOrEmpty(message.Cid))
            {
                // No response required.
                return null;
            }

            var completer = new TaskCompletionSource<WebSocketMessageEnvelope>();
            _messageReplies[message.Cid] = completer;
            var resultTask = completer.Task;

            var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(TimeoutMs));
            if (await Task.WhenAny(resultTask, timeoutTask).ConfigureAwait(false) == timeoutTask)
            {
                throw new TimeoutException($"Socket send timed out after '{TimeoutMs}' milliseconds.");
            }

            return await resultTask.ConfigureAwait(false);
        }

        private async void ReadSocketAsync(CancellationToken ct)
        {
            try
            {
                OnConnect?.Invoke(this, EventArgs.Empty);

                while (!ct.IsCancellationRequested && _listener.IsConnected)
                {
                    var readStream = await _listener.ReadMessageAsync(ct).ConfigureAwait(false);
                    if (readStream == null)
                    {
                        continue; // NOTE does stream need to be consumed?
                    }

                    using (var reader = new StreamReader(readStream, true))
                    {
                        var message = await reader.ReadToEndAsync().ConfigureAwait(false);
                        if (Trace)
                        {
                            Logger.DebugFormat("Socket read message: '{0}'", message);
                        }

                        var envelope = message.FromJson<WebSocketMessageEnvelope>();
                        ProcessMessageAsync(envelope, message);
                    }
                }

                OnDisconnect?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException e)
            {
                if (Trace) Logger.DebugFormat("Socket operation cancelled: '{0}'", e.Message);
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
                    var message = await _sendBuffer.DequeueAsync(ct).ConfigureAwait(false);
                    using (var output = _listener.CreateMessageWriter(WebSocketMessageType.Text))
                    using (var writer = new StreamWriter(output))
                    {
                        if (Trace)
                        {
                            Logger.DebugFormat("Socket write message: '{0}'", message);
                        }

                        await writer.WriteAsync(message).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                if (Trace) Logger.DebugFormat("Socket operation cancelled: '{0}'", e.Message);
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, e);
            }
        }

#pragma warning disable 1998
        private async void ProcessMessageAsync(WebSocketMessageEnvelope envelope, string message)
#pragma warning restore 1998
        {
            if (!string.IsNullOrEmpty(envelope.Cid))
            {
                // Handle message response.
                TaskCompletionSource<WebSocketMessageEnvelope> completer;
                var cid = envelope.Cid;
                _messageReplies.TryRemove(cid, out completer);
                if (completer == null)
                {
                    if (Trace) Logger.InfoFormat("No task completer for message: '{0}'", cid);
                    return;
                }

                if (envelope.Error != null)
                {
                    // FIXME use a dedicated exception type.
                    completer.SetException(new WebSocketException(envelope.Error.Message));
                }
                else
                {
                    completer.SetResult(envelope);
                }
            }
            else if (envelope.Error != null)
            {
                OnError?.Invoke(this, new WebSocketException(envelope.Error.Message));
            }
            else if (envelope.ChannelMessage != null)
            {
                OnChannelMessage?.Invoke(this, envelope.ChannelMessage);
            }
            else if (envelope.ChannelPresenceEvent != null)
            {
                OnChannelPresence?.Invoke(this, envelope.ChannelPresenceEvent);
            }
            else if (envelope.MatchmakerMatched != null)
            {
                OnMatchmakerMatched?.Invoke(this, envelope.MatchmakerMatched);
            }
            else if (envelope.MatchPresenceEvent != null)
            {
                OnMatchPresence?.Invoke(this, envelope.MatchPresenceEvent);
            }
            else if (envelope.MatchState != null)
            {
                OnMatchState?.Invoke(this, envelope.MatchState);
            }
            else if (envelope.NotificationList != null)
            {
                foreach (var notification in envelope.NotificationList.Notifications)
                {
                    OnNotification?.Invoke(this, notification);
                }
            }
            else if (envelope.StatusPresenceEvent != null)
            {
                OnStatusPresence?.Invoke(this, envelope.StatusPresenceEvent);
            }
            else if (envelope.StreamPresenceEvent != null)
            {
                OnStreamPresence?.Invoke(this, envelope.StreamPresenceEvent);
            }
            else if (envelope.StreamState != null)
            {
                OnStreamState?.Invoke(this, envelope.StreamState);
            }
            else
            {
                if (Trace) Logger.InfoFormat("Socket received unrecognised message: '{0}'", message);
            }
        }
    }
}
