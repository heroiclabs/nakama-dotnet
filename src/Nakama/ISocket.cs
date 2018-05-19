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

    /// <summary>
    /// A socket to interact with Nakama server.
    /// </summary>
    public interface ISocket : IDisposable
    {
        /// <summary>
        /// The protocol in use with this socket.
        /// </summary>
        SocketProtocol Protocol { get; }

        /// <summary>
        /// The number of reconnects to attempt when the socket is closed with the server.
        /// </summary>
        int Reconnect { get; set; }

        /// <summary>
        /// A logger which can write log messages. Defaults to <c>NullLogger</c>.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// Trace all actions performed by the socket. Defaults to false.
        /// </summary>
        bool Trace { get; set; }

        /// <summary>
        /// Receive chat channel messages.
        /// </summary>
        Action<IApiChannelMessage> OnChannelMessage { get; set; }

        /// <summary>
        /// Receive chat channel presences for when users join and leave.
        /// </summary>
        Action<IChannelPresenceEvent> OnChannelPresence { get; set; }

        /// <summary>
        /// Receive an event when the socket connects.
        /// </summary>
        Action OnConnect { get; set; }

        /// <summary>
        /// Receive an event when the socket disconnects.
        /// </summary>
        Action OnDisconnect { get; set; }

        /// <summary>
        /// Receive an event when the socket has an error.
        /// </summary>
        Action OnError { get; set; }

        /// <summary>
        /// Receive state messages from a realtime match.
        /// </summary>
        Action<IMatchState> OnMatchState { get; set; }

        /// <summary>
        /// Receive match presences for when users join and leave.
        /// </summary>
        Action<IMatchPresenceEvent> OnMatchPresence { get; set; }

        /// <summary>
        /// Receive an event when the player gets matched by the matchmaker.
        /// </summary>
        Action<IMatchmakerMatched> OnMatchmakerMatched { get; set; }

        /// <summary>
        /// Receive realtime notifications.
        /// </summary>
        Action<IApiNotification> OnNotification { get; set; }

        /// <summary>
        /// Receive presence events for when a user updates their status.
        /// </summary>
        Action<IStatusPresenceEvent> OnStatusPresence { get; set; }

        /// <summary>
        /// Receive low level presence events from a realtime stream.
        /// </summary>
        Action<IStreamPresenceEvent> OnStreamPresence { get; set; }

        /// <summary>
        /// Receive state messages from a realtime stream.
        /// </summary>
        Action<IStreamState> OnStreamState { get; set; }

        /// <summary>
        /// Connect to the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="appearOnline">True if the socket should show the user as online to others.</param>
        /// <returns>A task to resolve the session object as valid.</returns>
        Task<ISession> ConnectAsync(ISession session, bool appearOnline = false);

        /// <summary>
        /// Connect to the server.
        /// </summary>
        /// <param name="session">The session of the user.</param>
        /// <param name="appearOnline">True if the socket should show the user as online to others.</param>
        /// <param name="ct">A cancellation token for the asynchronous operation.</param>
        /// <returns>A task to resolve the session object as valid.</returns>
        Task<ISession> ConnectAsync(ISession session, bool appearOnline, CancellationToken ct);

        /// <summary>
        /// Close the connection with the server.
        /// </summary>
        /// <param name="dispatch">True if the disconnect should dispatch an on disconnect event.</param>
        /// <returns>A close task.</returns>
        Task DisconnectAsync(bool dispatch = true);
    }

    /// <summary>
    /// Enumerates the socket protocols which can be used with Nakama server.
    /// </summary>
    public enum SocketProtocol
    {
        /// <summary>
        /// Use the WebSocket protocol with the <c>ISocket</c>.
        /// </summary>
        WebSocket = 0,
        /// <summary>
        /// Use the rUDP protocol with the <c>ISocket</c>.
        /// </summary>
        //Udp = 1
    }
}
