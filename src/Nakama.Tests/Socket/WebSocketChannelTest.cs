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
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TinyJson;

    [TestFixture]
    public class WebSocketChannelTest
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
        public async Task ShouldCreateRoomChannel()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.Connect(session);
            var channelJoin = new ChannelJoinMessage("myroom", ChannelType.Room);
            var channel = await _socket.SendAsync(channelJoin);

            Assert.NotNull(channel);
            Assert.NotNull(channel.Id);
            Assert.AreEqual(channel.Self.UserId, session.UserId);
            Assert.AreEqual(channel.Self.Username, session.Username);
        }

        [Test]
        public async Task ShouldSendMessageRoomChannel()
        {
            _socket.Trace = true;
            _socket.Logger = new SystemConsoleLogger();

            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            IApiChannelMessage message = null;
            var evt = new AutoResetEvent(false);
            _socket.OnChannelMessage = chatMessage =>
            {
                Console.WriteLine("here");
                message = chatMessage;
                evt.Set();
            };
            await _socket.Connect(session);

            // Join channel.
            var channelJoin = new ChannelJoinMessage("myroom", ChannelType.Room);
            var channel = await _socket.SendAsync(channelJoin);

            // Send chat message.
            var content = new Dictionary<string, string> {{"hello", "world"}}.ToJson();
            var channelSend = new ChannelSendMessage(channel, content);
            var sendAck = await _socket.SendAsync(channelSend);

            var evtAck = evt.WaitOne(TimeSpan.FromSeconds(15));
            Console.WriteLine(evtAck);
            Assert.IsTrue(evtAck);
            Assert.NotNull(sendAck);
            Assert.NotNull(message);
            Assert.AreEqual(sendAck.ChannelId, message.ChannelId);
            Assert.AreEqual(sendAck.MessageId, message.MessageId);
            Assert.AreEqual(sendAck.Username, message.Username);
        }
    }
}
