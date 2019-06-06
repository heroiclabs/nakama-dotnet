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
        private const int MaxMessageSize = 1024 * 256;

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

        private readonly WebSocketClientOptions _options;
        private CancellationTokenSource _cancellationSource;
        private WebSocket _webSocket;
        private Uri _uri;

        public WebSocketAdapter(int keepAliveIntervalSec = KeepAliveIntervalSec) : this(new WebSocketClientOptions
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
        public void Close()
        {
            _cancellationSource?.Cancel();

            if (_webSocket == null) return;
            _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            _webSocket = null;
            IsConnecting = false;
            IsConnected = false;
        }

        /// <inheritdoc cref="ISocketAdapter.Connect"/>
        public async void Connect(Uri uri)
        {
            if (_webSocket != null)
            {
                ReceivedError?.Invoke(new SocketException((int) SocketError.IsConnected));
                return;
            }

            _cancellationSource = new CancellationTokenSource();
            _uri = uri;
            IsConnecting = true;

            var clientFactory = new WebSocketClientFactory();
            try
            {
                using (_webSocket = await clientFactory.ConnectAsync(_uri, _options, _cancellationSource.Token))
                {
                    IsConnected = true;
                    IsConnecting = false;
                    Connected?.Invoke();

                    await ReceiveLoop(_webSocket, _cancellationSource.Token);
                }
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
            finally
            {
                Close();
                Closed?.Invoke();
            }
        }

        public void Dispose()
        {
            _webSocket?.Dispose();
        }

        /// <inheritdoc cref="ISocketAdapter.Send"/>
        public async void Send(ArraySegment<byte> buffer, CancellationToken cancellationToken,
            bool reliable = true)
        {
            if (_webSocket == null)
            {
                ReceivedError?.Invoke(new SocketException((int) SocketError.NotConnected));
                return;
            }

            try
            {
                await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
            }
            catch (Exception e)
            {
                Close();
                ReceivedError?.Invoke(e);
            }
        }

        public override string ToString()
        {
            return
                $"WebSocketDriver(IsConnected={IsConnected}, IsConnecting={IsConnecting}, MaxMessageSize={MaxMessageSize}, Uri='{_uri}')";
        }

        private async Task ReceiveLoop(WebSocket webSocket, CancellationToken cancellationToken)
        {
            var buffer = new byte[MaxMessageSize];
            while (true)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result == null)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var data = await ReadFrames(result, webSocket, buffer);

                if (data.Count == 0)
                {
                    break;
                }

                try
                {
                    Received?.Invoke(data);
                }
                catch (Exception e)
                {
                    ReceivedError?.Invoke(e);
                }
            }
        }

        private async Task<ArraySegment<byte>> ReadFrames(WebSocketReceiveResult result, WebSocket webSocket,
            byte[] buffer)
        {
            var count = result.Count;
            while (!result.EndOfMessage)
            {
                if (count >= MaxMessageSize)
                {
                    var closeMessage = $"Maximum message size {MaxMessageSize} bytes reached.";
                    await webSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, closeMessage,
                        CancellationToken.None);
                    ReceivedError?.Invoke(new WebSocketException(WebSocketError.HeaderError));
                    return new ArraySegment<byte>();
                }

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, count, MaxMessageSize - count),
                    CancellationToken.None);
                count += result.Count;
            }

            return new ArraySegment<byte>(buffer, 0, count);
        }
    }
}