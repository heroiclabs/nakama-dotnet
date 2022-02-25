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
    using System.Threading.Tasks;
    using Xunit;
    using TinyJson;

    public class WebSocketChannelTest : IAsyncLifetime
    {
        private IClient _client;
        private ISocket _socket;

        public WebSocketChannelTest()
        {
            _client = TestsUtil.FromSettingsFile();
            _socket = Nakama.Socket.From(_client);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldCreateRoomChannel()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.ConnectAsync(session);
            var channel = await _socket.JoinChatAsync("myroom", ChannelType.Room);

            Assert.NotNull(channel);
            Assert.NotNull(channel.Id);
            Assert.Equal(channel.Self.UserId, session.UserId);
            Assert.Equal(channel.Self.Username, session.Username);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldSendMessageRoomChannel()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var completer = new TaskCompletionSource<IApiChannelMessage>();
            _socket.ReceivedChannelMessage += (chatMessage) => completer.SetResult(chatMessage);
            await _socket.ConnectAsync(session);
            var channel = await _socket.JoinChatAsync("myroom", ChannelType.Room);

            // Send chat message.
            var content = new Dictionary<string, string> {{"hello", "world"}}.ToJson();
            var sendAck = await _socket.WriteChatMessageAsync(channel, content);
            var message = await completer.Task.ConfigureAwait(false);

            Assert.NotNull(sendAck);
            Assert.NotNull(message);
            Assert.Equal(sendAck.ChannelId, message.ChannelId);
            Assert.Equal(sendAck.MessageId, message.MessageId);
            Assert.Equal(sendAck.Username, message.Username);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldSendMessageDirectChannel()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            await _client.AddFriendsAsync(session1, new[] {session2.UserId});
            await _client.AddFriendsAsync(session2, new[] {session1.UserId});

            var completer = new TaskCompletionSource<IApiChannelMessage>();
            _socket.ReceivedChannelMessage += (chatMessage) => completer.SetResult(chatMessage);
            await _socket.ConnectAsync(session1);

            var socket2 = Nakama.Socket.From(_client);
            await socket2.ConnectAsync(session2);
            var channel = await _socket.JoinChatAsync(session2.UserId, ChannelType.DirectMessage, false, false);

            // Send chat message.
            var content = new Dictionary<string, string> {{"hello", "world"}}.ToJson();
            var sendAck = await _socket.WriteChatMessageAsync(channel, content);
            var message = await completer.Task.ConfigureAwait(false);

            Assert.NotNull(sendAck);
            Assert.NotNull(message);
            Assert.Equal(sendAck.ChannelId, message.ChannelId);
            Assert.Equal(sendAck.MessageId, message.MessageId);
            Assert.Equal(sendAck.Username, message.Username);
        }

        Task IAsyncLifetime.InitializeAsync()
        {
            return Task.CompletedTask;
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            return _socket.CloseAsync();
        }
    }
}
