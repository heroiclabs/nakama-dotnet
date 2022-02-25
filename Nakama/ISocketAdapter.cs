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
using System.Threading;
using System.Threading.Tasks;

namespace Nakama
{
    /// <summary>
    /// An adapter which implements a socket with a protocol supported by Nakama.
    /// </summary>
    public interface ISocketAdapter
    {
        /// <summary>
        /// An event dispatched when the socket is connected.
        /// </summary>
        event Action Connected;

        /// <summary>
        /// An event dispatched when the socket is disconnected.
        /// </summary>
        event Action Closed;

        /// <summary>
        /// An event dispatched when the socket has an error when connected.
        /// </summary>
        event Action<Exception> ReceivedError;

        /// <summary>
        /// An event dispatched when the socket receives a message.
        /// </summary>
        event Action<ArraySegment<byte>> Received;

        /// <summary>
        /// If the socket is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// If the socket is connecting.
        /// </summary>
        bool IsConnecting { get; }

        /// <summary>
        /// Close the socket with an asynchronous operation.
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// Connect to the server with an asynchronous operation.
        /// </summary>
        /// <param name="uri">The URI of the server.</param>
        /// <param name="timeout">The timeout for the connect attempt on the socket.</param>
        Task ConnectAsync(Uri uri, int timeout);

        /// <summary>
        /// Send data to the server with an asynchronous operation.
        /// </summary>
        /// <param name="buffer">The buffer with the message to send.</param>
        /// <param name="reliable">If the message should be sent reliably (will be ignored by some protocols).</param>
        /// <param name="canceller">A cancellation token used to propagate when the operation should be canceled.</param>
        Task SendAsync(ArraySegment<byte> buffer, bool reliable = true, CancellationToken canceller = default);
    }
}
