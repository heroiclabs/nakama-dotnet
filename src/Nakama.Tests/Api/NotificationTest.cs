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

namespace Nakama.Tests.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TinyJson;

    // NOTE: Requires Lua modules from server repo.

    [TestFixture]
    public class NotificationTest
    {
        private IClient _client;

        // ReSharper disable RedundantArgumentDefaultValue
        [SetUp]
        public void SetUp()
        {
            _client = new Client("defaultkey", "127.0.0.1", 7350, false);
        }

        [Test]
        public async Task ShouldDeleteNotificationsEmpty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            Assert.DoesNotThrowAsync(() => _client.DeleteNotificationsAsync(session, new string[0]));
        }

        [Test]
        public async Task ShouldDeleteNotifications()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            // Must create a notification.
            var payload = new Dictionary<string, string>
            {
                {"user_id", session.UserId}
            };
            await _client.RpcAsync(session, "clientrpc.send_notification", payload.ToJson());

            // List and delete.
            var result1 = await _client.ListNotificationsAsync(session, 1);
            Assert.NotNull(result1);
            var notification = result1.Notifications.First();
            await _client.DeleteNotificationsAsync(session, new[] {notification.Id});

            var result2 = await _client.ListNotificationsAsync(session, 100);
            Assert.NotNull(result2);
            Assert.That(result2.Notifications, Is.Empty);
        }

        [Test]
        public async Task ShouldListNotificationsEmpty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var result = await _client.ListNotificationsAsync(session);

            Assert.NotNull(result);
            Assert.That(result.Notifications, Is.Empty);
            Assert.IsNotNull(result.CacheableCursor);
        }

        [Test]
        public async Task ShouldListNotifications()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            // Must create a notification.
            var payload = new Dictionary<string, string>
            {
                {"user_id", session.UserId}
            };
            await _client.RpcAsync(session, "clientrpc.send_notification", payload.ToJson());

            var result = await _client.ListNotificationsAsync(session, 1);

            Assert.NotNull(result);
            Assert.That(result.Notifications.Count(n => n.SenderId == session.UserId), Is.EqualTo(1));
            var notification = result.Notifications.First();
            Assert.IsTrue(notification.Persistent);
            Assert.NotNull(notification.CreateTime);
            Assert.AreEqual(1, notification.Code);
            Assert.AreEqual("{\"reward_coins\": 1000}", notification.Content);
            Assert.AreEqual("You've unlocked level 100!", notification.Subject);
        }

        [Test]
        public async Task ShouldListNotificationsWithCursor()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            // Must create notifications.
            var payload = new Dictionary<string, string>
            {
                {"user_id", session.UserId}
            }.ToJson();
            await _client.RpcAsync(session, "clientrpc.send_notification", payload);
            await _client.RpcAsync(session, "clientrpc.send_notification", payload);
            await _client.RpcAsync(session, "clientrpc.send_notification", payload);

            var result1 = await _client.ListNotificationsAsync(session, 1);
            Assert.NotNull(result1);
            Assert.NotNull(result1.CacheableCursor);
            var result2 = await _client.ListNotificationsAsync(session, 1, result1.CacheableCursor);
            Assert.That(result2.Notifications.Count(n => result1.Notifications.First().Id != n.Id), Is.EqualTo(2));
        }
    }
}
