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

using System.Collections.Concurrent;

namespace Nakama
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using vtortola.WebSockets;
    using vtortola.WebSockets.Rfc6455;

    internal abstract class WebSocketEventListener : IDisposable
    {
        public event EventHandler Connected = (sender, args) => { };
        public event EventHandler Disconnected = (sender, args) => { };
        public event EventHandler<Exception> ErrorReceived = (sender, exception) => { };
        public event EventHandler<string> MessageReceived = (sender, message) => { };

        private readonly WebSocketClient _client;
        private readonly WebSocketOptions _options;
        
        private BlockingCollection<string> _sendQueue;
        protected WebSocket _socket;

        public bool IsConnected => _socket != null && _socket.IsConnected;

        public WebSocketEventListener(WebSocketOptions options)
        {
            _sendQueue = new BlockingCollection<string>(256);
            options.ValidateOptions();
            _options = options.Clone();

            var opts = new WebSocketListenerOptions
            {
                // Must disable negotiation timeout for AOT iOS support.
                NegotiationTimeout = TimeSpan.Zero,
                PingTimeout = TimeSpan.Zero,
                PingMode = PingMode.Manual,
                CertificateValidationHandler = _options.CertificateValidationHandler
            };
            opts.Standards.RegisterRfc6455();
            opts.Transports.ConfigureTcp(tcpTransport =>
            {
                tcpTransport.NoDelay = true;
                // Dual mode needed for IPv6 support. Does not work with Mono :(
                tcpTransport.DualMode = _options.DualMode;
            });
            _client = new WebSocketClient(opts);
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (IsConnected)
            {
                return;
            }

            using (var cts = new CancellationTokenSource(_options.ConnectTimeout))
            {
                _socket = await _client.ConnectAsync(uri, cts.Token).ConfigureAwait(false);
                Connected.Invoke(this, EventArgs.Empty);
            }

            var recvTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (_socket.IsConnected && !cancellationToken.IsCancellationRequested)
                    {
                        var readStream = await _socket.ReadMessageAsync(cancellationToken).ConfigureAwait(false);
                        if (readStream == null)
                        {
                            continue; // NOTE does stream need to be consumed?
                        }

                        using (var reader = new StreamReader(readStream, true))
                        {
                            var message = await reader.ReadToEndAsync().ConfigureAwait(false);
                            MessageReceived.Invoke(this, message);
                        }
                    }
                }
                catch (Exception e)
                {
                    ErrorReceived.Invoke(this, e);
                }
                finally
                {
                    Disconnected.Invoke(this, EventArgs.Empty);
                    _sendQueue.CompleteAdding();
                }
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var cts2 = new CancellationTokenSource();
            var sendTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    foreach (var item in _sendQueue.GetConsumingEnumerable(cts2.Token))
                    {
                        using (var output = _socket.CreateMessageWriter(WebSocketMessageType.Text))
                        using (var writer = new StreamWriter(output))
                        {
                            writer.Write(item);
                        }
                    }
                }
                catch (Exception e)
                {
                    ErrorReceived?.Invoke(this, e);
                }
                finally
                {
                    // Setup a new send queue.
                    _sendQueue = new BlockingCollection<string>(256);
                }
            }, cts2.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            
#pragma warning disable 4014            
            Task.WhenAll(recvTask, sendTask);
#pragma warning restore 4014        
        }

        public async Task CloseAsync()
        {
            if (_socket != null && _socket.IsConnected)
            {
                await _socket.CloseAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }

        public void Send(string message)
        {
            _sendQueue.TryAdd(message);
        }
    }
}
