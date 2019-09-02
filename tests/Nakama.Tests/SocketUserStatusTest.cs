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
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests
{
    // NOTE Test name patterns are: MethodName_StateUnderTest_ExpectedBehavior
    public class SocketUserStatusTest : IDisposable
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        private IClient _client;
        private readonly ISocket _socket;

        public SocketUserStatusTest()
        {
            _client = ClientUtil.FromSettingsFile();
            _socket = Socket.From(_client);
        }

        public void Dispose()
        {
            _socket.Dispose();
            _client = null;
        }

        [Fact]
        public async void FollowUsers_NoUsers_AnotherUser()
        {
            var id = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id);
            var session2 = await _client.AuthenticateCustomAsync(id + "a");

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.SetCanceled());
            canceller.CancelAfter(Timeout);
            _socket.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            _socket.ReceivedError += e => completer.TrySetException(e);
            await _socket.ConnectAsync(session1);
            await _socket.FollowUsersAsync(new[] {session2.UserId});

            var socket = Socket.From(_client);
            await socket.ConnectAsync(session2);
            await socket.UpdateStatusAsync("new status change");

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session2.UserId));
        }

        [Fact]
        public async void FollowUsers_NoUsers_AnotherUserByUsername()
        {
            var id = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id);
            var session2 = await _client.AuthenticateCustomAsync(id + "a");

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.SetCanceled());
            canceller.CancelAfter(Timeout);
            _socket.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            _socket.ReceivedError += e => completer.TrySetException(e);
            await _socket.ConnectAsync(session1);
            await _socket.FollowUsersAsync(new string[] { }, new[] {session2.Username});

            var socket = Socket.From(_client);
            await socket.ConnectAsync(session2);
            await socket.UpdateStatusAsync("new status change");

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session2.UserId));
        }

        [Fact]
        public async void FollowUsers_NoUsers_FollowedSelf()
        {
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);
            await _socket.ConnectAsync(session);

            var statuses = await _socket.FollowUsersAsync(new[] {session.UserId});
            Assert.NotNull(statuses);
            Assert.Empty(statuses.Presences);
        }

        [Fact]
        public async void FollowUsers_NoUsers_UserJoinsAndLeaves()
        {
            var id = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id);
            var session2 = await _client.AuthenticateCustomAsync(id + "a");

            var completer1 = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer1.SetCanceled());
            canceller.CancelAfter(Timeout);
            _socket.ReceivedStatusPresence += statuses => completer1.TrySetResult(statuses);
            _socket.ReceivedError += e => completer1.TrySetException(e);
            await _socket.ConnectAsync(session1);
            await _socket.FollowUsersAsync(new[] {session2.UserId});

            // Second user comes online and sets status.
            var socket = Socket.From(_client);
            await socket.ConnectAsync(session2);
            await socket.UpdateStatusAsync("new status change");

            var result1 = await completer1.Task;
            Assert.NotNull(result1);
            Assert.Empty(result1.Leaves);
            Assert.Contains(result1.Joins, joined => joined.UserId.Equals(session2.UserId));

            var completer2 = new TaskCompletionSource<IStatusPresenceEvent>();
            _socket.ReceivedStatusPresence += statuses => completer2.SetResult(statuses);

            // Second user drops offline.
            await socket.CloseAsync();
            var result2 = await completer2.Task;
            Assert.NotNull(result2);
            Assert.Empty(result2.Joins);
            Assert.Contains(result2.Leaves, left => left.UserId.Equals(session2.UserId));
        }

        [Fact]
        public async void UpdateStatus_NoStatus_HasStatus()
        {
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.SetCanceled());
            canceller.CancelAfter(Timeout);
            _socket.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            _socket.ReceivedError += e => completer.TrySetException(e);
            await _socket.ConnectAsync(session);

            await _socket.UpdateStatusAsync("super status change!");
            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session.UserId));
        }
    }
}