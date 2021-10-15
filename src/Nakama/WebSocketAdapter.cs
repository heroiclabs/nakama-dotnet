/**
 * Copyright 2019 The Nakama Authors
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

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Nakama.Ninja.WebSockets;

namespace Nakama
{
    /// <summary>
    /// An adapter which uses the WebSocket protocol with Nakama server.
    /// </summary>
    public class WebSocketAdapter : ISocketAdapter
    {
        private const int KeepAliveIntervalSec = 15;
        private const int MaxMessageReadSize = 1024 * 256;

        /// <inheritdoc cref="ISocketAdapter.Closed"/>
        public event Action Closed;

        /// <inheritdoc cref="ISocketAdapter.ReceivedError"/>
        public event Action<Exception> ReceivedError;

        /// <inheritdoc cref="ISocketAdapter.Received"/>
        public event Action<ArraySegment<byte>> Received;

        private readonly WebSocketClientOptions _options;
        private CancellationTokenSource _closeTcs = new CancellationTokenSource();
        private readonly Queue<SendOperation> _queuedSends = new Queue<SendOperation>();

        private readonly object _sendBufferLock = new object();

        public WebSocketAdapter(int keepAliveIntervalSec = KeepAliveIntervalSec) :
            this(new WebSocketClientOptions
            {
                IncludeExceptionInCloseResponse = true,
                KeepAliveInterval = TimeSpan.FromSeconds(keepAliveIntervalSec),
                NoDelay = true
            })
        {
        }

        public WebSocketAdapter(WebSocketClientOptions options)
        {
            _options = options;
        }

        /// <inheritdoc cref="ISocketAdapter.Close"/>
        public Task Close()
        {
            _closeTcs?.Cancel();
            _closeTcs?.Dispose();
            _closeTcs = new CancellationTokenSource();
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="ISocketAdapter.Connect"/>
        public async Task Connect(Uri uri, int timeout)
        {
            var clientFactory = new WebSocketClientFactory();

            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
                var lcts = CancellationTokenSource.CreateLinkedTokenSource(_closeTcs.Token, cts.Token);
                WebSocket webSocket = await clientFactory.ConnectAsync(uri, _options, lcts.Token);
                LaunchReceiveLoop(webSocket, _closeTcs.Token);
                LaunchSendLoop(webSocket, _closeTcs.Token);
            }
            catch (TaskCanceledException)
            {
                // No error, the socket got closed via the cancellation signal.
            }
            catch (ObjectDisposedException)
            {
                // No error, the socket got closed.
            }
            catch (Exception e)
            {
                ReceivedError?.Invoke(e);
            }
        }

        /// <inheritdoc cref="ISocketAdapter.Send"/>
        public Task Send(ArraySegment<byte> buffer, CancellationToken cancellationToken, bool reliable = true)
        {
            var operation = new SendOperation(buffer, cancellationToken);

            lock (_sendBufferLock)
            {
                _queuedSends.Enqueue(operation);
            }

            return Task.CompletedTask;
        }

        public override string ToString()
        {
            // todo add other state here.
            return
                $"WebSocketDriver(MaxMessageSize={MaxMessageReadSize})";
        }

        private void LaunchSendLoop(WebSocket webSocket, CancellationToken closeCancellationToken)
        {
           Task.Run(async() =>
           {
                while (!closeCancellationToken.IsCancellationRequested)
                {
                    SendOperation sendOperation = null;

                    lock (_sendBufferLock)
                    {
                        if (_queuedSends.Count > 0)
                        {
                            sendOperation = _queuedSends.Dequeue();
                        }
                    }

                    if (sendOperation != null)
                    {
                        await webSocket.SendAsync(sendOperation.Bytes, WebSocketMessageType.Text, true, sendOperation.CancellationToken);
                    }
                }
           }, closeCancellationToken);
        }

        private void LaunchReceiveLoop(WebSocket webSocket, CancellationToken closeCancellationToken)
        {
            Task.Run(async() =>
            {
                var buffer = new byte[MaxMessageReadSize];

                // in case cancellation requested prior to socket reading from stream
                while (!closeCancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket
                        .ReceiveAsync(new ArraySegment<byte>(buffer), closeCancellationToken)
                        .ConfigureAwait(false);

                    if (result == null || result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    var data = await ReadFrames(result, webSocket, buffer);

                    if (data.Count == 0)
                    {
                        break;
                    }

                    Received?.Invoke(data);
                }

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                webSocket.Dispose();
                Closed?.Invoke();
            }, closeCancellationToken);
        }

        private async Task<ArraySegment<byte>> ReadFrames(WebSocketReceiveResult result, WebSocket webSocket,
            byte[] buffer)
        {
            var count = result.Count;
            while (!result.EndOfMessage)
            {
                if (count >= MaxMessageReadSize)
                {
                    var closeMessage = $"Maximum message size {MaxMessageReadSize} bytes reached.";
                    await webSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, closeMessage,
                        CancellationToken.None);
                    ReceivedError?.Invoke(new WebSocketException(WebSocketError.HeaderError));
                    return new ArraySegment<byte>();
                }

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, count, MaxMessageReadSize - count),
                    CancellationToken.None).ConfigureAwait(false);
                count += result.Count;
            }

            return new ArraySegment<byte>(buffer, 0, count);
        }

        private class SendOperation
        {
            public ArraySegment<byte> Bytes { get; }
            public CancellationToken CancellationToken { get; }

            public SendOperation(ArraySegment<byte> bytes, CancellationToken cancellationToken)
            {
                Bytes = bytes;
                CancellationToken = cancellationToken;
            }
        }
    }
}
