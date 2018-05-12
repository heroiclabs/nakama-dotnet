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
    public class AuthenticateTest
    {
        private IClient _client;

        // ReSharper disable RedundantArgumentDefaultValue
        [SetUp]
        public void SetUp()
        {
            _client = new Client("defaultkey", "127.0.0.1", 7350, false);
        }

        [Test]
        public async Task ShouldAuthenticateCustomId()
        {
            var customid = Guid.NewGuid();
            var session = await _client.AuthenticateCustomAsync(customid.ToString());

            Assert.IsNotNull(session);
            Assert.IsNotNull(session.UserId);
            Assert.IsNotNull(session.Username);
            Assert.IsFalse(session.IsExpired);
        }

        [Test]
        public async Task ShouldAuthenticateDeviceId()
        {
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);

            Assert.IsNotNull(session);
            Assert.IsNotNull(session.UserId);
            Assert.IsNotNull(session.Username);
            Assert.IsFalse(session.IsExpired);

            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.That(account.Devices.Count(d => d.Id == deviceid), Is.EqualTo(1));
        }

        [Test]
        public async Task ShouldAuthenticateEmail()
        {
            var session = await _client.AuthenticateEmailAsync("super@heroes.com", "batsignal");

            Assert.IsNotNull(session);
            Assert.IsNotNull(session.UserId);
            Assert.IsNotNull(session.Username);
            Assert.IsFalse(session.IsExpired);
        }

        [Test]
        public void ShouldNotAuthenticateFacebook()
        {
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.AuthenticateFacebookAsync("invalid"));

            Assert.AreEqual("401 (Unauthorized)", ex.Message);
        }

        [Test]
        public void ShouldNotAuthenticateGameCenter()
        {
            var bundleId = string.Empty;
            var playerId = string.Empty;
            var publicKeyUrl = string.Empty;
            var salt = string.Empty;
            var signature = string.Empty;
            var timestamp = string.Empty;

            var ex = Assert.ThrowsAsync<HttpRequestException>(() =>
                _client.AuthenticateGameCenterAsync(bundleId, playerId, publicKeyUrl, salt, signature, timestamp));

            Assert.AreEqual("400 (Bad Request)", ex.Message);
        }

        [Test]
        public void ShouldNotAuthenticateGoogle()
        {
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.AuthenticateGoogleAsync("invalid"));

            Assert.AreEqual("401 (Unauthorized)", ex.Message);
        }

        [Test]
        public void ShouldNotAuthenticateSteam()
        {
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.AuthenticateSteamAsync("invalid"));

            // Precondition failed because Steam requires special configuration with the server.
            Assert.AreEqual("412 (Precondition Failed)", ex.Message);
        }
    }
}
