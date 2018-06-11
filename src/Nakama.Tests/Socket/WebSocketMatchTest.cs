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

namespace Nakama.Tests.Socket
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class WebSocketMatchTest
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
        public async Task ShouldCreateMatch()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.ConnectAsync(session);
            var match = await _socket.CreateMatchAsync();

            Assert.NotNull(match);
            Assert.NotNull(match.Id);
            Assert.IsNotEmpty(match.Id);
            Assert.False(match.Authoritative);
            Assert.NotZero(match.Size);
            Assert.Positive(match.Size);
        }

        [Test]
        public async Task ShouldCreateMatchAndSecondUserJoin()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.ConnectAsync(session1);
            var socket2 = _client.CreateWebSocket();
            await socket2.ConnectAsync(session2);

            var match1 = await _socket.CreateMatchAsync();
            var match2 = await socket2.JoinMatchAsync(match1.Id);

            Assert.NotNull(match1);
            Assert.NotNull(match2);
            Assert.AreEqual(match1.Id, match2.Id);
            Assert.AreEqual(match1.Label, match2.Label);
            Assert.That(match1.Presences, Is.EqualTo(1));
            Assert.That(match2.Presences, Is.EqualTo(2));

            await socket2.DisconnectAsync(false);
        }

        [Test]
        public async Task ShouldCreateMatchAndLeave()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.ConnectAsync(session);
            var match = await _socket.CreateMatchAsync();

            Assert.NotNull(match);
            Assert.NotNull(match.Id);
            Assert.DoesNotThrowAsync(() => _socket.LeaveMatchAsync(match.Id));
        }
    }
}
