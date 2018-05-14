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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class UserTest
    {
        private IClient _client;

        // ReSharper disable RedundantArgumentDefaultValue
        [SetUp]
        public void SetUp()
        {
            _client = new Client("defaultkey", "127.0.0.1", 7350, false);
        }

        [Test]
        public async Task ShouldGetAccount()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.IsEmpty(account.Devices);
            Assert.AreEqual(customid, account.CustomId);
            Assert.NotNull(account.User);
            Assert.NotNull(account.User.Id);
            Assert.NotNull(account.User.Username);
            Assert.IsFalse(account.User.Online);
            Assert.Zero(account.User.EdgeCount);
            Assert.IsNotEmpty(account.User.CreateTime);
            Assert.IsNotEmpty(account.User.UpdateTime);
        }

        [Test]
        public async Task ShouldUpdateAccount()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            Assert.NotNull(session);

            var username = Guid.NewGuid().ToString();
            const string displayName = "Superman";
            const string avatarUrl = "http://graph.facebook.com/892489324234/picture?type=square";
            const string langTag = "en_US";
            const string location = "San Francisco, CA.";
            const string timezone = "Pacific Time (US & Canada)";
            await _client.UpdateAccountAsync(session, username, displayName, avatarUrl, langTag, location, timezone);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.NotNull(account.User);
            Assert.AreEqual(username, account.User.Username);
            Assert.AreEqual(displayName, account.User.DisplayName);
            Assert.AreEqual(avatarUrl, account.User.AvatarUrl);
            Assert.AreEqual(langTag, account.User.LangTag);
            Assert.AreEqual(location, account.User.Location);
            Assert.AreEqual(timezone, account.User.Timezone);
        }

        [Test]
        public async Task ShouldUpdateAccountUsername()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var original = await _client.GetAccountAsync(session);

            // Must sleep because update_time is in UTC (seconds).
            Thread.Sleep(TimeSpan.FromSeconds(1));

            var username = Guid.NewGuid().ToString();
            await _client.UpdateAccountAsync(session, username);
            var updated = await _client.GetAccountAsync(session);

            Assert.NotNull(original);
            Assert.NotNull(updated);
            Assert.NotNull(original.User);
            Assert.NotNull(updated.User);
            Assert.AreEqual(original.User.AvatarUrl, updated.User.AvatarUrl);
            Assert.AreEqual(original.User.CreateTime, updated.User.CreateTime);
            Assert.AreEqual(original.User.DisplayName, updated.User.DisplayName);
            Assert.AreEqual(original.User.LangTag, updated.User.LangTag);
            Assert.AreEqual(original.User.Location, updated.User.Location);
            Assert.AreEqual(original.User.Timezone, updated.User.Timezone);
            Assert.AreNotEqual(original.User.UpdateTime, updated.User.UpdateTime);
            Assert.AreEqual(username, updated.User.Username);
            Assert.AreNotEqual(username, original.User.Username);
        }

        [Test]
        public async Task ShouldGetUsersEmpty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var result = await _client.GetUsersAsync(session, null);

            Assert.NotNull(result);
            Assert.IsEmpty(result.Users);
        }

        [Test]
        public async Task ShouldGetUsersSelf()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var result = await _client.GetUsersAsync(session, new[] {session.UserId});

            Assert.NotNull(result);
            Assert.That(result.Users.Count(u => u.Id == session.UserId), Is.EqualTo(1));
        }

        [Test]
        public async Task ShouldGetUsersTwo()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var result = await _client.GetUsersAsync(session1, new[] {session1.UserId}, new[] {session2.Username});

            Assert.NotNull(result);
            Assert.That(result.Users, Has.Count.EqualTo(2));
            Assert.That(result.Users.Count(u => u.Id == session1.UserId || u.Id == session2.UserId), Is.EqualTo(2));
        }

        [Test]
        public async Task ShouldListUserGroupsEmpty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var result = await _client.ListUserGroupsAsync(session);

            Assert.NotNull(result);
            Assert.IsEmpty(result.UserGroups);
        }
    }
}
