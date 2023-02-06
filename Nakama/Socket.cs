// Copyright 2019 The Nakama Authors
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
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Nakama.TinyJson;

namespace Nakama
{
    /// <summary>
    /// A socket which implements the Nakama realtime API.
    /// </summary>
    public class Socket : ISocket
    {
        private int _cid; // callback id.

        /// <summary>
        /// The default timeout for when the socket connects.
        /// </summary>
        public const int DefaultConnectTimeout = 30;

        /// <summary>
        /// The default timeout for when the socket sends a message.
        /// </summary>
        public const int DefaultSendTimeout = 10;

        /// <inheritdoc cref="Closed"/>
        public event Action Closed;

        /// <inheritdoc cref="Connected"/>
        public event Action Connected;

        /// <inheritdoc cref="ReceivedChannelMessage"/>
        public event Action<IApiChannelMessage> ReceivedChannelMessage;

        /// <inheritdoc cref="ReceivedChannelPresence"/>
        public event Action<IChannelPresenceEvent> ReceivedChannelPresence;

        /// <inheritdoc cref="ReceivedError"/>
        public event Action<Exception> ReceivedError;

        /// <inheritdoc cref="ReceivedMatchmakerMatched"/>
        public event Action<IMatchmakerMatched> ReceivedMatchmakerMatched;

        /// <inheritdoc cref="ReceivedMatchState"/>
        public event Action<IMatchState> ReceivedMatchState;

        /// <inheritdoc cref="ReceivedMatchPresence"/>
        public event Action<IMatchPresenceEvent> ReceivedMatchPresence;

        /// <inheritdoc cref="ReceivedNotification"/>
        public event Action<IApiNotification> ReceivedNotification;

        /// <inheritdoc cref="ReceivedStatusPresence"/>
        public event Action<IStatusPresenceEvent> ReceivedStatusPresence;

        /// <inheritdoc cref="ReceivedStreamPresence"/>
        public event Action<IStreamPresenceEvent> ReceivedStreamPresence;

        /// <inheritdoc cref="ReceivedStreamState"/>
        public event Action<IStreamState> ReceivedStreamState;

        /// <inheritdoc cref="ReceivedParty"/>
        public event Action<IParty> ReceivedParty;

        /// <inheritdoc cref="ReceivedPartyClose"/>
        public event Action<IPartyClose> ReceivedPartyClose;

        /// <inheritdoc cref="ReceivedPartyData"/>
        public event Action<IPartyData> ReceivedPartyData;

        /// <inheritdoc cref="ReceivedPartyJoinRequest"/>
        public event Action<IPartyJoinRequest> ReceivedPartyJoinRequest;

        /// <inheritdoc cref="ReceivedPartyLeader"/>
        public event Action<IPartyLeader> ReceivedPartyLeader;

        /// <inheritdoc cref="ReceivedPartyPresence"/>
        public event Action<IPartyPresenceEvent> ReceivedPartyPresence;

        /// <inheritdoc cref="ReceivedPartyMatchmakerTicket"/>
        public event Action<IPartyMatchmakerTicket> ReceivedPartyMatchmakerTicket;

        /// <inheritdoc cref="IsConnected"/>
        public bool IsConnected => _adapter.IsConnected;

        /// <inheritdoc cref="IsConnecting"/>
        public bool IsConnecting => _adapter.IsConnecting;

        /// <summary>
        /// The logger to use with the socket.
        /// </summary>
        public ILogger Logger { get; set; }

        private readonly ISocketAdapter _adapter;
        private readonly Uri _baseUri;
        private readonly Dictionary<string, TaskCompletionSource<WebSocketMessageEnvelope>> _responses;
        private readonly TimeSpan _sendTimeoutSec;

        private readonly object _responsesLock = new object();

        /// <summary>
        /// A new socket with default options.
        /// </summary>
        public Socket() : this(Client.DefaultScheme, Client.DefaultHost, Client.DefaultPort, new WebSocketAdapter())
        {
        }

        /// <summary>
        /// A new socket with an adapter.
        /// </summary>
        /// <param name="adapter">The adapter for use with the socket.</param>
        public Socket(ISocketAdapter adapter) : this(Client.DefaultScheme, Client.DefaultHost, Client.DefaultPort,
            adapter)
        {
        }

        /// <summary>
        /// A new socket with server connection and adapter options.
        /// </summary>
        /// <param name="scheme">The protocol scheme. Must be "ws" or "wss".</param>
        /// <param name="host">The host address of the server.</param>
        /// <param name="port">The port number of the server.</param>
        /// <param name="adapter">The adapter for use with the socket.</param>
        /// <param name="sendTimeoutSec">The maximum time allowed for a message to be sent.</param>
        public Socket(string scheme, string host, int port, ISocketAdapter adapter, int sendTimeoutSec = DefaultSendTimeout)
        {
            Logger = NullLogger.Instance;
            _adapter = adapter;
            _baseUri = new UriBuilder(scheme, host, port).Uri;
            _responses = new Dictionary<string, TaskCompletionSource<WebSocketMessageEnvelope>>();
            _sendTimeoutSec = TimeSpan.FromSeconds(sendTimeoutSec);

            _adapter.Connected += () => Connected?.Invoke();
            _adapter.Closed += () =>
            {
                lock (_responsesLock)
                {
                    foreach (var response in _responses)
                    {
                        response.Value.TrySetCanceled();
                    }

                    _responses.Clear();
                }

                Closed?.Invoke();
            };
            _adapter.ReceivedError += e =>
            {
                if (!_adapter.IsConnected)
                {
                    lock (_responsesLock)
                    {
                        foreach (var response in _responses)
                        {
                            response.Value.TrySetCanceled();
                        }

                        _responses.Clear();
                    }
                }

                ReceivedError?.Invoke(e);
            };

            _adapter.Received += ProcessMessage;
        }

        /// <inheritdoc cref="AcceptPartyMemberAsync"/>
        public Task AcceptPartyMemberAsync(string partyId, IUserPresence presence)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                PartyAccept = new PartyAccept
                {
                    PartyId = partyId,
                    Presence = presence as UserPresence // TODO serialize interface directly in protobuf
                }
            };

            return SendAsync(envelope);
        }

        /// <inheritdoc cref="AddMatchmakerAsync"/>
        public async Task<IMatchmakerTicket> AddMatchmakerAsync(string query = "*", int minCount = 2, int maxCount = 8,
            Dictionary<string, string> stringProperties = null, Dictionary<string, double> numericProperties = null, int? countMultiple = null)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                MatchmakerAdd = new MatchmakerAddMessage
                {
                    Query = query,
                    MinCount = minCount,
                    MaxCount = maxCount,
                    StringProperties = stringProperties,
                    NumericProperties = numericProperties,
                    CountMultiple = countMultiple
                }
            };

            var response = await SendAsync(envelope);
            return response.MatchmakerTicket;
        }

        /// <inheritdoc cref="AddMatchmakerPartyAsync"/>
        public async Task<IPartyMatchmakerTicket> AddMatchmakerPartyAsync(string partyId, string query, int minCount,
            int maxCount, Dictionary<string, string> stringProperties = null,
            Dictionary<string, double> numericProperties = null, int? countMultiple = null)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                PartyMatchmakerAdd = new PartyMatchmakerAdd
                {
                    PartyId = partyId,
                    Query = query,
                    MinCount = minCount,
                    MaxCount = maxCount,
                    StringProperties = stringProperties,
                    NumericProperties = numericProperties,
                    CountMultiple = countMultiple
                }
            };

            var response = await SendAsync(envelope);
            return response.PartyMatchmakerTicket;
        }

        /// <inheritdoc cref="CloseAsync"/>
        public Task CloseAsync() => _adapter.CloseAsync();

        /// <inheritdoc cref="ConnectAsync"/>
        public Task ConnectAsync(ISession session, bool appearOnline = false,
            int connectTimeoutSec = DefaultConnectTimeout, string langTag = "en")
        {
            var uri = new UriBuilder(_baseUri)
            {
                Path = "/ws",
                Query = $"lang={langTag}&status={appearOnline}&token={session.AuthToken}"
            }.Uri;
            return _adapter.ConnectAsync(uri, connectTimeoutSec);
        }

        /// <inheritdoc cref="ClosePartyAsync"/>
        public Task ClosePartyAsync(string partyId)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                PartyClose = new PartyClose { PartyId = partyId }
            };

            return SendAsync(envelope);
        }

        /// <inheritdoc cref="CreateMatchAsync"/>
        public async Task<IMatch> CreateMatchAsync(string name = null)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                MatchCreate = new MatchCreateMessage { Name = name }
            };

            var response = await SendAsync(envelope);
            return response.Match;
        }

        /// <inheritdoc cref="CreatePartyAsync"/>
        public async Task<IParty> CreatePartyAsync(bool open, int maxSize)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                PartyCreate = new PartyCreate
                {
                    Open = open,
                    MaxSize = maxSize
                }
            };

            var response = await SendAsync(envelope);
            return response.Party;
        }

        /// <inheritdoc cref="FollowUsersAsync(System.Collections.Generic.IEnumerable{Nakama.IApiUser})"/>
        public Task<IStatus> FollowUsersAsync(IEnumerable<IApiUser> users) =>
            FollowUsersAsync(users.Select(user => user.Id));

        /// <inheritdoc cref="FollowUsersAsync(System.Collections.Generic.IEnumerable{string},System.Collections.Generic.IEnumerable{string})"/>
        public async Task<IStatus> FollowUsersAsync(IEnumerable<string> userIDs, IEnumerable<string> usernames = null)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                StatusFollow = new StatusFollowMessage
                {
                    UserIds = new List<string>(userIDs),
                    Usernames = usernames != null ? new List<string>(usernames) : new List<string>()
                }
            };

            var response = await SendAsync(envelope);
            return response.Status;
        }

        /// <inheritdoc cref="JoinChatAsync"/>
        public async Task<IChannel> JoinChatAsync(string target, ChannelType type, bool persistence = false,
            bool hidden = false)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                ChannelJoin = new ChannelJoinMessage
                {
                    Hidden = hidden,
                    Persistence = persistence,
                    Target = target,
                    Type = (int)type
                }
            };

            var response = await SendAsync(envelope);
            return response.Channel;
        }

        /// <inheritdoc cref="JoinMatchAsync(Nakama.IMatchmakerMatched)"/>
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

            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                MatchJoin = message
            };

            var response = await SendAsync(envelope);
            return response.Match;
        }

        /// <inheritdoc cref="JoinMatchAsync(string,IDictionary{string, string})"/>
        public async Task<IMatch> JoinMatchAsync(string matchId, IDictionary<string, string> metadata = null)
        {
            int cid = Interlocked.Increment(ref _cid);

            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                MatchJoin = new MatchJoinMessage
                {
                    MatchId = matchId,
                    Metadata = metadata
                }
            };

            var response = await SendAsync(envelope);
            return response.Match;
        }

        /// <inheritdoc cref="JoinPartyAsync"/>
        public Task JoinPartyAsync(string partyId)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                PartyJoin = new PartyJoin
                {
                    PartyId = partyId
                }
            };

            return SendAsync(envelope);
        }

        /// <inheritdoc cref="LeaveChatAsync(Nakama.IChannel)"/>
        public Task LeaveChatAsync(IChannel channel) => LeaveChatAsync(channel.Id);

        /// <inheritdoc cref="LeaveChatAsync(string)"/>
        public Task LeaveChatAsync(string channelId)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                ChannelLeave = new ChannelLeaveMessage
                {
                    ChannelId = channelId
                }
            };

            return SendAsync(envelope);
        }

        /// <inheritdoc cref="LeaveMatchAsync(Nakama.IMatch)"/>
        public Task LeaveMatchAsync(IMatch match) => LeaveMatchAsync(match.Id);

        /// <inheritdoc cref="LeaveMatchAsync(string)"/>
        public Task LeaveMatchAsync(string matchId)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                MatchLeave = new MatchLeaveMessage
                {
                    MatchId = matchId
                }
            };

            return SendAsync(envelope);
        }

        /// <inheritdoc cref="LeavePartyAsync"/>
        public Task LeavePartyAsync(string partyId)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                PartyLeave = new PartyLeave
                {
                    PartyId = partyId
                }
            };

            return SendAsync(envelope);
        }

        /// <inheritdoc cref="ListPartyJoinRequestsAsync"/>
        public async Task<IPartyJoinRequest> ListPartyJoinRequestsAsync(string partyId)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                PartyJoinRequestList = new PartyJoinRequestList
                {
                    PartyId = partyId,
                }
            };

            var response = await SendAsync(envelope);
            return response.PartyJoinRequest;
        }

        /// <inheritdoc cref="PromotePartyMemberAsync"/>
        public Task PromotePartyMemberAsync(string partyId, IUserPresence partyMember)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                PartyPromote = new PartyPromote
                {
                    PartyId = partyId,
                    Presence = partyMember as UserPresence // TODO serialize interface directly in protobuf
                }
            };


            return SendAsync(envelope);
        }

        /// <inheritdoc cref="RemoveChatMessageAsync(Nakama.IChannel,string)"/>
        public Task<IChannelMessageAck> RemoveChatMessageAsync(IChannel channel, string messageId) =>
            RemoveChatMessageAsync(channel.Id, messageId);

        /// <inheritdoc cref="RemoveChatMessageAsync(string,string)"/>
        public async Task<IChannelMessageAck> RemoveChatMessageAsync(string channelId, string messageId)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                ChannelMessageRemove = new ChannelRemoveMessage
                {
                    ChannelId = channelId,
                    MessageId = messageId
                }
            };

            var response = await SendAsync(envelope);
            return response.ChannelMessageAck;
        }

        /// <inheritdoc cref="RemoveMatchmakerAsync(Nakama.IMatchmakerTicket)"/>
        public Task RemoveMatchmakerAsync(IMatchmakerTicket ticket) => RemoveMatchmakerAsync(ticket.Ticket);

        /// <inheritdoc cref="RemoveMatchmakerAsync(string)"/>
        public Task RemoveMatchmakerAsync(string ticket)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                MatchmakerRemove = new MatchmakerRemoveMessage
                {
                    Ticket = ticket
                }
            };

            return SendAsync(envelope);
        }

        /// <inheritdoc cref="RemoveMatchmakerPartyAsync"/>
        public Task RemoveMatchmakerPartyAsync(string partyId, string ticket)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                PartyMatchmakerRemove = new PartyMatchmakerRemove
                {
                    PartyId = partyId,
                    Ticket = ticket
                }
            };

            return SendAsync(envelope);
        }

        /// <inheritdoc cref="RemovePartyMemberAsync"/>
        public Task RemovePartyMemberAsync(string partyId, IUserPresence presence)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                PartyMemberRemove = new PartyMemberRemove
                {
                    PartyId = partyId,
                    Presence = presence as UserPresence
                }
            };

            return SendAsync(envelope);
        }

        /// <inheritdoc cref="RpcAsync(string,string)"/>
        public async Task<IApiRpc> RpcAsync(string funcId, string payload = null)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                Rpc = new ApiRpc
                {
                    Id = funcId,
                    Payload = payload
                }
            };

            var response = await SendAsync(envelope);
            return response.Rpc;
        }

        /// <inheritdoc cref="RpcAsync(string,ArraySegment{byte})"/>
        public async Task<IApiRpc> RpcAsync(string funcId, ArraySegment<byte> payload)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                Rpc = new ApiRpc
                {
                    Id = funcId,
                    Payload = Convert.ToBase64String(payload.Array, payload.Offset, payload.Count)
                }
            };

            var response = await SendAsync(envelope);
            return response.Rpc;
        }

        /// <inheritdoc cref="SendMatchStateAsync(string,long,ArraySegment{byte},System.Collections.Generic.IEnumerable{Nakama.IUserPresence})"/>
        public Task SendMatchStateAsync(string matchId, long opCode, ArraySegment<byte> state,
            IEnumerable<IUserPresence> presences = null)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                MatchStateSend = new MatchSendMessage
                {
                    MatchId = matchId,
                    OpCode = Convert.ToString(opCode),
                    Presences = BuildPresenceList(presences),
                    State = Convert.ToBase64String(state.Array, state.Offset, state.Count)
                }
            };
            return SendAsync(envelope);
        }

        /// <inheritdoc cref="SendMatchStateAsync(string,long,string,System.Collections.Generic.IEnumerable{Nakama.IUserPresence})"/>
        public Task SendMatchStateAsync(string matchId, long opCode, string state,
            IEnumerable<IUserPresence> presences = null) => SendMatchStateAsync(matchId, opCode,
            System.Text.Encoding.UTF8.GetBytes(state), presences);

        /// <inheritdoc cref="SendMatchStateAsync(string,long,byte[],System.Collections.Generic.IEnumerable{Nakama.IUserPresence})"/>
        public Task SendMatchStateAsync(string matchId, long opCode, byte[] state,
            IEnumerable<IUserPresence> presences = null)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                MatchStateSend = new MatchSendMessage
                {
                    MatchId = matchId,
                    OpCode = Convert.ToString(opCode),
                    Presences = BuildPresenceList(presences),
                    State = Convert.ToBase64String(state)
                }
            };
            return SendAsync(envelope);
        }

        /// <inheritdoc cref="SendPartyDataAsync(string,long,ArraySegment{byte})"/>
        public Task SendPartyDataAsync(string partyId, long opCode, ArraySegment<byte> data)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                PartyDataSend = new PartyDataSend
                {
                    PartyId = partyId,
                    OpCode = Convert.ToString(opCode),
                    Data = Convert.ToBase64String(data.Array, data.Offset, data.Count)
                }
            };
            return SendAsync(envelope);
        }

        /// <inheritdoc cref="SendPartyDataAsync(string,long,string)"/>
        public Task SendPartyDataAsync(string partyId, long opCode, string data) =>
            SendPartyDataAsync(partyId, opCode, System.Text.Encoding.UTF8.GetBytes(data));

        /// <inheritdoc cref="SendPartyDataAsync(string,long,byte[])"/>
        public Task SendPartyDataAsync(string partyId, long opCode, byte[] data)
        {
            var envelope = new WebSocketMessageEnvelope
            {
                PartyDataSend = new PartyDataSend
                {
                    PartyId = partyId,
                    OpCode = Convert.ToString(opCode),
                    Data = Convert.ToBase64String(data)
                }
            };

            return SendAsync(envelope);
        }

        public override string ToString()
        {
            return
                $"Socket(_baseUri='{_baseUri}', _cid={_cid}, IsConnected={IsConnected}, IsConnecting={IsConnecting})";
        }

        /// <inheritdoc cref="UnfollowUsersAsync(System.Collections.Generic.IEnumerable{Nakama.IApiUser})"/>
        public Task UnfollowUsersAsync(IEnumerable<IApiUser> users) =>
            UnfollowUsersAsync(users.Select(user => user.Id));

        /// <inheritdoc cref="UnfollowUsersAsync(System.Collections.Generic.IEnumerable{string})"/>
        public Task UnfollowUsersAsync(IEnumerable<string> userIDs)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                StatusUnfollow = new StatusUnfollowMessage
                {
                    UserIds = new List<string>(userIDs)
                }
            };

            return SendAsync(envelope);
        }

        /// <inheritdoc cref="UpdateChatMessageAsync(Nakama.IChannel,string,string)"/>
        public Task<IChannelMessageAck> UpdateChatMessageAsync(IChannel channel, string messageId, string content) =>
            UpdateChatMessageAsync(channel.Id, messageId, content);

        /// <inheritdoc cref="UpdateChatMessageAsync(string,string,string)"/>
        public async Task<IChannelMessageAck> UpdateChatMessageAsync(string channelId, string messageId, string content)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                ChannelMessageUpdate = new ChannelUpdateMessage
                {
                    ChannelId = channelId,
                    MessageId = messageId,
                    Content = content
                }
            };

            var response = await SendAsync(envelope);
            return response.ChannelMessageAck;
        }

        /// <inheritdoc cref="UpdateStatusAsync"/>
        public Task UpdateStatusAsync(string status)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                StatusUpdate = new StatusUpdateMessage
                {
                    Status = status
                }
            };

            return SendAsync(envelope);
        }

        /// <inheritdoc cref="WriteChatMessageAsync(Nakama.IChannel,string)"/>
        public Task<IChannelMessageAck> WriteChatMessageAsync(IChannel channel, string content) =>
            WriteChatMessageAsync(channel.Id, content);

        /// <inheritdoc cref="WriteChatMessageAsync(string,string)"/>
        public async Task<IChannelMessageAck> WriteChatMessageAsync(string channelId, string content)
        {
            int cid = Interlocked.Increment(ref _cid);
            var envelope = new WebSocketMessageEnvelope
            {
                Cid = $"{cid}",
                ChannelMessageSend = new ChannelSendMessage
                {
                    ChannelId = channelId,
                    Content = content
                }
            };

            var response = await SendAsync(envelope);
            return response.ChannelMessageAck;
        }

        /// <summary>
        /// Build a socket from a client object.
        /// </summary>
        /// <param name="client">A client object.</param>
        /// <returns>A new socket with the connection settings from the client.</returns>
        public static ISocket From(IClient client) => From(client, new WebSocketAdapter());

        /// <summary>
        /// Build a socket from a client object and socket adapter.
        /// </summary>
        /// <param name="client">A client object.</param>
        /// <param name="adapter">The socket adapter to use with the connection.</param>
        /// <returns>A new socket with connection settings from the client.</returns>
        public static ISocket From(IClient client, ISocketAdapter adapter)
        {
            var scheme = client.Scheme.ToLower().Equals("http") ? "ws" : "wss";
            return new Socket(scheme, client.Host, client.Port, adapter) { Logger = client.Logger };
        }

        private void ProcessMessage(ArraySegment<byte> buffer)
        {
            var contents = System.Text.Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);

            Logger?.DebugFormat("Received JSON over web socket: {0}", contents);

            var envelope = contents.FromJson<WebSocketMessageEnvelope>();
            try
            {
                if (!string.IsNullOrEmpty(envelope.Cid))
                {
                    lock (_responsesLock)
                    {
                        // Handle message response.
                        if (_responses.ContainsKey(envelope.Cid))
                        {
                            var completer = _responses[envelope.Cid];
                            _responses.Remove(envelope.Cid);

                            if (envelope.Error != null)
                            {
                                completer.SetException(new WebSocketException(WebSocketError.InvalidState,
                                    envelope.Error.Message));
                            }
                            else
                            {
                                completer.SetResult(envelope);
                            }
                        }
                        else
                        {
                            // it is valid for this to occur if a completer timed out and was
                            // removed from the responses dictionary after the timeout.
                            Logger?.WarnFormat("No completer for message cid: {0}", envelope.Cid);
                        }
                    }
                }
                else if (envelope.Error != null)
                {
                    ReceivedError?.Invoke(new WebSocketException(WebSocketError.InvalidState, envelope.Error.Message));
                }
                else if (envelope.ChannelMessage != null)
                {
                    ReceivedChannelMessage?.Invoke(envelope.ChannelMessage);
                }
                else if (envelope.ChannelPresenceEvent != null)
                {
                    ReceivedChannelPresence?.Invoke(envelope.ChannelPresenceEvent);
                }
                else if (envelope.MatchmakerMatched != null)
                {
                    ReceivedMatchmakerMatched?.Invoke(envelope.MatchmakerMatched);
                }
                else if (envelope.MatchPresenceEvent != null)
                {
                    ReceivedMatchPresence?.Invoke(envelope.MatchPresenceEvent);
                }
                else if (envelope.MatchState != null)
                {
                    ReceivedMatchState?.Invoke(envelope.MatchState);
                }
                else if (envelope.NotificationList != null)
                {
                    foreach (var notification in envelope.NotificationList.Notifications)
                    {
                        ReceivedNotification?.Invoke(notification);
                    }
                }
                else if (envelope.StatusPresenceEvent != null)
                {
                    ReceivedStatusPresence?.Invoke(envelope.StatusPresenceEvent);
                }
                else if (envelope.StreamPresenceEvent != null)
                {
                    ReceivedStreamPresence?.Invoke(envelope.StreamPresenceEvent);
                }
                else if (envelope.StreamState != null)
                {
                    ReceivedStreamState?.Invoke(envelope.StreamState);
                }
                else if (envelope.Party != null)
                {
                    ReceivedParty?.Invoke(envelope.Party);
                }
                else if (envelope.PartyClose != null)
                {
                    ReceivedPartyClose?.Invoke(envelope.PartyClose);
                }
                else if (envelope.PartyData != null)
                {
                    ReceivedPartyData?.Invoke(envelope.PartyData);
                }
                else if (envelope.PartyJoinRequest != null)
                {
                    ReceivedPartyJoinRequest?.Invoke(envelope.PartyJoinRequest);
                }
                else if (envelope.PartyLeader != null)
                {
                    ReceivedPartyLeader?.Invoke(envelope.PartyLeader);
                }
                else if (envelope.PartyMatchmakerTicket != null)
                {
                    ReceivedPartyMatchmakerTicket?.Invoke(envelope.PartyMatchmakerTicket);
                }
                else if (envelope.PartyPresenceEvent != null)
                {
                    ReceivedPartyPresence?.Invoke(envelope.PartyPresenceEvent);
                }
                else
                {
                    Logger?.ErrorFormat("Received unrecognised message: '{0}'", contents);
                }
            }
            catch (Exception e)
            {
                ReceivedError?.Invoke(e);
            }
        }

        private async Task<WebSocketMessageEnvelope> SendAsync(WebSocketMessageEnvelope envelope)
        {
            var json = envelope.ToJson();

            Logger?.DebugFormat("Sending JSON over web socket: {0}", json);

            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            var cts = new CancellationTokenSource(_sendTimeoutSec);
            if (string.IsNullOrEmpty(envelope.Cid))
            {
                await _adapter.SendAsync(new ArraySegment<byte>(buffer), true, cts.Token);
                return null; // No response required.
            }

            var completer = new TaskCompletionSource<WebSocketMessageEnvelope>();
            lock (_responsesLock)
            {
                _responses[envelope.Cid] = completer;
            }
            cts.Token.Register(() => {
                lock (_responsesLock)
                {
                    if (_responses.ContainsKey(envelope.Cid))
                    {
                        _responses.Remove(envelope.Cid);
                    }
                }

                completer.TrySetCanceled();
            });

            await _adapter.SendAsync(new ArraySegment<byte>(buffer), true, cts.Token);
            return await completer.Task;
        }

        private static List<UserPresence> BuildPresenceList(IEnumerable<IUserPresence> presences)
        {
            if (presences == null)
            {
                return (List<UserPresence>)UserPresence.NoPresences;
            }

            var presenceList = presences as List<UserPresence>;
            if (presenceList != null)
            {
                return presenceList;
            }

            presenceList = new List<UserPresence>();
            foreach (var userPresence in presences)
            {
                var concretePresence = (UserPresence)userPresence;
                presenceList.Add(concretePresence);
            }

            return presenceList;
        }
    }
}