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

        private CancellationTokenSource _closeTcs = new CancellationTokenSource();
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
            _closeTcs.Cancel();
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="ISocketAdapter.Connect"/>
        public Task Connect(Uri uri, int timeout)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            var connectTimeout = cts.Token;

            var tcs = new TaskCompletionSource<bool>();
            // todo be sure this doesn't leak
            Task.Run(() => RunWorkerTasks(uri, connectTimeout, tcs));

            return tcs.Task;
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
            while (true)
            {
                closeToken.ThrowIfCancellationRequested();

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

            while (true)
            {
                var bufferSegment = new ArraySegment<byte>(buffer, bufferReadCount, MaxMessageReadSize - bufferReadCount);

                closeToken.ThrowIfCancellationRequested();

                var result = await webSocket
                    .ReceiveAsync(bufferSegment, closeToken)
                    // todo figure out why we configure await. do we care where the continuation is marshaled back to?
                    // does unity sync context not like if we try to marshal continuation back to it via ConfigureAwait(true)?
                    .ConfigureAwait(false);


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
                else if (result.Count == 0)  // not enough space to write to the buffer segment
                {
                    // todo add this change in behavior to changelog
                    // e.g., if you were relying on previous exception now use this
                    var e = new WebSocketException($"Maximum message size {MaxMessageReadSize} bytes reached.");
                    e.Data[CloseStatusKey] = WebSocketCloseStatus.MessageTooBig;
                    throw e;
                }
            }
        }

        private async Task RunWorkerTasks(Uri uri, CancellationToken connectCancel, TaskCompletionSource<bool> connectCompleteSource)
        {
            var clientFactory = new WebSocketClientFactory();
            WebSocket webSocket = null;

            try
            {
                webSocket = await clientFactory.ConnectAsync(uri, _options, connectCancel);
                connectCompleteSource.SetResult(true);
            }
            catch (Exception e)
            {
                connectCompleteSource.SetException(e);
                webSocket?.Dispose();
                return;
            }

            var closeStatus = WebSocketCloseStatus.Empty;
            var closeMessage = "";

            try
            {
                var receiveToken = _closeTcs.Token;
                var sendToken = _closeTcs.Token;
                await Task.WhenAny(new Task[]{ReceiveLoop(webSocket,receiveToken), SendLoop(webSocket, sendToken)});
            }
            // todo what about task cancelled exception
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
                await CloseInternal(webSocket, closeMessage, closeStatus);
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
                _closeTcs.Dispose();
                _closeTcs = new CancellationTokenSource();
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
