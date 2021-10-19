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
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading;
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

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task SocketCancelsWhileConnecting()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var canceller = new CancellationTokenSource();

            var connectTask = _socket.ConnectAsync(session, false, 30, "en", null, canceller);
            canceller.Cancel();

            await Assert.ThrowsAsync<Nakama.Ninja.WebSockets.Exceptions.WebSocketHandshakeFailedException>(async () => await connectTask);
            Assert.False(_socket.IsConnected);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task SocketRetriesAfterConnectFailure()
        {
            var adapter =  new TransientExceptionSocketAdapter(new TransientExceptionSocketAdapter.NetworkSchedule(new TransientAdapterResponseType[]{TransientAdapterResponseType.TransientError, TransientAdapterResponseType.ServerOk}));
            _socket = Nakama.Socket.From(_client, adapter);
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            int numInvocations = 0;
            var retryConfiguration = new RetryConfiguration(1, 1, delegate {
                numInvocations++;
            });

            await _socket.ConnectAsync(session, appearOnline: false, connectTimeout: 30, langTag: "en", retryConfiguration);
            Assert.Equal(1, numInvocations);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task SocketRetriesAfterBrokenConnection()
        {
            var connectSchedule = new TransientAdapterResponseType[]{TransientAdapterResponseType.ServerOk};

            var firstBreak = new Tuple<TimeSpan, TransientAdapterResponseType>(TimeSpan.FromMilliseconds(150), TransientAdapterResponseType.TransientError);

            var reconnectSchedule = new List<Tuple<TimeSpan, TransientAdapterResponseType>>();
            reconnectSchedule.Add(firstBreak);

            var schedule = new TransientExceptionSocketAdapter.NetworkSchedule(connectSchedule, reconnectSchedule);
            var adapter =  new TransientExceptionSocketAdapter(schedule);

            _socket = Nakama.Socket.From(_client, adapter);
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var connectedTwiceTask = new TaskCompletionSource();
            var closedOnceTask = new TaskCompletionSource();

            int numConnects = 0;
            _socket.Connected += () =>
            {
                numConnects++;
                if (numConnects == 2)
                {
                    connectedTwiceTask.SetResult();
                }
            };

            int numCloses = 0;
            _socket.Closed += () =>
            {
                numCloses++;
                closedOnceTask.SetResult();
            };

            await _socket.ConnectAsync(session, appearOnline: false, connectTimeout: 30, langTag: "en");
            await connectedTwiceTask.Task;
            await closedOnceTask.Task;

            Assert.Equal(2, numConnects);
            Assert.Equal(1, numCloses);
        }
    }
}
