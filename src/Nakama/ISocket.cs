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
        /// Close the connection with the server.
        /// </summary>
        /// <returns>A close task.</returns>
        Task DisconnectAsync();
    }

    /// <summary>
    /// Enumerates the socket protocols which can be used with Nakama server.
    /// </summary>
    public enum SocketProtocol
    {
        /// <summary>
        /// Use an custom protocol with the <c>ISocket</c>.
        /// </summary>
        Custom = 0,
        /// <summary>
        /// Use the WebSocket protocol with the <c>ISocket</c>.
        /// </summary>
        WebSocket = 1,
        /// <summary>
        /// Use the rUDP protocol with the <c>ISocket</c>.
        /// </summary>
        Udp = 2
    }
}
