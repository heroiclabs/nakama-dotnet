// Copyright 2022 The Nakama Authors
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
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Nakama
{
    /// <summary>
    /// An adapter which uses the WebSocket protocol with Nakama server.
    /// </summary>
    public class WebSocketStdlibAdapter : ISocketAdapter
    {
        private const int KeepAliveIntervalSec = 15;
        private const int MaxMessageReadSize = 1024 * 256;
        private const int SendTimeoutSec = 10;

        /// <inheritdoc cref="ISocketAdapter.Connected"/>
        public event Action Connected;

        /// <inheritdoc cref="ISocketAdapter.Closed"/>
        public event Action Closed;

        /// <inheritdoc cref="ISocketAdapter.ReceivedError"/>
        public event Action<Exception> ReceivedError;

        /// <inheritdoc cref="ISocketAdapter.Received"/>
        public event Action<ArraySegment<byte>> Received;

        /// <summary>
        /// If the WebSocket is connected.
        /// </summary>
        public bool IsConnected => _webSocket != null && _webSocket.State == WebSocketState.Open;

        /// <summary>
        /// If the WebSocket is connecting.
        /// </summary>
        public bool IsConnecting => _webSocket != null && _webSocket.State == WebSocketState.Connecting;

        private CancellationTokenSource _cancellationSource;
        private Uri _uri;
        private ClientWebSocket _webSocket;
        private readonly int _maxMessageReadSize;
        private readonly TimeSpan _sendTimeoutSec;

        public WebSocketStdlibAdapter(int sendTimeoutSec = SendTimeoutSec, int maxMessageReadSize = MaxMessageReadSize)
        {
            _maxMessageReadSize = maxMessageReadSize;
            _sendTimeoutSec = TimeSpan.FromSeconds(sendTimeoutSec);
            _webSocket = new ClientWebSocket();
        }

        public WebSocketStdlibAdapter(ClientWebSocket webSocket)
        {
            // There is no way to override options so allow constructor to take a websocket that already has options.
            _webSocket = webSocket;
        }

        /// <inheritdoc cref="ISocketAdapter.CloseAsync"/>
        public async Task CloseAsync()
        {
            if (_webSocket == null) return;

            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            else if (_webSocket.State == WebSocketState.Connecting)
            {
                // cancel mid-connect
                _cancellationSource?.Cancel();
            }

            _webSocket = null;
        }

        /// <inheritdoc cref="ISocketAdapter.ConnectAsync"/>
        public async Task ConnectAsync(Uri uri, int timeout)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                // Already connected so we can return.
                return;
            }

            _cancellationSource = new CancellationTokenSource();
            _uri = uri;
            _webSocket = new ClientWebSocket();

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationSource.Token, cts.Token);
            await _webSocket.ConnectAsync(_uri, linkedCts.Token).ConfigureAwait(false);
            _ = ReceiveLoop(_webSocket, _cancellationSource.Token);
            Connected?.Invoke();
        }

        /// <inheritdoc cref="ISocketAdapter.SendAsync"/>
        public Task SendAsync(ArraySegment<byte> buffer, bool reliable = true, CancellationToken canceller = default)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            canceller.ThrowIfCancellationRequested();

            try
            {
                var cts = new CancellationTokenSource(_sendTimeoutSec);
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(canceller, cts.Token);
                var t = _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, linkedCts.Token);
                t.ConfigureAwait(false);
                return t;
            }
            catch
            {
                _ = CloseAsync();
                throw;
            }
        }

        /// <inheritdoc cref="object.ToString" />
        public override string ToString() => $"WebSocketDriver(MaxMessageSize={_maxMessageReadSize}, Uri='{_uri}')";

        private async Task ReceiveLoop(WebSocket webSocket, CancellationToken canceller)
        {
            canceller.ThrowIfCancellationRequested();

            var buffer = new byte[_maxMessageReadSize];
            var bufferReadCount = 0;

            try
            {
                do
                {
                    var bufferSegment =
                        new ArraySegment<byte>(buffer, bufferReadCount, _maxMessageReadSize - bufferReadCount);
                    var result = await webSocket.ReceiveAsync(bufferSegment, canceller).ConfigureAwait(false);
                    if (result == null)
                    {
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    bufferReadCount += result.Count;
                    if (!result.EndOfMessage) continue;

                    try
                    {
                        Received?.Invoke(new ArraySegment<byte>(buffer, 0, bufferReadCount));
                    }
                    catch (Exception e)
                    {
                        ReceivedError?.Invoke(e);
                    }

                    bufferReadCount = 0;
                } while (!canceller.IsCancellationRequested && _webSocket != null && _webSocket.State == WebSocketState.Open);
            }
            catch (Exception e)
            {
                ReceivedError?.Invoke(e);
            }
            finally
            {
                Closed?.Invoke();
            }
        }
    }
}
