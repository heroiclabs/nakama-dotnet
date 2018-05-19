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
    public class FriendTest
    {
        private IClient _client;

        // ReSharper disable RedundantArgumentDefaultValue
        [SetUp]
        public void SetUp()
        {
            _client = new Client("defaultkey", "127.0.0.1", 7350, false);
        }

        [Test]
        public async Task ShouldNotImportFacebookFriends()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var ex = Assert.ThrowsAsync<HttpRequestException>(() =>
                _client.ImportFacebookFriendsAsync(session, "invalid"));
            Assert.AreEqual("401 (Unauthorized)", ex.Message);
        }

        [Test]
        public async Task ShouldListFriendsEmpty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var result = await _client.ListFriendsAsync(session);

            Assert.NotNull(result);
            Assert.IsEmpty(result.Friends);
        }

        [Test]
        public async Task ShouldDeleteFriendsInvalidEmpty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            Assert.NotNull(session);
            Assert.DoesNotThrowAsync(() => _client.DeleteFriendsAsync(session, new string[0], new string[0]));
        }

        [Test]
        public async Task ShouldDeleteFriendsInvalidUserId()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            Assert.NotNull(session);
            Assert.DoesNotThrowAsync(() =>
                _client.DeleteFriendsAsync(session, new[] {"eb67ad3c-5628-11e8-b43d-bbed48c7ceef"}));
        }

        [Test]
        public async Task ShouldDeleteFriendsInvalidUsername()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            Assert.NotNull(session);
            Assert.DoesNotThrowAsync(() => _client.DeleteFriendsAsync(session, null, new[] {"someusername"}));
        }

        [Test]
        public async Task ShouldAddFriendsAccepted()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            await _client.AddFriendsAsync(session1, new[] {session2.UserId});
            await _client.AddFriendsAsync(session2, new[] {session1.UserId});

            var result1 = await _client.ListFriendsAsync(session1);
            Assert.NotNull(result1);
            Assert.AreEqual(1, result1.Friends.First().State);
            Assert.AreEqual(session2.UserId, result1.Friends.First().User.Id);
            var result2 = await _client.ListFriendsAsync(session2);
            Assert.NotNull(result2);
            Assert.AreEqual(1, result2.Friends.First().State);
            Assert.AreEqual(session1.UserId, result2.Friends.First().User.Id);
        }

        [Test]
        public async Task ShouldAddFriendsWithUsername()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            await _client.AddFriendsAsync(session, new[] {session1.UserId}, new []{session2.Username});
            var result = await _client.ListFriendsAsync(session);

            Assert.NotNull(result);
            Assert.IsNotEmpty(result.Friends);
            Assert.That(
                result.Friends.Count(f =>
                    f.State == 3 && (f.User.Id.Equals(session1.UserId) || f.User.Id.Equals(session2.UserId))),
                Is.EqualTo(2));
        }

        [Test]
        public async Task ShouldNotAddFriendsSelf()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var ex = Assert.ThrowsAsync<HttpRequestException>(() =>
                _client.AddFriendsAsync(session, new[] {session.UserId}));
            Assert.AreEqual("400 (Bad Request)", ex.Message);
        }

        [Test]
        public async Task ShouldNotAddFriendsNoUser()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            Assert.DoesNotThrowAsync(() => _client.AddFriendsAsync(session, new[] {$"{Guid.NewGuid()}"}));
        }

        [Test]
        public async Task ShouldDeleteFriends()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            await _client.AddFriendsAsync(session1, new[] {session2.UserId});
            await _client.DeleteFriendsAsync(session1, new[] {session2.UserId});
            var result = await _client.ListFriendsAsync(session1);

            Assert.NotNull(result);
            Assert.That(result.Friends.Count(f => f.User.Id.Equals(session2.UserId)), Is.EqualTo(0));
        }

        [Test]
        public async Task ShouldDeleteFriendsInvalid()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            Assert.DoesNotThrowAsync(() => _client.DeleteFriendsAsync(session, new []{"invalid"}));
        }

        [Test]
        public async Task ShouldDeleteFriendsNotAFriend()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            Assert.DoesNotThrowAsync(() => _client.DeleteFriendsAsync(session1, new []{session2.UserId}));
        }

        [Test]
        public async Task ShouldDeleteFriendsBlocked()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            await _client.AddFriendsAsync(session1, new[] {session2.UserId});
            await _client.BlockFriendsAsync(session1, new[] {session2.UserId});
            Assert.DoesNotThrowAsync(() => _client.DeleteFriendsAsync(session1, new []{session2.UserId}));
        }
    }
}
