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
    public interface ISocket
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
        /// A logger which can write log messages. Defaults to <c>NoopLogger</c>.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// Trace all actions performed by the socket. Defaults to false.
        /// </summary>
        bool Trace { get; set; }

        Action<IApiChannelMessage> OnChannelMessage { get; set; }

        Action<IChannelPresenceEvent> OnChannelPresence { get; set; }

        Action OnConnect { get; set; }

        Action OnDisconnect { get; set; }

        Action OnError { get; set; }

        Action<IMatchState> OnMatchState { get; set; }

        Action<IMatchPresenceEvent> OnMatchPresence { get; set; }

        Action<IMatchmakerMatched> OnMatchmakerMatched { get; set; }

        Action<IApiNotification> OnNotification { get; set; }

        Action<IStatusPresenceEvent> OnStatusPresence { get; set; }

        Action<IStreamPresenceEvent> OnStreamPresence { get; set; }

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
