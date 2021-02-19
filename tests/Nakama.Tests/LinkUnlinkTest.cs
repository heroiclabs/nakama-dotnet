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

namespace Nakama.Tests.Api
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    public class LinkUnlinkTest
    {
        private IClient _client;

        public LinkUnlinkTest()
        {
            _client = new Client("http", "127.0.0.1", 7350, "defaultkey");
        }

        [Fact]
        public async Task ShouldLinkCustomId()
        {
            var customid1 = Guid.NewGuid().ToString();
            var original = await _client.AuthenticateCustomAsync(customid1);
            var customid2 = Guid.NewGuid().ToString();
            await _client.LinkCustomAsync(original, customid2);
            var updated = await _client.AuthenticateCustomAsync(customid2);

            Assert.NotNull(original);
            Assert.NotNull(updated);
            Assert.Equal(original.UserId, updated.UserId);
            Assert.Equal(original.Username, updated.Username);
        }

        [Fact]
        public async Task ShouldLinkCustomIdSame()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);
            await _client.LinkCustomAsync(session, customid);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.Equal(session.UserId, account.User.Id);
            Assert.Equal(session.Username, account.User.Username);
        }

        [Fact]
        public async Task ShouldLinkCustomIdFieldEmpty()
        {
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);
            var customid = Guid.NewGuid().ToString();
            await _client.LinkCustomAsync(session, customid);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.Equal(account.Devices.Count(d => d.Id == deviceid), 1);
            Assert.Equal(customid, account.CustomId);
        }

        [Fact]
        public async Task ShouldUnlinkCustomId()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);
            var deviceid = Guid.NewGuid().ToString();
            await _client.LinkDeviceAsync(session, deviceid);
            await _client.UnlinkCustomAsync(session, customid);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.Equal(account.Devices.Count(d => d.Id == deviceid), 1);
            Assert.Null(account.CustomId);
        }

        [Fact]
        public async Task ShouldNotLinkCustomIdInuse()
        {
            var customid = Guid.NewGuid().ToString();
            await _client.AuthenticateCustomAsync(customid);
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.LinkCustomAsync(session, customid));
            Assert.Equal((int) HttpStatusCode.Conflict, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotUnlinkCustomId()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.UnlinkCustomAsync(session, customid));
            Assert.Equal((int) HttpStatusCode.Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotUnlinkCustomIdNotOwned()
        {
            var customid = Guid.NewGuid().ToString();
            await _client.AuthenticateCustomAsync(customid);
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.UnlinkCustomAsync(session, customid));
            Assert.Equal((int) HttpStatusCode.Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldLinkDeviceId()
        {
            var deviceid1 = Guid.NewGuid().ToString();
            var original = await _client.AuthenticateDeviceAsync(deviceid1);
            var deviceid2 = Guid.NewGuid().ToString();
            await _client.LinkDeviceAsync(original, deviceid2);
            var updated = await _client.AuthenticateDeviceAsync(deviceid2);

            Assert.NotNull(original);
            Assert.NotNull(updated);
            Assert.Equal(original.UserId, updated.UserId);
            Assert.Equal(original.Username, updated.Username);
        }

        [Fact]
        public async Task ShouldLinkDeviceIdSame()
        {
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);
            await _client.LinkDeviceAsync(session, deviceid);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.Equal(session.UserId, account.User.Id);
            Assert.Equal(session.Username, account.User.Username);
        }

        [Fact]
        public async Task ShouldNotLinkDeviceIdInuse()
        {
            var deviceid = Guid.NewGuid().ToString();
            await _client.AuthenticateDeviceAsync(deviceid);
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.LinkDeviceAsync(session, deviceid));
            Assert.Equal((int) HttpStatusCode.Conflict, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldUnlinkDeviceId()
        {
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);
            await _client.LinkDeviceAsync(session, Guid.NewGuid().ToString());
            await _client.UnlinkDeviceAsync(session, deviceid);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.Equal(account.Devices.Count(d => d.Id == deviceid), 0);
        }

        [Fact]
        public async Task ShouldNotUnlinkDeviceId()
        {
            var deviceid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.UnlinkDeviceAsync(session, deviceid));
            Assert.Equal((int) HttpStatusCode.Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotUnlinkDeviceIdNotOwned()
        {
            var deviceid1 = Guid.NewGuid().ToString();
            await _client.AuthenticateDeviceAsync(deviceid1);
            var deviceid2 = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateDeviceAsync(deviceid2);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.UnlinkDeviceAsync(session, deviceid1));
            Assert.Equal((int) HttpStatusCode.Forbidden, ex.StatusCode);
        }

        [Fact]
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
            Assert.Equal(original.UserId, updated.UserId);
            Assert.Equal(original.Username, updated.Username);
        }

        [Fact]
        public async Task ShouldLinkEmailSame()
        {
            var email = string.Format("{0}@{0}.com", Guid.NewGuid().ToString());
            const string password = "newpassword";
            var session = await _client.AuthenticateEmailAsync(email, password);
            await _client.LinkEmailAsync(session, email, password);
            var account = await _client.GetAccountAsync(session);

            Assert.NotNull(account);
            Assert.Equal(session.UserId, account.User.Id);
            Assert.Equal(session.Username, account.User.Username);
        }

        [Fact]
        public async Task ShouldNotLinkEmailInuse()
        {
            var email = string.Format("{0}@{0}.com", Guid.NewGuid().ToString());
            const string password = "newpassword";
            await _client.AuthenticateEmailAsync(email, password);
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.LinkEmailAsync(session, email, password));
            Assert.Equal((int) HttpStatusCode.Conflict, ex.StatusCode);
        }

        [Fact]
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
            Assert.NotEqual(email, account.Email);
        }

        [Fact]
        public async Task ShouldNotUnlinkEmail()
        {
            var email = string.Format("{0}@{0}.com", Guid.NewGuid().ToString());
            const string password = "newpassword";
            var session = await _client.AuthenticateEmailAsync(email, password);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.UnlinkEmailAsync(session, email, password));
            Assert.Equal((int) HttpStatusCode.Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotUnlinkEmailNotOwned()
        {
            var email = string.Format("{0}@{0}.com", Guid.NewGuid().ToString());
            const string password = "newpassword";
            await _client.AuthenticateEmailAsync(email, password);
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.UnlinkEmailAsync(session, email, password));
            Assert.Equal((int) HttpStatusCode.Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotLinkFacebook()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.LinkFacebookAsync(session, "invalid"));
            Assert.Equal((int) HttpStatusCode.Unauthorized, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotUnlinkFacebook()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.UnlinkFacebookAsync(session, "invalid"));
            Assert.Equal((int) HttpStatusCode.Unauthorized, ex.StatusCode);
        }

        [Fact]
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

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() =>
                _client.LinkGameCenterAsync(session, bundleId, playerId, publicKeyUrl, salt, signature, timestamp));
            Assert.Equal((int) HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Fact]
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

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() =>
                _client.LinkGameCenterAsync(session, bundleId, playerId, publicKeyUrl, salt, signature, timestamp));
            Assert.Equal((int) HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Fact]
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

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() =>
                _client.UnlinkGameCenterAsync(session, bundleId, playerId, publicKeyUrl, salt, signature, timestamp));
            Assert.Equal((int) HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotLinkGoogle()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.LinkGoogleAsync(session, "invalid"));
            Assert.Equal((int) HttpStatusCode.Unauthorized, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotUnlinkGoogle()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.UnlinkGoogleAsync(session, "invalid"));
            Assert.Equal((int) HttpStatusCode.Unauthorized, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotLinkSteam()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.LinkSteamAsync(session, "invalid", false));
            Assert.Equal((int) HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotUnlinkSteam()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.UnlinkSteamAsync(session, "invalid"));
            Assert.Equal((int) HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotLinkApple()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.LinkAppleAsync(session, "invalid"));
            Assert.Equal((int) HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Fact]
        public async Task ShouldNotUnlinkApple()
        {
            var customid = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(customid);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.UnlinkAppleAsync(session, "invalid"));
            Assert.Equal((int) HttpStatusCode.BadRequest, ex.StatusCode);
        }
    }
}
