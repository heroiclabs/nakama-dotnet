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

using System;
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests.Socket
{
    public class AwaitedSocketTaskTest : IDisposable
    {
        private IClient _client;

        public AwaitedSocketTaskTest()
        {
            _client = ClientUtil.FromSettingsFile();
        }

        public void Dispose() { _client = null; }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async void Socket_AwaitedTasks_AreCanceled(TestAdapterFactory adapterFactory)
        {
            var socket = Nakama.Socket.From(_client, adapterFactory());
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);
            await socket.ConnectAsync(session);

            var matchmakerTask1 = socket.AddMatchmakerAsync("+label.foo:\"val\"", 15, 20);
            var matchmakerTask2 = socket.AddMatchmakerAsync("+label.bar:\"val\"", 15, 20);
            await socket.CloseAsync();

            await Assert.ThrowsAsync<TaskCanceledException>(() => Task.WhenAll(matchmakerTask1, matchmakerTask2));
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async void Socket_AwaitedTasksAfterDisconnect_AreCanceled(TestAdapterFactory adapterFactory)
        {
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);
            var socket = Nakama.Socket.From(_client, adapterFactory());

            await socket.ConnectAsync(session);
            await socket.CloseAsync();

            var statusTask1 = socket.FollowUsersAsync(new[] {session.UserId});
            var statusTask2 = socket.FollowUsersAsync(new[] {session.UserId});

            await Assert.ThrowsAsync<TaskCanceledException>(() => Task.WhenAll(statusTask1, statusTask2));
        }
    }
}
