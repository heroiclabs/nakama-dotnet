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
    }
}
