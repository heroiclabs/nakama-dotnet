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

using System.Diagnostics;

namespace Nakama.Tests.Socket
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class WebSocketUserStatusTest
    {
        private IClient _client;
        private ISocket _socket;

        // ReSharper disable RedundantArgumentDefaultValue
        [SetUp]
        public void SetUp()
        {
            _client = new Client("defaultkey", "127.0.0.1", 7350, false);
            _socket = _client.CreateWebSocket();
        }

        [TearDown]
        public async Task TearDown()
        {
            await _socket.DisconnectAsync(false);
        }

        [Test]
        public async Task ShouldUpdateStatus()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            _socket.OnStatusPresence += (sender, presence) => completer.SetResult(presence);
            await _socket.ConnectAsync(session);
            await _socket.UpdateStatusAsync("online");

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.That(result.Joins.Count(p => p.UserId.Equals(session.UserId)), Is.EqualTo(1));
        }

        [Test]
        public async Task ShouldFollowStatusUpdate()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            _socket.OnStatusPresence += (sender, presence) => completer.SetResult(presence);
            await _socket.ConnectAsync(session1);
            await _socket.FollowUsersAsync(new[] {session2.UserId});

            var socket2 = _client.CreateWebSocket();
            await socket2.ConnectAsync(session2, default(CancellationToken), true);
            await socket2.UpdateStatusAsync("online");

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.That(result.Joins.Count(p => p.UserId.Equals(session2.UserId)), Is.EqualTo(1));

            await socket2.DisconnectAsync();
        }

        [Test]
        public async Task ShouldFollowStatusJoinAndLeaveEventSeen()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var logger = new SystemConsoleLogger();
            _client.Logger = logger;
            _client.Trace = true;

            var socket1 = _client.CreateWebSocket();
            socket1.OnStatusPresence += (_, presence) =>
            {
                foreach (var join in presence.Joins)
                {
                    Console.WriteLine("User id '{0}' name '{1}' and status '{2}'.", join.UserId, join.Username, join.Status);
                }

                foreach (var leave in presence.Leaves)
                {
                    Console.WriteLine("User id '{0}' name '{1}' and status '{2}'.", leave.UserId, leave.Username, leave.Status);
                }
            };

            var socket2 = _client.CreateWebSocket();
            socket2.OnStatusPresence += (_, presence) =>
            {
                foreach (var join in presence.Joins)
                {
                    Console.WriteLine("User id '{0}' name '{1}' and status '{2}'.", join.UserId, join.Username, join.Status);
                }

                foreach (var leave in presence.Leaves)
                {
                    Console.WriteLine("User id '{0}' name '{1}' and status '{2}'.", leave.UserId, leave.Username, leave.Status);
                }
            };

            await socket1.ConnectAsync(session1, default(CancellationToken), true);
            await socket1.FollowUsersAsync(new[] {session2.UserId, session2.UserId, session1.UserId});
            await socket1.UpdateStatusAsync("online");
            await socket2.ConnectAsync(session2, default(CancellationToken), true);
            await socket2.FollowUsersAsync(new[] {session1.UserId, session1.UserId, session2.UserId});
            await socket2.UpdateStatusAsync("online");

            await socket2.DisconnectAsync(true);
            Thread.Sleep(5000);
            await socket1.DisconnectAsync(true);
        }
    }
}
