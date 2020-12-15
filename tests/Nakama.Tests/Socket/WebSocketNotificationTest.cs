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

    public class WebSocketNotificationTest
    {
        private IClient _client;

        public WebSocketNotificationTest()
        {
            _client = ClientUtil.FromSettingsFile();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldReceiveNotification(TestAdapterFactory adapterFactory)
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var completer = new TaskCompletionSource<IApiNotification>();

            var socket = Nakama.Socket.From(_client, adapterFactory());

            socket.ReceivedNotification += (notification) => completer.SetResult(notification);
            await socket.ConnectAsync(session);

            var payload = new Dictionary<string, string> {{"user_id", session.UserId}};
            var _ = _client.RpcAsync(session, "clientrpc.send_notification", payload.ToJson());

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Equal(session.UserId, result.SenderId);

            await socket.CloseAsync();
        }
    }
}
