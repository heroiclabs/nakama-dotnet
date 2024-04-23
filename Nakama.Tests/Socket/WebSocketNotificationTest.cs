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
    using System.Linq;
    using System.Runtime.CompilerServices;

    public class WebSocketNotificationTest : IAsyncLifetime
    {
        private IClient _client;
        private ISocket _socket;

        public WebSocketNotificationTest()
        {
            _client = TestsUtil.FromSettingsFile();
            _socket = Nakama.Socket.From(_client);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldReceiveNotification()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var completer = new TaskCompletionSource<IApiNotification>();
            _socket.ReceivedNotification += (notification) => completer.SetResult(notification);
            await _socket.ConnectAsync(session);

            var payload = new Dictionary<string, string> {{"user_id", session.UserId}};
            var _ = _client.RpcAsync(session, "clientrpc.send_notification", payload.ToJson());

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Equal(session.UserId, result.SenderId);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldObtainDifferentCursors()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var payload = new Dictionary<string, string> {{"user_id", session.UserId}};
            for (int i = 0; i < 10; i++)
            {
                var _ = await _client.RpcAsync(session, "clientrpc.send_notification", payload.ToJson());
            }
            IApiNotificationList notifs = await _client.ListNotificationsAsync(session, limit: 9);
            string firstCursor = notifs.CacheableCursor;
            Assert.Equal(9, notifs.Notifications.Count());
            Assert.NotEmpty(firstCursor);

            notifs = await _client.ListNotificationsAsync(session, limit: 10, cacheableCursor: firstCursor); // should only be one left

            Assert.Single(notifs.Notifications);
            Assert.NotEmpty(notifs.CacheableCursor);
            Assert.NotEqual(firstCursor, notifs.CacheableCursor);
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
