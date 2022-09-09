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
using System.Threading.Tasks;

namespace Nakama
{
    /// <summary>
    /// A socket to interact with Nakama server.
    /// </summary>
    public interface ISocket
    {
        /// <summary>
        /// Received when a socket is closed.
        /// </summary>
        event Action Closed;

        /// <summary>
        /// Received when a socket is connected.
        /// </summary>
        event Action Connected;

        /// <summary>
        /// Received a chat channel message.
        /// </summary>
        event Action<IApiChannelMessage> ReceivedChannelMessage;

        /// <summary>
        /// Received a presence change for joins and leaves with users in a chat channel.
        /// </summary>
        event Action<IChannelPresenceEvent> ReceivedChannelPresence;

        /// <summary>
        /// Received when an error occurs on the socket.
        /// </summary>
        event Action<Exception> ReceivedError;

        /// <summary>
        /// Received a matchmaker matched message.
        /// </summary>
        event Action<IMatchmakerMatched> ReceivedMatchmakerMatched;

        /// <summary>
        /// Received a message from a multiplayer match.
        /// </summary>
        event Action<IMatchState> ReceivedMatchState;

        /// <summary>
        /// Received a presence change for joins and leaves of users in a multiplayer match.
        /// </summary>
        event Action<IMatchPresenceEvent> ReceivedMatchPresence;

        /// <summary>
        /// Received a notification for the current user.
        /// </summary>
        event Action<IApiNotification> ReceivedNotification;

        /// <summary>
        /// Received a party event. This will occur when the current user's invitation request is accepted
        /// by the party leader of a closed party.
        /// </summary>
        event Action<IParty> ReceivedParty;

        /// <summary>
        /// Received a party close event.
        /// </summary>
        event Action<IPartyClose> ReceivedPartyClose;

        /// <summary>
        /// Received custom party data.
        /// </summary>
        event Action<IPartyData> ReceivedPartyData;

        /// <summary>
        /// Received a request to join the party.
        /// </summary>
        event Action<IPartyJoinRequest> ReceivedPartyJoinRequest;

        /// <summary>
        /// Received a change in the party leader.
        /// </summary>
        event Action<IPartyLeader> ReceivedPartyLeader;

        /// <summary>
        /// Received a new matchmaker ticket for the party.
        /// </summary>
        event Action<IPartyMatchmakerTicket> ReceivedPartyMatchmakerTicket;

        /// <summary>
        /// Received a new presence event in the party.
        /// </summary>
        event Action<IPartyPresenceEvent> ReceivedPartyPresence;

        /// <summary>
        /// Received a presence change for when a user updated their online status.
        /// </summary>
        event Action<IStatusPresenceEvent> ReceivedStatusPresence;

        /// <summary>
        /// Received a presence change for joins and leaves on a realtime stream.
        /// </summary>
        event Action<IStreamPresenceEvent> ReceivedStreamPresence;

        /// <summary>
        /// Received a message from a realtime stream.
        /// </summary>
        event Action<IStreamState> ReceivedStreamState;

        /// <summary>
        /// If the socket is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// If the socket is connecting.
        /// </summary>
        bool IsConnecting { get; }

        /// <summary>
        /// Accept a party member's request to join the party.
        /// </summary>
        /// <param name="partyId">The party ID to accept the join request for.</param>
        /// <param name="presence"> The presence to accept as a party member. </param>
        /// <returns>A task to represent the asynchronous operation.</returns>
        Task AcceptPartyMemberAsync(string partyId, IUserPresence presence);

        /// <summary>
        /// Join the matchmaker pool and search for opponents on the server.
        /// </summary>
        /// <param name="query">The matchmaker query to search for opponents.</param>
        /// <param name="minCount">The minimum number of players to compete against in a match.</param>
        /// <param name="maxCount">The maximum number of players to compete against in a match.</param>
        /// <param name="stringProperties">A set of key/value properties to provide to searches.</param>
        /// <param name="numericProperties">A set of key/value numeric properties to provide to searches.</param>
        /// <param name="countMultiple">An optional integer to force the matchmaker to match in multiples of.</param>
        /// <returns>A task which resolves to a matchmaker ticket object.</returns>
        Task<IMatchmakerTicket> AddMatchmakerAsync(string query = "*", int minCount = 2, int maxCount = 8,
            Dictionary<string, string> stringProperties = null, Dictionary<string, double> numericProperties = null, int? countMultiple = null);

        /// <summary>
        /// Begin matchmaking as a party.
        /// </summary>
        /// <param name="partyId">Party ID.</param>
        /// <param name="query">Filter query used to identify suitable users.</param>
        /// <param name="minCount">Minimum total user count to match together.</param>
        /// <param name="maxCount">Maximum total user count to match together.</param>
        /// <param name="stringProperties">String properties.</param>
        /// <param name="numericProperties">Numeric properties.</param>
        /// <param name="countMultiple">An optional integer to force the matchmaker to match in multiples of.</param>
        /// <returns>A task which resolves to a party matchmaker ticket object.</returns>
        Task<IPartyMatchmakerTicket> AddMatchmakerPartyAsync(string partyId, string query, int minCount, int maxCount,
            Dictionary<string, string> stringProperties = null, Dictionary<string, double> numericProperties = null, int? countMultiple = null);

        /// <summary>
        /// End a party, kicking all party members and closing it.
        /// </summary>
        /// <param name="partyId">The ID of the party.</param>
        /// <returns>A task to represent the asynchronous operation.</returns>
        Task ClosePartyAsync(string partyId);

        /// <summary>
        /// Close the socket connection to the server.
        /// </summary>
        /// <returns>A task to represent the asynchronous operation.</returns>
        Task CloseAsync();

        /// <summary>
        /// Connect to the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="appearOnline">If the user who appear online to other users.</param>
        /// <param name="connectTimeout">The time allowed for the socket connection to be established.</param>
        /// <param name="langTag">The language tag of the user on the connected socket.</param>
        /// <returns>A task to represent the asynchronous operation.</returns>
        Task ConnectAsync(ISession session, bool appearOnline = false,
            int connectTimeout = Socket.DefaultConnectTimeout, string langTag = "en");

        /// <summary>
        /// Create a multiplayer match on the server.
        /// </summary>
        /// <returns>A task to represent the asynchronous operation.</returns>
        Task<IMatch> CreateMatchAsync(string matchName = null);

        /// <summary>
        /// Create a party.
        /// </summary>
        /// <param name="open">Whether or not the party will require join requests to be approved by the party leader.</param>
        /// <param name="maxSize">Maximum number of party members.</param>
        /// <returns>A task to represent the asynchronous operation.</returns>
        Task<IParty> CreatePartyAsync(bool open, int maxSize);

        /// <summary>
        /// Subscribe to one or more users for their status updates.
        /// </summary>
        /// <param name="users">The users.</param>
        /// <returns>A task which resolves to the current statuses for the users.</returns>
        Task<IStatus> FollowUsersAsync(IEnumerable<IApiUser> users);

        /// <summary>
        /// Subscribe to one or more users for their status updates.
        /// </summary>
        /// <param name="userIDs">The IDs of users.</param>
        /// <param name="usernames">The usernames of the users.</param>
        /// <returns>A task which resolves to the current statuses for the users.</returns>
        Task<IStatus> FollowUsersAsync(IEnumerable<string> userIDs, IEnumerable<string> usernames = null);

        /// <summary>
        /// Join a chat channel on the server.
        /// </summary>
        /// <param name="target">The target channel to join.</param>
        /// <param name="type">The type of channel to join.</param>
        /// <param name="persistence">If chat messages should be stored.</param>
        /// <param name="hidden">If the current user should be hidden on the channel.</param>
        /// <returns>A task which resolves to a chat channel object.</returns>
        Task<IChannel> JoinChatAsync(string target, ChannelType type, bool persistence = false, bool hidden = false);

        /// <summary>
        /// Join a party.
        /// </summary>
        /// <param name="partyId">Party ID</param>
        /// <returns>A task to represent the asynchronous operation.</returns>
        Task JoinPartyAsync(string partyId);

        /// <summary>
        /// Join a multiplayer match with the matchmaker matched object.
        /// </summary>
        /// <param name="matched">A matchmaker matched object.</param>
        /// <returns>A task which resolves to a multiplayer match.</returns>
        Task<IMatch> JoinMatchAsync(IMatchmakerMatched matched);

        /// <summary>
        /// Join a multiplayer match by ID.
        /// </summary>
        /// <param name="matchId">The ID of the match to attempt to join.</param>
        /// <param name="metadata">An optional set of key-value metadata pairs to be passed to the match handler.</param>
        /// <returns>A task which resolves to a multiplayer match.</returns>
        Task<IMatch> JoinMatchAsync(string matchId, IDictionary<string, string> metadata = null);

        /// <summary>
        /// Leave a chat channel on the server.
        /// </summary>
        /// <param name="channel">The chat channel to leave.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LeaveChatAsync(IChannel channel);

        /// <summary>
        /// Leave a chat channel on the server.
        /// </summary>
        /// <param name="channelId">The ID of the chat channel to leave.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LeaveChatAsync(string channelId);

        /// <summary>
        /// Leave a multiplayer match on the server.
        /// </summary>
        /// <param name="match">The multiplayer match to leave.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LeaveMatchAsync(IMatch match);

        /// <summary>
        /// Leave a multiplayer match on the server.
        /// </summary>
        /// <param name="matchId">The multiplayer match to leave.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task LeaveMatchAsync(string matchId);

        /// <summary>
        /// Leave the party.
        /// </summary>
        /// <param name="partyId">Party ID.</param>
        /// <returns>A task to represent the asynchronous operation.</returns>
        Task LeavePartyAsync(string partyId);

        /// <summary>
        /// Request a list of pending join requests for a party.
        /// </summary>
        /// <param name="partyId">Party ID.</param>
        /// <returns>A task which resolves to a list of all party join requests.</returns>
        Task<IPartyJoinRequest> ListPartyJoinRequestsAsync(string partyId);

        /// <summary>
        /// Promote a new party leader.
        /// </summary>
        /// <param name="partyId">Party ID.</param>
        /// <param name="partyMember">The presence of an existing party member to promote as the new leader.</param>
        /// <returns>A task which resolves to an announcement of a new party leader.</returns>
        Task PromotePartyMemberAsync(string partyId, IUserPresence partyMember);

        /// <summary>
        /// Remove a chat message from a chat channel on the server.
        /// </summary>
        /// <param name="channel">The chat channel with the message to remove.</param>
        /// <param name="messageId">The ID of the chat message to remove.</param>
        /// <returns>A task which resolves to an acknowledgement of the removed message.</returns>
        Task<IChannelMessageAck> RemoveChatMessageAsync(IChannel channel, string messageId);

        /// <summary>
        /// Remove a chat message from a chat channel on the server.
        /// </summary>
        /// <param name="channelId">The ID of the chat channel with the message to remove.</param>
        /// <param name="messageId">The ID of the chat message to remove.</param>
        /// <returns>A task which resolves to an acknowledgement of the removed message.</returns>
        Task<IChannelMessageAck> RemoveChatMessageAsync(string channelId, string messageId);

        /// <summary>
        /// Leave the matchmaker pool with the ticket.
        /// </summary>
        /// <param name="ticket">The ticket returned by the matchmaker on join.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task RemoveMatchmakerAsync(IMatchmakerTicket ticket);

        /// <summary>
        /// Leave the matchmaker pool with the ticket contents.
        /// </summary>
        /// <param name="ticket">The contents of the ticket returned by the matchmaker on join.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task RemoveMatchmakerAsync(string ticket);

        /// <summary>
        /// Cancel a party matchmaking process using a ticket.
        /// </summary>
        /// <param name="partyId">Party ID.</param>
        /// <param name="ticket">The ticket to cancel.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task RemoveMatchmakerPartyAsync(string partyId, string ticket);

        /// <summary>
        /// Kick a party member, or decline a request to join.
        /// </summary>
        /// <param name="partyId">Party ID to remove/reject from.</param>
        /// <param name="presence">The presence to remove or reject.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task RemovePartyMemberAsync(string partyId, IUserPresence presence);

        /// <summary>
        /// Execute an RPC function to the server.
        /// </summary>
        /// <param name="funcId">The ID of the function to execute.</param>
        /// <param name="payload">An (optional) payload to send to the server.</param>
        /// <returns>A task which resolves to the RPC function response object.</returns>
        Task<IApiRpc> RpcAsync(string funcId, string payload = null);

        /// <summary>
        /// Execute an RPC function to the server.
        /// </summary>
        /// <param name="funcId">The ID of the function to execute.</param>
        /// <param name="payload">An (optional) payload sent to the server from the byte buffer.</param>
        /// <returns>A task which resolves to the RPC function response object.</returns>
        Task<IApiRpc> RpcAsync(string funcId, ArraySegment<byte> payload);

        /// <summary>
        /// Send input to a multiplayer match on the server.
        /// </summary>
        /// /// <remarks>
        /// When no presences are supplied the new match state will be sent to all presences.
        /// </remarks>
        /// <param name="matchId">The ID of the match.</param>
        /// <param name="opCode">An operation code for the input.</param>
        /// <param name="state">The input data to send.</param>
        /// <param name="presences">The presences in the match who should receive the input.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task SendMatchStateAsync(string matchId, long opCode, string state,
            IEnumerable<IUserPresence> presences = null);

        /// <summary>
        /// Send input to a multiplayer match on the server.
        /// </summary>
        /// <param name="matchId">The ID of the match.</param>
        /// <param name="opCode">An operation code for the input.</param>
        /// <param name="state">The input data to send from the byte buffer.</param>
        /// <param name="presences">The presences in the match who should receive the input.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task SendMatchStateAsync(string matchId, long opCode, ArraySegment<byte> state,
            IEnumerable<IUserPresence> presences = null);

        /// <summary>
        /// Send input to a multiplayer match on the server.
        /// </summary>
        /// /// <remarks>
        /// When no presences are supplied the new match state will be sent to all presences.
        /// </remarks>
        /// <param name="matchId">The ID of the match.</param>
        /// <param name="opCode">An operation code for the input.</param>
        /// <param name="state">The input data to send.</param>
        /// <param name="presences">The presences in the match who should receive the input.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task SendMatchStateAsync(string matchId, long opCode, byte[] state,
            IEnumerable<IUserPresence> presences = null);

        /// <summary>
        /// Send data to a party.
        /// </summary>
        /// <param name="partyId">Party ID to send to.</param>
        /// <param name="opCode">Op code value.</param>
        /// <param name="data">The input data to send from the byte buffer, if any.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task SendPartyDataAsync(string partyId, long opCode, ArraySegment<byte> data);

        /// <summary>
        /// Send data to a party.
        /// </summary>
        /// <param name="partyId">Party ID to send to.</param>
        /// <param name="opCode">Op code value.</param>
        /// <param name="data">Data payload, if any.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task SendPartyDataAsync(string partyId, long opCode, string data);

        /// <summary>
        /// Send data to a party.
        /// </summary>
        /// <param name="partyId">Party ID to send to.</param>
        /// <param name="opCode">Op code value.</param>
        /// <param name="data">Data payload, if any.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task SendPartyDataAsync(string partyId, long opCode, byte[] data);

        /// <summary>
        /// Unfollow one or more users from their status updates.
        /// </summary>
        /// <param name="users">The users to unfollow.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UnfollowUsersAsync(IEnumerable<IApiUser> users);

        /// <summary>
        /// Unfollow one or more users from their status updates.
        /// </summary>
        /// <param name="userIDs">The IDs of the users to unfollow.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UnfollowUsersAsync(IEnumerable<string> userIDs);

        /// <summary>
        /// Update a chat message on a chat channel in the server.
        /// </summary>
        /// <param name="channel">The chat channel with the message to update.</param>
        /// <param name="messageId">The ID of the message to update.</param>
        /// <param name="content">The new contents of the chat message.</param>
        /// <returns>A task which resolves to an acknowledgement of the updated message.</returns>
        Task<IChannelMessageAck> UpdateChatMessageAsync(IChannel channel, string messageId, string content);

        /// <summary>
        /// Update a chat message on a chat channel in the server.
        /// </summary>
        /// <param name="channelId">The ID of the chat channel with the message to update.</param>
        /// <param name="messageId">The ID of the message to update.</param>
        /// <param name="content">The new contents of the chat message.</param>
        /// <returns>A task which resolves to an acknowledgement of the updated message.</returns>
        Task<IChannelMessageAck> UpdateChatMessageAsync(string channelId, string messageId, string content);

        /// <summary>
        /// Update the status for the current user online.
        /// </summary>
        /// <param name="status">The new status for the user.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        Task UpdateStatusAsync(string status);

        /// <summary>
        /// Send a chat message to a chat channel on the server.
        /// </summary>
        /// <param name="channel">The chat channel to send onto.</param>
        /// <param name="content">The contents of the message to send.</param>
        /// <returns>A task which resolves to the acknowledgement of the chat message write.</returns>
        Task<IChannelMessageAck> WriteChatMessageAsync(IChannel channel, string content);

        /// <summary>
        /// Send a chat message to a chat channel on the server.
        /// </summary>
        /// <param name="channelId">The ID of the chat channel to send onto.</param>
        /// <param name="content">The contents of the message to send.</param>
        /// <returns>A task which resolves to the acknowledgement of the chat message write.</returns>
        Task<IChannelMessageAck> WriteChatMessageAsync(string channelId, string content);
    }
}
