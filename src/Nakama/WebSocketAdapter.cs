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
        private CancellationTokenSource _closeSource = new CancellationTokenSource();
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
            _closeSource?.Cancel();
            _closeSource?.Dispose();
            _closeSource = new CancellationTokenSource();
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="ISocketAdapter.Connect"/>
        public Task Connect(Uri uri, int timeout)
        {
            var clientFactory = new WebSocketClientFactory();

            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
                var lcts = CancellationTokenSource.CreateLinkedTokenSource(_closeSource.Token, cts.Token);
                Task<WebSocket> connectTask = clientFactory.ConnectAsync(uri, _options, lcts.Token);
                Task.Run(() => LaunchLoops(connectTask));
                return connectTask;
            }
            catch (TaskCanceledException)
            {
                // No error, the socket got closed via the cancellation signal.
            }
            catch (Exception e)
            {
                ReceivedError?.Invoke(e);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="ISocketAdapter.Send"/>
        public Task Send(ArraySegment<byte> buffer, CancellationToken cancelSendToken, bool reliable = true)
        {
            var operation = new SendOperation(buffer, cancelSendToken);

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
                $"WebSocketAdapter(MaxMessageReadSize={MaxMessageReadSize})";
        }

        private async Task SendLoop(WebSocket webSocket, CancellationToken closeToken)
        {
            while (!closeToken.IsCancellationRequested)
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
                    await webSocket.SendAsync(sendOperation.Bytes, WebSocketMessageType.Text, true, sendOperation.CancelSendToken);
                }
            }
        }

        private async Task ReceiveLoop(WebSocket webSocket, CancellationToken closeToken)
        {
            var buffer = new byte[MaxMessageReadSize];
            int bufferReadCount = 0;

            while (!closeToken.IsCancellationRequested)
            {
                // in case cancellation requested prior to beginning loop
                closeToken.ThrowIfCancellationRequested();

                var bufferSegment = new ArraySegment<byte>(buffer, bufferReadCount, MaxMessageReadSize - bufferReadCount);

                var result = await webSocket
                    .ReceiveAsync(bufferSegment, closeToken)
                    .ConfigureAwait(false);

                if (result == null || result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                bufferReadCount += result.Count;

                if (result.EndOfMessage)
                {
                    bufferReadCount = 0;
                    Received?.Invoke(new ArraySegment<byte>(buffer));
                }
                else if (result.Count == 0)  // not enough space to write to the buffer segment
                {
                    var closeMessage = $"Maximum message size {MaxMessageReadSize} bytes reached.";
                    await webSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, closeMessage, CancellationToken.None);
                    throw new WebSocketException(WebSocketError.HeaderError);
                }
            }

            System.Console.WriteLine("read loop ended");
        }

        private async Task LaunchLoops(Task<WebSocket> webSocketTask)
        {
            using (WebSocket webSocket = await webSocketTask)
            {
                try
                {
                    CancellationToken receiveToken = _closeSource.Token;
                    CancellationToken sendToken = _closeSource.Token;

                    Task.WaitAll(new Task[]{ReceiveLoop(webSocket, receiveToken), SendLoop(webSocket, sendToken)});
                }
                catch (Exception e)
                {
                    ReceivedError?.Invoke(e);
                }

                try
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                catch (Exception e)
                {
                    ReceivedError?.Invoke(e);
                }

                Closed?.Invoke();
            }
        }

        private class SendOperation
        {
            public ArraySegment<byte> Bytes { get; }
            public CancellationToken CancelSendToken { get; }

            public SendOperation(ArraySegment<byte> bytes, CancellationToken cancellationToken)
            {
                Bytes = bytes;
                CancelSendToken = cancellationToken;
            }
        }
    }
}
