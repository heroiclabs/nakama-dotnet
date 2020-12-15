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

    public class WebSocketChannelTest
    {
        private IClient _client;

        public WebSocketChannelTest()
        {
            _client = ClientUtil.FromSettingsFile();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldCreateRoomChannel(TestAdapterFactory adapterFactory)
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket = Nakama.Socket.From(_client, adapterFactory());

            await socket.ConnectAsync(session);
            var channel = await socket.JoinChatAsync("myroom", ChannelType.Room);

            Assert.NotNull(channel);
            Assert.NotNull(channel.Id);
            Assert.Equal(channel.Self.UserId, session.UserId);
            Assert.Equal(channel.Self.Username, session.Username);

            await socket.CloseAsync();
        }


        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldSendMessageRoomChannel(TestAdapterFactory adapterFactory)
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket = Nakama.Socket.From(_client, adapterFactory());

            var completer = new TaskCompletionSource<IApiChannelMessage>();
            socket.ReceivedChannelMessage += (chatMessage) => completer.SetResult(chatMessage);
            await socket.ConnectAsync(session);
            var channel = await socket.JoinChatAsync("myroom", ChannelType.Room);

            // Send chat message.
            var content = new Dictionary<string, string> {{"hello", "world"}}.ToJson();
            var sendAck = await socket.WriteChatMessageAsync(channel, content);
            var message = await completer.Task.ConfigureAwait(false);

            Assert.NotNull(sendAck);
            Assert.NotNull(message);
            Assert.Equal(sendAck.ChannelId, message.ChannelId);
            Assert.Equal(sendAck.MessageId, message.MessageId);
            Assert.Equal(sendAck.Username, message.Username);

            await socket.CloseAsync();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldSendMessageDirectChannel(TestAdapterFactory adapterFactory)
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            await _client.AddFriendsAsync(session1, new[] {session2.UserId});
            await _client.AddFriendsAsync(session2, new[] {session1.UserId});

            var socket1 = Nakama.Socket.From(_client, adapterFactory());

            var completer = new TaskCompletionSource<IApiChannelMessage>();
            socket1.ReceivedChannelMessage += (chatMessage) => completer.SetResult(chatMessage);
            await socket1.ConnectAsync(session1);

            var socket2 = Nakama.Socket.From(_client, adapterFactory());
            await socket2.ConnectAsync(session2);
            var channel = await socket1.JoinChatAsync(session2.UserId, ChannelType.DirectMessage, false, false);

            // Send chat message.
            var content = new Dictionary<string, string> {{"hello", "world"}}.ToJson();
            var sendAck = await socket1.WriteChatMessageAsync(channel, content);
            var message = await completer.Task.ConfigureAwait(false);

            Assert.NotNull(sendAck);
            Assert.NotNull(message);
            Assert.Equal(sendAck.ChannelId, message.ChannelId);
            Assert.Equal(sendAck.MessageId, message.MessageId);
            Assert.Equal(sendAck.Username, message.Username);

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }
    }
}
