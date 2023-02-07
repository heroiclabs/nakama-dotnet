// Copyright 2021 The Nakama Authors
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
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Nakama.Tests.Socket
{
    public class WebSocketTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IClient _client;
        private readonly ISocket _socket;

        // ReSharper disable RedundantArgumentDefaultValue

        public WebSocketTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _client = TestsUtil.FromSettingsFile();
            _socket = Nakama.Socket.From(_client);
            var logger = new StdoutLogger();
            _socket.ReceivedError += e => logger.ErrorFormat(e.Message);
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
        public async Task ShouldCreateSocketAndDisconnectEventListener()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var completer = new TaskCompletionSource<bool>();
            _socket.Closed += () => completer.SetResult(true);

            await _socket.ConnectAsync(session);
            await _socket.CloseAsync();

            Assert.True(await completer.Task);
            Assert.False(_socket.IsConnecting);
            Assert.False(_socket.IsConnected);
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
        public async Task MultipleConnectAttemptsDoesNotThrowException()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.ConnectAsync(session);
            Assert.True(_socket.IsConnected);
            await _socket.ConnectAsync(session);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ClosingBeforeConnecting()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.CloseAsync();
            await _socket.ConnectAsync(session);
            Assert.True(_socket.IsConnected);
        }

        [Fact(Skip = "Test case requires 60 seconds minimum execution time.")]
        public async Task LongLivedSocketLifecycle()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.ConnectAsync(session, false, 5);
            await Task.Delay(TimeSpan.FromSeconds(60));
            Assert.True(_socket.IsConnected);
            _ = _socket.CloseAsync();
        }

        [Fact(Skip = "Test requires you to disconnect the internet and wait for 60 seconds minimum")]
        public async Task SocketDetectsLossOfInternet()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.ConnectAsync(session, false, 5);
            var closeTriggered = false;
            
            _socket.Closed += () =>
            {
                _testOutputHelper.WriteLine($"Socket was closed");
                closeTriggered = true;
            };
            
            _testOutputHelper.WriteLine("---Disconnect Internet Now---");
            await Task.Delay(TimeSpan.FromSeconds(60));
            Assert.False(_socket.IsConnected);
            Assert.True(closeTriggered);
        }
    }
}
