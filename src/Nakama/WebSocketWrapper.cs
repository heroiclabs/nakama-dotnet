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
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using TinyJson;

    /// <summary>
    /// A socket which uses the WebSocket protocol to interact with Nakama server.
    /// </summary>
    internal class WebSocketWrapper : WebSocketEventListener, ISocket
    {
        /// <inheritdoc />
        public event EventHandler<IApiChannelMessage> OnChannelMessage = (sender, message) => { };

        /// <inheritdoc />
        public event EventHandler<IChannelPresenceEvent> OnChannelPresence = (sender, _event) => { };

        /// <inheritdoc />
        public event EventHandler OnConnect = (sender, args) => { };

        /// <inheritdoc />
        public event EventHandler OnDisconnect = (sender, args) => { };

        /// <inheritdoc />
        public event EventHandler<Exception> OnError;

        /// <inheritdoc />
        public event EventHandler<IMatchmakerMatched> OnMatchmakerMatched = (sender, matched) => { };

        /// <inheritdoc />
        public event EventHandler<IMatchState> OnMatchState = (sender, state) => { };

        /// <inheritdoc />
        public event EventHandler<IMatchPresenceEvent> OnMatchPresence = (sender, _event) => { };

        /// <inheritdoc />
        public event EventHandler<IApiNotification> OnNotification = (sender, notification) => { };

        /// <inheritdoc />
        public event EventHandler<IStatusPresenceEvent> OnStatusPresence = (sender, _event) => { };

        /// <inheritdoc />
        public event EventHandler<IStreamPresenceEvent> OnStreamPresence;

        /// <inheritdoc />
        public event EventHandler<IStreamState> OnStreamState;

        private readonly Uri _baseUri;
        private readonly WebSocketOptions _options;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<WebSocketMessageEnvelope>> _messageReplies;

        private bool IsTrace => _options.Logger != null;

        internal WebSocketWrapper(Uri baseUri, ILogger logger, int timeout) : this(baseUri, new WebSocketOptions
        {
            Logger = logger,
            ConnectTimeout = TimeSpan.FromMilliseconds(timeout)
        })
        {
        }

        internal WebSocketWrapper(Uri baseUri, WebSocketOptions options) : base(options)
        {
            _baseUri = baseUri;
            _messageReplies = new ConcurrentDictionary<string, TaskCompletionSource<WebSocketMessageEnvelope>>();
            options.ValidateOptions();
            _options = options.Clone();

            if (!IsTrace) _options.Logger = new NullLogger();

            OnError = (sender, exception) => _options.Logger.Error(exception);

            OnChannelMessage = (sender, message) =>
                _options.Logger.DebugFormat("Received channel message '{0}'", message);
            OnChannelPresence = (sender, _event) =>
                _options.Logger.DebugFormat("Received channel presence '{0}'", _event);
            OnConnect = (sender, args) => _options.Logger.Debug("Socket connected.");
            OnDisconnect = (sender, args) => _options.Logger.Debug("Socket disconnected.");
            OnMatchmakerMatched = (sender, matched) =>
                _options.Logger.DebugFormat("Received matchmaker match '{0}'", matched);
            OnMatchPresence = (sender, _event) =>
                _options.Logger.DebugFormat("Received match presence '{0}'", _event);
            OnMatchState = (sender, state) => _options.Logger.DebugFormat("Received match state '{0}'", state);
            OnNotification = (sender, notification) =>
                _options.Logger.DebugFormat("Received notification '{0}'", notification);
            OnStatusPresence = (sender, _event) =>
                _options.Logger.DebugFormat("Received status presence '{0}'", _event);
            OnStreamPresence = (sender, _event) =>
                _options.Logger.DebugFormat("Received stream presence '{0}'", _event);
            OnStreamState = (sender, state) => _options.Logger.DebugFormat("Received stream state '{0}'", state);

            Connected += (sender, args) => OnConnect.Invoke(this, EventArgs.Empty);
            Disconnected += (sender, args) => OnDisconnect.Invoke(this, EventArgs.Empty);
            ErrorReceived += (sender, exception) => OnError?.Invoke(this, exception);
            MessageReceived += (sender, message) =>
            {
                if (IsTrace)
                {
                    _options.Logger.DebugFormat("Socket read message: '{0}'", message);
                }

                var envelope = message.FromJson<WebSocketMessageEnvelope>();
                if (!string.IsNullOrEmpty(envelope.Cid))
                {
                    // Handle message response.
                    TaskCompletionSource<WebSocketMessageEnvelope> completer;
                    var cid = envelope.Cid;
                    _messageReplies.TryRemove(cid, out completer);
                    if (completer == null)
                    {
                        if (IsTrace) _options.Logger.InfoFormat("No task completer for message: '{0}'", cid);
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
                    if (IsTrace)
                    {
                        _options.Logger.InfoFormat("Socket received unrecognised message: '{0}'", message);
                    }
                }
            };
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
            var addr = new UriBuilder(_baseUri)
            {
                Path = "/ws",
                Query = string.Concat("lang=en&status=", appearOnline, "&token=", session.AuthToken)
            };            
            await base.ConnectAsync(addr.Uri, ct);
        }

        /// <inheritdoc />
        public async Task DisconnectAsync(bool dispatch = true)
        {
            await CloseAsync();

            if (dispatch)
            {
                OnDisconnect?.Invoke(this, EventArgs.Empty);
            }
            _messageReplies.Clear();
        }

        public new void Dispose()
        {
            base.Dispose();
            _messageReplies.Clear();
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
            if (!IsConnected)
            {
                throw new InvalidOperationException("Socket is not connected.");
            }

            Send(message.ToJson());
            if (string.IsNullOrEmpty(message.Cid))
            {
                // No response required.
                return null;
            }

            var completer = new TaskCompletionSource<WebSocketMessageEnvelope>();
            _messageReplies[message.Cid] = completer;
            var resultTask = completer.Task;

            var timeoutTask = Task.Delay(_options.ConnectTimeout);
            if (await Task.WhenAny(resultTask, timeoutTask).ConfigureAwait(false) == timeoutTask)
            {
                throw new TimeoutException(string.Format("Socket send timed out after {0} time.", _options.ConnectTimeout.ToString()));
            }

            return await resultTask.ConfigureAwait(false);
        }
    }
}
