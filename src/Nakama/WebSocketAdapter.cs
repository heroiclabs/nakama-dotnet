/**
 * Copyright 2021 The Nakama Authors
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
        private const string CloseStatusKey = "CloseStatus";

        /// <inheritdoc cref="ISocketAdapter.Closed"/>
        public event Action Closed;

        /// <inheritdoc cref="ISocketAdapter.ReceivedError"/>
        public event Action<Exception> ReceivedError;

        /// <inheritdoc cref="ISocketAdapter.Received"/>
        public event Action<ArraySegment<byte>> Received;

        private CancellationTokenSource _userCloseTcs = new CancellationTokenSource();
        private readonly WebSocketClientOptions _options;
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
            _userCloseTcs.Cancel();
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="ISocketAdapter.Connect"/>
        public async Task Connect(Uri uri, int timeout)
        {
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            var timeoutToken = timeoutCts.Token;
            var userCloseToken = _userCloseTcs.Token;
            var connectCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, userCloseToken);
            var connectToken = connectCts.Token;

            var clientFactory = new WebSocketClientFactory();

            WebSocket webSocket = null;

            try
            {
                webSocket = await clientFactory.ConnectAsync(uri, _options, connectToken);
                Task.Run(() => RunSocketLoop(webSocket));
            }
            catch
            {
                webSocket?.Dispose();
                throw;
            }
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
            return
                $"WebSocketAdapter(MaxMessageReadSize={MaxMessageReadSize})";
        }

        private void RunSocketLoop(WebSocket webSocket)
        {
            var closeStatus = WebSocketCloseStatus.Empty;
            var closeMessage = "";

            try
            {
                var buffer = new byte[MaxMessageReadSize];
                int bufferReadCount = 0;
                var userCloseToken = _userCloseTcs.Token;
                Task<WebSocketReceiveResult> receiveTask = null;

                while (true)
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
                        webSocket.SendAsync(sendOperation.Bytes, WebSocketMessageType.Text, true, sendOperation.CancelSendToken);
                    }

                    // todo what if the last param goes negative
                    var bufferSegment = new ArraySegment<byte>(buffer, bufferReadCount, MaxMessageReadSize - bufferReadCount);

                    if (receiveTask == null)
                    {
                        receiveTask = webSocket.ReceiveAsync(bufferSegment, userCloseToken);
                    }

                    if (receiveTask.Status == TaskStatus.Canceled)
                    {
                        // user closed
                        receiveTask = null;
                        break;
                    }
                    else if (receiveTask.Status == TaskStatus.Faulted)
                    {
                        Exception e = receiveTask.Exception;
                        receiveTask = null;
                        throw e;
                    }
                    else if (receiveTask.Status == TaskStatus.RanToCompletion)
                    {
                        var result = receiveTask.Result;

                        if (result == null || result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }

                        bufferReadCount += result.Count;

                        if (result.EndOfMessage)
                        {
                            Received?.Invoke(new ArraySegment<byte>(buffer, 0, bufferReadCount));
                            bufferReadCount = 0;
                        }

                        receiveTask = null;
                    }
                }
            }
            catch (Exception e)
            {
                if (e.Data.Contains(CloseStatusKey))
                {
                    closeStatus = (WebSocketCloseStatus) e.Data[CloseStatusKey];
                }

                closeMessage = e.Message;
                ReceivedError?.Invoke(e);
            }
            finally
            {
                Task.Run(() => CloseInternal(webSocket, closeMessage, closeStatus));
            }
        }

        private async Task CloseInternal(WebSocket webSocket, string closeMessage, WebSocketCloseStatus closeStatus)
        {
            try
            {
                await webSocket.CloseAsync(closeStatus, "", CancellationToken.None);
            }
            catch (Exception e)
            {
                ReceivedError?.Invoke(e);
            }
            finally
            {
                // create a fresh cancellation token source for closing
                _userCloseTcs.Dispose();
                _userCloseTcs = new CancellationTokenSource();
                webSocket?.Dispose();
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
