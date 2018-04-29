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
    /// A client to interact with Nakama server.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// The host address of the server. Defaults to "127.0.0.1".
        /// </summary>
        string Host { get; }

        /// <summary>
        /// A logger which can write log messages. Defaults to <c>NoopLogger</c>.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// The port number of the server. Defaults to 7350.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// The number of retries to attempt with each request with the server.
        /// </summary>
        int Retries { get; set; }

        /// <summary>
        /// The key used to authenticate with the server without a session. Defaults to "defaultkey".
        /// </summary>
        string ServerKey { get; }

        /// <summary>
        /// Set connection strings to use the secure mode with the server. Defaults to false.
        /// <remarks>
        /// The server must be configured to make use of this option. With HTTP, GRPC, and WebSockets the server must
        /// be configured with an SSL certificate or use a load balancer which performs SSL termination. For rUDP you
        /// must configure the server to expose it's IP address so it can be bundled within session tokens. See the
        /// server documentation for more information.
        /// </remarks>
        /// </summary>
        bool Secure { get; }

        /// <summary>
        /// Trace all actions performed by the client. Defaults to false.
        /// </summary>
        bool Trace { get; set; }

        /// <summary>
        /// Set the timeout on requests sent to the server.
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// Authenticate a user with a custom id against the server.
        /// </summary>
        /// <param name="id">The custom identifier usually obtained from an external authentication service.</param>
        /// <returns>A task to resolve a session object.</returns>
        Task<ISession> AuthenticateCustomAsync(string id);

        /// <summary>
        /// Create a new WebSocket from the client.
        /// </summary>
        /// <param name="session">The session for the current authenticated user.</param>
        /// <param name="appearOnline">True if the user should appear online to other users.</param>
        /// <param name="reconnect">Set the number of retries to attempt after a disconnect.</param>
        /// <returns>A socket object.</returns>
        Task<ISocket> CreateWebSocketAsync(ISession session, bool appearOnline = true, int reconnect = 3);
    }
}
