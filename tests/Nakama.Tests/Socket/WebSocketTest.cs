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

        // ReSharper disable RedundantArgumentDefaultValue

        public WebSocketTest()
        {
            _client = ClientUtil.FromSettingsFile();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public void ShouldCreateSocket(TestAdapterFactory adapterFactory)
        {
            var client = ClientUtil.FromSettingsFile();
            var socket = Nakama.Socket.From(client, adapterFactory());
            Assert.NotNull(socket);
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldCreateSocketAndConnect(TestAdapterFactory adapterFactory)
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var completer = new TaskCompletionSource<bool>();
            var socket = Nakama.Socket.From(_client, adapterFactory());

            socket.Connected += () => completer.SetResult(true);

            await socket.ConnectAsync(session);

            Assert.True(await completer.Task);
            await socket.CloseAsync();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldCreateSocketAndDisconnect(TestAdapterFactory adapterFactory)
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var completer = new TaskCompletionSource<bool>();

            var socket = Nakama.Socket.From(_client, adapterFactory());
            socket.Closed += () => completer.SetResult(true);

            await socket.ConnectAsync(session);
            await socket.CloseAsync();

            Assert.True(await completer.Task);
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldCreateSocketAndDisconnectSilent(TestAdapterFactory adapterFactory)
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket = Nakama.Socket.From(_client, adapterFactory());

            await socket.ConnectAsync(session);
            Assert.True(socket.IsConnected);

            await socket.CloseAsync();
            Assert.False(socket.IsConnected);
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task MultipleConnectAttemptsThrowException(TestAdapterFactory adapterFactory)
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket = Nakama.Socket.From(_client, adapterFactory());

            await socket.ConnectAsync(session);
            Assert.True(socket.IsConnected);
            await Assert.ThrowsAsync<SocketException>(() => socket.ConnectAsync(session));
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task MultipleSocketsDoNotThrowException(TestAdapterFactory adapterFactory)
        {
            var id1 = $"{Guid.NewGuid()}";
            var id2 = $"{Guid.NewGuid()}";

            var session1 = await _client.AuthenticateCustomAsync(id1);
            var session2 = await _client.AuthenticateCustomAsync(id2);

            var socket1 = Nakama.Socket.From(_client, adapterFactory());
            var socket2 = Nakama.Socket.From(_client, adapterFactory());

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }
    }
}
