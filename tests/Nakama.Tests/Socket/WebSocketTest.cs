/**
 * Copyright 2020 The Nakama Authors
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

namespace Nakama.Tests.Socket
{
    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Xunit;

    public class WebSocketTest
    {
        private IClient _client;
        private ISocket _socket;

        // ReSharper disable RedundantArgumentDefaultValue

        public WebSocketTest()
        {
            _client = TestsUtil.FromSettingsFile();
            _socket = Nakama.Socket.From(_client);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public void ShouldCreateSocket()
        {
            var client = TestsUtil.FromSettingsFile();
            var socket = Nakama.Socket.From(client);
            Assert.NotNull(socket);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldCreateSocketAndConnect()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var completer = new TaskCompletionSource<bool>();
            _socket.Connected += () => completer.SetResult(true);

            await _socket.ConnectAsync(session);

            Assert.True(await completer.Task);
            await _socket.CloseAsync();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldCreateSocketAndDisconnect()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var completer = new TaskCompletionSource<bool>();
            _socket.Closed += () => completer.SetResult(true);

            await _socket.ConnectAsync(session);
            await _socket.CloseAsync();

            Assert.True(await completer.Task);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldCreateSocketAndDisconnectSilent()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            await _socket.ConnectAsync(session);
            Assert.True(_socket.IsConnected);

            await _socket.CloseAsync();
            Assert.False(_socket.IsConnected);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task MultipleConnectAttemptsThrowException()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.ConnectAsync(session);
            Assert.True(_socket.IsConnected);
            await Assert.ThrowsAsync<SocketException>(() => _socket.ConnectAsync(session));
        }
    }
}
