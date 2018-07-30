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
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A socket to interact with Nakama server.
    /// </summary>
    public interface ISocket : IDisposable
    {
        /// <summary>
        /// Receive chat channel messages.
        /// </summary>
        event EventHandler<IApiChannelMessage> OnChannelMessage;

        /// <summary>
        /// Receive chat channel presences for when users join and leave.
        /// </summary>
        event EventHandler<IChannelPresenceEvent> OnChannelPresence;

        /// <summary>
        /// Receive an event when the socket connects.
        /// </summary>
        event EventHandler OnConnect;

        /// <summary>
        /// Receive an event when the socket disconnects.
        /// </summary>
        event EventHandler OnDisconnect;

        /// <summary>
        /// Receive an event when the socket has an error.
        /// </summary>
        event EventHandler<Exception> OnError;

        /// <summary>
        /// Receive an event when the player gets matched by the matchmaker.
        /// </summary>
        event EventHandler<IMatchmakerMatched> OnMatchmakerMatched;

        /// <summary>
        /// Receive state messages from a realtime match.
        /// </summary>
        event EventHandler<IMatchState> OnMatchState;

        /// <summary>
        /// Receive match presences for when users join and leave.
        /// </summary>
        event EventHandler<IMatchPresenceEvent> OnMatchPresence;

        /// <summary>
        /// Receive realtime notifications.
        /// </summary>
        event EventHandler<IApiNotification> OnNotification;

        /// <summary>
        /// Receive presence events for when a user updates their status.
        /// </summary>
        event EventHandler<IStatusPresenceEvent> OnStatusPresence;

        /// <summary>
        /// Receive low level presence events from a realtime stream.
        /// </summary>
        event EventHandler<IStreamPresenceEvent> OnStreamPresence;

        /// <summary>
        /// Receive state messages from a realtime stream.
        /// </summary>
        event EventHandler<IStreamState> OnStreamState;

        /// <summary>
        /// Join the matchmaker pool and search for opponents on the server.
        /// </summary>
        /// <param name="query">A matchmaker query to search for opponents.</param>
        /// <param name="minCount">The minimum number of players to compete against.</param>
        /// <param name="maxCount">The maximum number of players to compete against.</param>
        /// <param name="stringProperties">A set of k/v properties to provide in searches.</param>
        /// <param name="numericProperties">A set of k/v numeric properties to provide in searches.</param>
        /// <returns>A task which resolves to a matchmaker ticket object.</returns>
        Task<IMatchmakerTicket> AddMatchmakerAsync(string query = "*", int minCount = 2, int maxCount = 8,
            Dictionary<string, string> stringProperties = null, Dictionary<string, double> numericProperties = null);

        /// <summary>
        /// Connect to the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="ct">A cancellation token for the asynchronous operation.</param>
        /// <param name="appearOnline">True if the socket should show the user as online to others.</param>
        /// <param name="connectTimeout">Time in millisecs before the connection attempt is considered failed.</param>
        /// <returns>A task.</returns>
        Task ConnectAsync(ISession session, CancellationToken ct = default(CancellationToken),
            bool appearOnline = false, int connectTimeout = 5000);

        /// <summary>
        /// Create a multiplayer match on the server.
        /// </summary>
        /// <returns>A task.</returns>
        Task<IMatch> CreateMatchAsync();

        /// <summary>
        /// Close the connection with the server.
        /// </summary>
        /// <param name="dispatch">True if the disconnect should dispatch an on disconnect event.</param>
        /// <returns>A close task.</returns>
        Task DisconnectAsync(bool dispatch = true);

        /// <summary>
        /// Follow one or more users for status updates.
        /// </summary>
        /// <param name="userIds">The user Ids to follow.</param>
        /// <returns>A task.</returns>
        Task<IStatus> FollowUsersAsync(IEnumerable<string> userIds);

        /// <summary>
        /// Join a chat channel on the server.
        /// </summary>
        /// <param name="target">The target channel to join.</param>
        /// <param name="type">The type of channel to join.</param>
        /// <param name="persistence">True if chat messages should be stored.</param>
        /// <param name="hidden">True if the user should be hidden on the channel.</param>
        /// <returns>A task which resolves to a Channel response.</returns>
        Task<IChannel> JoinChatAsync(string target, ChannelType type, bool persistence = false, bool hidden = false);

        /// <summary>
        /// Join a multiplayer match with a matchmaker.
        /// </summary>
        /// <param name="matched">A matchmaker result object.</param>
        /// <returns>A task which resolves to the match joined.</returns>
        Task<IMatch> JoinMatchAsync(IMatchmakerMatched matched);

        /// <summary>
        /// Join a multiplayer match by ID.
        /// </summary>
        /// <param name="matchId">A match ID.</param>
        /// <returns>A task which resolves to the match joined.</returns>
        Task<IMatch> JoinMatchAsync(string matchId);

        /// <summary>
        /// Leave a chat channel on the server.
        /// </summary>
        /// <param name="channel">The channel to leave.</param>
        /// <returns>A task.</returns>
        Task LeaveChatAsync(IChannel channel);

        /// <summary>
        /// Leave a chat channel on the server.
        /// </summary>
        /// <param name="channelId">The ID of the channel to leave.</param>
        /// <returns>A task.</returns>
        Task LeaveChatAsync(string channelId);

        /// <summary>
        /// Leave a match on the server.
        /// </summary>
        /// <param name="match">The match to leave.</param>
        /// <returns>A task.</returns>
        Task LeaveMatchAsync(IMatch match);

        /// <summary>
        /// Leave a match on the server.
        /// </summary>
        /// <param name="matchId">The ID of the match to leave.</param>
        /// <returns>A task.</returns>
        Task LeaveMatchAsync(string matchId);

        /// <summary>
        /// Remove a chat message from a channel on the server.
        /// </summary>
        /// <param name="channel">The chat channel with the message.</param>
        /// <param name="messageId">The ID of a chat message to update.</param>
        /// <returns>A task.</returns>
        Task<IChannelMessageAck> RemoveChatMessageAsync(IChannel channel, string messageId);

        /// <summary>
        /// Remove a chat message from a channel on the server.
        /// </summary>
        /// <param name="channelId">The ID of the chat channel with the message.</param>
        /// <param name="messageId">The ID of a chat message to update.</param>
        /// <returns>A task.</returns>
        Task<IChannelMessageAck> RemoveChatMessageAsync(string channelId, string messageId);

        /// <summary>
        /// Leave the matchmaker pool by ticket.
        /// </summary>
        /// <param name="ticket">The ticket returned by the matchmaker on join.</param>
        /// <returns>A task.</returns>
        Task RemoveMatchmakerAsync(IMatchmakerTicket ticket);

        /// <summary>
        /// Leave the matchmaker pool by ticket.
        /// </summary>
        /// <param name="ticket">The ticket returned by the matchmaker on join. See <c>IMatchmakerTicket.Ticket</c>.</param>
        /// <returns>A task.</returns>
        Task RemoveMatchmakerAsync(string ticket);

        /// <summary>
        /// Send an RPC message to the server.
        /// </summary>
        /// <param name="id">The ID of the function to execute.</param>
        /// <param name="payload">The string content to send to the server.</param>
        /// <returns>A task which resolves to an RPC response.</returns>
        Task<IApiRpc> RpcAsync(string id, string payload);

        /// <summary>
        /// Send new state to a match on the server.
        /// </summary>
        /// <param name="matchId">The Id of the match.</param>
        /// <param name="opCode">An operation code for the match state.</param>
        /// <param name="state">The new state to send to the match.</param>
        /// <param name="presences">The presences in the match to send the state.</param>
        Task SendMatchStateAsync(string matchId, long opCode, string state, IEnumerable<IUserPresence> presences = null);

        /// <summary>
        /// Send a state change to a match on the server.
        /// </summary>
        /// <remarks>
        /// When no presences are supplied the new match state will be sent to all presences.
        /// </remarks>
        /// <param name="matchId">The Id of the match.</param>
        /// <param name="opCode">An operation code for the match state.</param>
        /// <param name="state">The new state to send to the match.</param>
        /// <param name="presences">The presences in the match to send the state.</param>
        Task SendMatchStateAsync(string matchId, long opCode, byte[] state, IEnumerable<IUserPresence> presences = null);

        /// <summary>
        /// Send new state to a match on the server.
        /// </summary>
        /// <param name="matchId">The Id of the match.</param>
        /// <param name="opCode">An operation code for the match state.</param>
        /// <param name="state">The new state to send to the match.</param>
        /// <param name="presences">The presences in the match to send the state.</param>
        void SendMatchState(string matchId, long opCode, string state, IEnumerable<IUserPresence> presences = null);

        /// <summary>
        /// Send a state change to a match on the server.
        /// </summary>
        /// <remarks>
        /// When no presences are supplied the new match state will be sent to all presences.
        /// </remarks>
        /// <param name="matchId">The Id of the match.</param>
        /// <param name="opCode">An operation code for the match state.</param>
        /// <param name="state">The new state to send to the match.</param>
        /// <param name="presences">The presences in the match to send the state.</param>
        void SendMatchState(string matchId, long opCode, byte[] state, IEnumerable<IUserPresence> presences = null);

        /// <summary>
        /// Unfollow status updates for one or more users.
        /// </summary>
        /// <param name="userIds">The ids of users to unfollow.</param>
        /// <returns>A task.</returns>
        Task UnfollowUsersAsync(IEnumerable<string> userIds);

        /// <summary>
        /// Update a chat message to a channel on the server.
        /// </summary>
        /// <param name="channel">The channel with the message to update.</param>
        /// <param name="messageId">The ID of the message to update.</param>
        /// <param name="content">The content update for the message.</param>
        /// <returns>A task.</returns>
        Task<IChannelMessageAck> UpdateChatMessageAsync(IChannel channel, string messageId, string content);

        /// <summary>
        /// Update a chat message to a channel on the server.
        /// </summary>
        /// <param name="channelId">The ID of the chat channel with the message.</param>
        /// <param name="messageId">The ID of the message to update.</param>
        /// <param name="content">The content update for the message.</param>
        /// <returns>A task.</returns>
        Task<IChannelMessageAck> UpdateChatMessageAsync(string channelId, string messageId, string content);

        /// <summary>
        /// Update the user's status online.
        /// </summary>
        /// <param name="status">The new status of the user.</param>
        /// <returns>A task.</returns>
        Task UpdateStatusAsync(string status);

        /// <summary>
        /// Send a chat message to a channel on the server.
        /// </summary>
        /// <param name="channel">The channel to send on.</param>
        /// <param name="content">The content of the chat message.</param>
        /// <returns>A task which resolves to a Channel Ack response.</returns>
        Task<IChannelMessageAck> WriteChatMessageAsync(IChannel channel, string content);

        /// <summary>
        /// Send a chat message to a channel on the server.
        /// </summary>
        /// <param name="channelId">The ID of the channel as the destination for the message.</param>
        /// <param name="content">The content of the chat message.</param>
        /// <returns>A task which resolves to a Channel Ack response.</returns>
        Task<IChannelMessageAck> WriteChatMessageAsync(string channelId, string content);
    }
}
