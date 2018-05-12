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
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

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
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);
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
    }
}
