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
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests
{
    public class AwaitedSocketTaskTest : IDisposable
    {
        private IClient _client;
        private readonly ISocket _socket;

        public AwaitedSocketTaskTest()
        {
            _client = TestsUtil.FromSettingsFile();
            _socket = Nakama.Socket.From(_client);
        }

        public void Dispose() => _client = null;

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async void Socket_AwaitedTasks_AreCanceled()
        {
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);
            await _socket.ConnectAsync(session);

            var matchmakerTask1 = _socket.AddMatchmakerAsync("+label.foo:\"val\"", 15, 20);
            var matchmakerTask2 = _socket.AddMatchmakerAsync("+label.bar:\"val\"", 15, 20);
            await _socket.CloseAsync();

            await Assert.ThrowsAsync<TaskCanceledException>(() => Task.WhenAll(matchmakerTask1, matchmakerTask2));
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async void Socket_AwaitedTasksAfterDisconnect_ThrowException()
        {
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);
            await _socket.ConnectAsync(session);

            await _socket.CloseAsync();
            var statusTask1 = _socket.FollowUsersAsync(new[] {session.UserId});
            var statusTask2 = _socket.FollowUsersAsync(new[] {session.UserId});

            await Assert.ThrowsAsync<SocketException>(() => Task.WhenAll(statusTask1, statusTask2));
        }
    }
}
