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

using System;

namespace Nakama.Tests.Socket
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class WebSocketTest
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

        [Test]
        public void ShouldCreateSocket()
        {
            var client = new Client();
            var socket = client.CreateWebSocket(5);

            Assert.NotNull(socket);
            Assert.AreSame(client.Logger, socket.Logger);
            Assert.AreEqual(5, socket.Reconnect);
        }

        [Test]
        public async Task ShouldCreateSocketAndConnect()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var evt = new AutoResetEvent(false);
            _socket.OnConnect = () => { evt.Set(); };
            await _socket.ConnectAsync(session);

            Assert.NotNull(evt);
            Assert.IsTrue(evt.WaitOne(TimeSpan.FromSeconds(2)));
        }

        [Test]
        public async Task ShouldCreateSocketAndDisconnect()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var evt = new AutoResetEvent(false);
            _socket.OnDisconnect = () => { evt.Set(); };
            await _socket.ConnectAsync(session);
            await _socket.DisconnectAsync();

            Assert.NotNull(evt);
            Assert.IsTrue(evt.WaitOne(TimeSpan.FromSeconds(2)));
        }

        [Test]
        public async Task ShouldCreateSocketAndDisconnectSilent()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var evt = new AutoResetEvent(false);
            _socket.OnDisconnect = () => { evt.Set(); };
            await _socket.ConnectAsync(session);
            await _socket.DisconnectAsync(false);

            Assert.NotNull(evt);
            Assert.IsFalse(evt.WaitOne(TimeSpan.FromSeconds(2)));
        }

        [Test]
        public async Task ShouldCreateSocketAndDisconnectNoConnect()
        {
            var evt = new AutoResetEvent(false);
            _socket.OnDisconnect = () => { evt.Set(); };
            await _socket.DisconnectAsync();

            Assert.DoesNotThrowAsync(() => _socket.DisconnectAsync());
            Assert.IsTrue(evt.WaitOne(TimeSpan.FromSeconds(2)));
        }
    }
}
