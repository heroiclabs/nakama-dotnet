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
using System.IO;
using System.Net.Sockets;
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
        public bool IsConnected { get; private set; }

        /// <summary>
        /// If the WebSocket is connecting.
        /// </summary>
        public bool IsConnecting { get; private set; }

        private readonly int _maxMessageReadSize;
        private readonly WebSocketClientOptions _options;
        private readonly TimeSpan _sendTimeoutSec;
        private CancellationTokenSource _cancellationSource;
        private WebSocket _webSocket;
        private Uri _uri;
        private readonly ILogger _logger;

        public WebSocketAdapter(int keepAliveIntervalSec = KeepAliveIntervalSec, int sendTimeoutSec = SendTimeoutSec,
            int maxMessageReadSize = MaxMessageReadSize, ILogger logger = null) :
            this(new WebSocketClientOptions
            {
                IncludeExceptionInCloseResponse = true,
                KeepAliveInterval = TimeSpan.FromSeconds(keepAliveIntervalSec),
                NoDelay = true
            }, sendTimeoutSec, maxMessageReadSize, logger)
        {
        }

        public WebSocketAdapter(WebSocketClientOptions options, int sendTimeoutSec, int maxMessageReadSize, ILogger logger)
        {
            _maxMessageReadSize = maxMessageReadSize;
            _options = options;
            _sendTimeoutSec = TimeSpan.FromSeconds(sendTimeoutSec);
            _logger = logger;
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
            IsConnecting = false;
            IsConnected = false;
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
            IsConnecting = true;

            var clientFactory = new WebSocketClientFactory();
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationSource.Token, cts.Token);
                _webSocket = await clientFactory.ConnectAsync(_uri, _options, linkedCts.Token).ConfigureAwait(false);
                _ = Task.Factory.StartNew(_ => ReceiveLoop(_webSocket, _cancellationSource.Token),
                    TaskCreationOptions.LongRunning, _cancellationSource.Token);
                Connected?.Invoke();
                IsConnected = true;
            }
            catch (Exception)
            {
                IsConnected = false;
                throw;
            }
            finally
            {
                IsConnecting = false;
            }
        }

        /// <inheritdoc cref="ISocketAdapter.SendAsync"/>
        public Task SendAsync(ArraySegment<byte> buffer, bool reliable = true,
            CancellationToken canceller = default)
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
                        // Don't stop receive loop if received function throws.
                        ReceivedError?.Invoke(e);
                    }

                    bufferReadCount = 0;
                } while (!canceller.IsCancellationRequested && _webSocket != null && _webSocket.State == WebSocketState.Open);
            }
            catch (EndOfStreamException)
            {
                // IGNORE:
                // "Unexpected end of stream encountered whilst attempting to read 2 bytes."
            }
            catch (IOException)
            {
                // IGNORE.
            }
            catch (SocketException)
            {
                // IGNORE:
                // "Unable to read data from the transport connection: Access denied."
                // "Unable to read data from the transport connection: Network subsystem is down."
                // "Unable to write data to the transport connection: The socket has been shut down."
                // "The socket is not connected"
                // "Unable to read data from the transport connection: Connection reset by peer."
                // "Unable to read data from the transport connection: Connection timed out."
                // "Unable to read data from the transport connection: Connection refused."
            }
            catch (Exception e)
            {
                ReceivedError?.Invoke(e);
            }
            finally
            {
                IsConnecting = false;
                IsConnected = false;
                Closed?.Invoke();
            }
        }
    }
}
