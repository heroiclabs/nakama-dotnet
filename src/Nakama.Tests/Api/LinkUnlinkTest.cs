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
    using System.Net.Http;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class LinkUnlinkTest
    {
        private IClient _client;

        // ReSharper disable RedundantArgumentDefaultValue
        [SetUp]
        public void SetUp()
        {
            _client = new Client("defaultkey", "127.0.0.1", 7350, false);
        }

        [Test]
        public async Task ShouldLinkCustomId()
        {
            var customid1 = Guid.NewGuid().ToString();
            var original = await _client.AuthenticateCustomAsync(customid1);
            var customid2 = Guid.NewGuid().ToString();
            await _client.LinkCustomAsync(original, customid2);
            var updated = await _client.AuthenticateCustomAsync(customid2);

            Assert.NotNull(original);
            Assert.NotNull(updated);
            Assert.AreEqual(original.UserId, updated.UserId);
            Assert.AreEqual(original.Username, updated.Username);
        }

        [Test]
        public async Task ShouldLinkCustomIdSame()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);
            await _client.LinkCustomAsync(session, customid);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.AreEqual(session.UserId, account.User.Id);
            Assert.AreEqual(session.Username, account.User.Username);
        }

        [Test]
        public async Task ShouldLinkCustomIdFieldEmpty()
        {
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);
            var customid = Guid.NewGuid().ToString();
            await _client.LinkCustomAsync(session, customid);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.That(account.Devices.Count(d => d.Id == deviceid), Is.EqualTo(1));
            Assert.AreEqual(customid, account.CustomId);
        }

        [Test]
        public async Task ShouldUnlinkCustomId()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);
            var deviceid = Guid.NewGuid().ToString();
            await _client.LinkDeviceAsync(session, deviceid);
            await _client.UnlinkCustomAsync(session, customid);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.That(account.Devices.Count(d => d.Id == deviceid), Is.EqualTo(1));
            Assert.IsNull(account.CustomId);
        }

        [Test]
        public async Task ShouldNotLinkCustomIdInuse()
        {
            var customid = Guid.NewGuid().ToString();
            await _client.AuthenticateCustomAsync(customid);
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.LinkCustomAsync(session, customid));
            Assert.AreEqual("409 (Conflict)", ex.Message);
        }

        [Test]
        public async Task ShouldNotUnlinkCustomId()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.UnlinkCustomAsync(session, customid));
            Assert.AreEqual("403 (Forbidden)", ex.Message);
        }

        [Test]
        public async Task ShouldNotUnlinkCustomIdNotOwned()
        {
            var customid = Guid.NewGuid().ToString();
            await _client.AuthenticateCustomAsync(customid);
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.UnlinkCustomAsync(session, customid));
            Assert.AreEqual("403 (Forbidden)", ex.Message);
        }

        [Test]
        public async Task ShouldLinkDeviceId()
        {
            var deviceid1 = Guid.NewGuid().ToString();
            var original = await _client.AuthenticateDeviceAsync(deviceid1);
            var deviceid2 = Guid.NewGuid().ToString();
            await _client.LinkDeviceAsync(original, deviceid2);
            var updated = await _client.AuthenticateDeviceAsync(deviceid2);

            Assert.NotNull(original);
            Assert.NotNull(updated);
            Assert.AreEqual(original.UserId, updated.UserId);
            Assert.AreEqual(original.Username, updated.Username);
        }

        [Test]
        public async Task ShouldLinkDeviceIdSame()
        {
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);
            await _client.LinkDeviceAsync(session, deviceid);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.AreEqual(session.UserId, account.User.Id);
            Assert.AreEqual(session.Username, account.User.Username);
        }

        [Test]
        public async Task ShouldNotLinkDeviceIdInuse()
        {
            var deviceid = Guid.NewGuid().ToString();
            await _client.AuthenticateDeviceAsync(deviceid);
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.LinkDeviceAsync(session, deviceid));
            Assert.AreEqual("409 (Conflict)", ex.Message);
        }

        [Test]
        public async Task ShouldUnlinkDeviceId()
        {
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);
            await _client.LinkDeviceAsync(session, Guid.NewGuid().ToString());
            await _client.UnlinkDeviceAsync(session, deviceid);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.That(account.Devices.Count(d => d.Id == deviceid), Is.EqualTo(0));
        }

        [Test]
        public async Task ShouldNotUnlinkDeviceId()
        {
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.UnlinkDeviceAsync(session, deviceid));
            Assert.AreEqual("403 (Forbidden)", ex.Message);
        }

        [Test]
        public async Task ShouldNotUnlinkDeviceIdNotOwned()
        {
            var deviceid1 = Guid.NewGuid().ToString();
            await _client.AuthenticateDeviceAsync(deviceid1);
            var deviceid2 = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid2);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.UnlinkDeviceAsync(session, deviceid1));
            Assert.AreEqual("403 (Forbidden)", ex.Message);
        }

        [Test]
        public async Task ShouldLinkEmail()
        {
            var customid = Guid.NewGuid().ToString();
            var original = await _client.AuthenticateCustomAsync(customid);
            var email = string.Format("{0}@{0}.com", Guid.NewGuid().ToString());
            const string password = "newpassword";
            await _client.LinkEmailAsync(original, email, password);
            var updated = await _client.AuthenticateEmailAsync(email, password);

            Assert.NotNull(original);
            Assert.NotNull(updated);
            Assert.AreEqual(original.UserId, updated.UserId);
            Assert.AreEqual(original.Username, updated.Username);
        }

        [Test]
        public async Task ShouldLinkEmailSame()
        {
            var email = string.Format("{0}@{0}.com", Guid.NewGuid().ToString());
            const string password = "newpassword";
            var session = await _client.AuthenticateEmailAsync(email, password);
            await _client.LinkEmailAsync(session, email, password);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.AreEqual(session.UserId, account.User.Id);
            Assert.AreEqual(session.Username, account.User.Username);
        }

        [Test]
        public async Task ShouldNotLinkEmailInuse()
        {
            var email = string.Format("{0}@{0}.com", Guid.NewGuid().ToString());
            const string password = "newpassword";
            await _client.AuthenticateEmailAsync(email, password);
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.LinkEmailAsync(session, email, password));
            Assert.AreEqual("409 (Conflict)", ex.Message);
        }

        [Test]
        public async Task ShouldUnlinkEmail()
        {
            var email = string.Format("{0}@{0}.com", Guid.NewGuid().ToString());
            const string password = "newpassword";
            var session = await _client.AuthenticateEmailAsync(email, password);
            var customid = Guid.NewGuid().ToString();
            await _client.LinkCustomAsync(session, customid);
            await _client.UnlinkEmailAsync(session, email, password);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.AreNotEqual(email, account.Email);
        }

        [Test]
        public async Task ShouldNotUnlinkEmail()
        {
            var email = string.Format("{0}@{0}.com", Guid.NewGuid().ToString());
            const string password = "newpassword";
            var session = await _client.AuthenticateEmailAsync(email, password);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.UnlinkEmailAsync(session, email, password));
            Assert.AreEqual("403 (Forbidden)", ex.Message);
        }

        [Test]
        public async Task ShouldNotUnlinkEmailNotOwned()
        {
            var email = string.Format("{0}@{0}.com", Guid.NewGuid().ToString());
            const string password = "newpassword";
            await _client.AuthenticateEmailAsync(email, password);
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.UnlinkEmailAsync(session, email, password));
            Assert.AreEqual("403 (Forbidden)", ex.Message);
        }

        [Test]
        public async Task ShouldNotLinkFacebook()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.LinkFacebookAsync(session, "invalid"));
            Assert.AreEqual("401 (Unauthorized)", ex.Message);
        }

        [Test]
        public async Task ShouldNotUnlinkFacebook()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.UnlinkFacebookAsync(session, "invalid"));
            Assert.AreEqual("401 (Unauthorized)", ex.Message);
        }

        [Test]
        public async Task ShouldNotLinkGameCenter()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            const string bundleId = "a";
            const string playerId = "b";
            const string publicKeyUrl = "c";
            const string salt = "d";
            const string signature = "e";
            const string timestamp = "f";

            var ex = Assert.ThrowsAsync<HttpRequestException>(() =>
                _client.LinkGameCenterAsync(session, bundleId, playerId, publicKeyUrl, salt, signature, timestamp));
            Assert.AreEqual("400 (Bad Request)", ex.Message);
        }

        [Test]
        public async Task ShouldNotLinkGameCenterBadInput()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var bundleId = string.Empty;
            var playerId = string.Empty;
            var publicKeyUrl = string.Empty;
            var salt = string.Empty;
            var signature = string.Empty;
            var timestamp = string.Empty;

            var ex = Assert.ThrowsAsync<HttpRequestException>(() =>
                _client.LinkGameCenterAsync(session, bundleId, playerId, publicKeyUrl, salt, signature, timestamp));
            Assert.AreEqual("400 (Bad Request)", ex.Message);
        }

        [Test]
        public async Task ShouldNotUnlinkGameCenterBadInput()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var bundleId = string.Empty;
            var playerId = string.Empty;
            var publicKeyUrl = string.Empty;
            var salt = string.Empty;
            var signature = string.Empty;
            var timestamp = string.Empty;

            var ex = Assert.ThrowsAsync<HttpRequestException>(() =>
                _client.UnlinkGameCenterAsync(session, bundleId, playerId, publicKeyUrl, salt, signature, timestamp));
            Assert.AreEqual("400 (Bad Request)", ex.Message);
        }

        [Test]
        public async Task ShouldNotLinkGoogle()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.LinkGoogleAsync(session, "invalid"));
            Assert.AreEqual("401 (Unauthorized)", ex.Message);
        }

        [Test]
        public async Task ShouldNotUnlinkGoogle()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.UnlinkGoogleAsync(session, "invalid"));
            Assert.AreEqual("401 (Unauthorized)", ex.Message);
        }

        [Test]
        public async Task ShouldNotLinkSteam()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.LinkSteamAsync(session, "invalid"));
            Assert.AreEqual("412 (Precondition Failed)", ex.Message);
        }

        [Test]
        public async Task ShouldNotUnlinkSteam()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.UnlinkSteamAsync(session, "invalid"));
            Assert.AreEqual("412 (Precondition Failed)", ex.Message);
        }
    }
}
