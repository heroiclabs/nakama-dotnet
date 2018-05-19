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

using System.Linq;

namespace Nakama.Tests.Api
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class GroupTest
    {
        private IClient _client;

        // ReSharper disable RedundantArgumentDefaultValue
        [SetUp]
        public void SetUp()
        {
            _client = new Client("defaultkey", "127.0.0.1", 7350, false);
        }

        [Test]
        public async Task ShouldCreateGroup()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var name = $"{Guid.NewGuid()}";
            const string desc = "A group for Marvel super heroes.";
            const string avatarUrl = "http://graph.facebook.com/892489324234/picture?type=square";
            const string langTag = "en_US";
            const bool open = false;
            var group = await _client.CreateGroupAsync(session, name, desc, avatarUrl, langTag, open);

            Assert.NotNull(group);
            Assert.NotNull(group.Id);
            Assert.NotNull(group.CreateTime);
            Assert.NotNull(group.UpdateTime);
            Assert.AreEqual(1, group.EdgeCount);
            Assert.AreEqual(name, group.Name);
            Assert.AreEqual(desc, group.Description);
            Assert.AreEqual(avatarUrl, group.AvatarUrl);
            Assert.AreEqual(langTag, group.LangTag);
            Assert.AreEqual(open, group.Open);
        }

        [Test]
        public async Task ShouldCreateGroupDefault()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var name = $"{Guid.NewGuid()}";
            var group = await _client.CreateGroupAsync(session, name);

            Assert.NotNull(group);
            Assert.NotNull(group.Id);
            Assert.NotNull(group.CreateTime);
            Assert.NotNull(group.UpdateTime);
            Assert.Null(group.AvatarUrl);
            Assert.Null(group.Description);
            Assert.AreEqual(1, group.EdgeCount);
            Assert.AreEqual(true, group.Open);
            Assert.AreEqual(name, group.Name);
        }

        [Test]
        public async Task ShouldNotCreateGroup()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var name = $"{Guid.NewGuid()}";
            await _client.CreateGroupAsync(session, name);

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.CreateGroupAsync(session, name));
            Assert.AreEqual("400 (Bad Request)", ex.Message);
        }

        [Test]
        public async Task ShouldListGroups()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            // Must create at least one group.
            await _client.CreateGroupAsync(session, $"{Guid.NewGuid()}");
            var result = await _client.ListGroupsAsync(session);

            Assert.NotNull(result);
            Assert.IsNotEmpty(result.Groups);
        }

        [Test]
        public async Task ShouldListGroupsNameFilter()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var name = $"{Guid.NewGuid()}";
            await _client.CreateGroupAsync(session, name);
            var result = await _client.ListGroupsAsync(session, name, 1);

            Assert.NotNull(result);
            Assert.That(result.Groups, Has.Count.EqualTo(1));
            Assert.That(result.Groups.Count(g => name.Equals(g.Name)), Is.EqualTo(1));
        }

        [Test]
        public async Task ShouldListGroupsFilterTwo()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var basename = $"{Guid.NewGuid()}";
            var name1 = string.Concat(basename, "1");
            await _client.CreateGroupAsync(session, name1);
            var name2 = string.Concat(basename, "2");
            await _client.CreateGroupAsync(session, name2);
            // Filter on name with a wildcard.
            var result = await _client.ListGroupsAsync(session, string.Concat(basename, "%"), 2);

            Assert.NotNull(result);
            Assert.That(result.Groups, Has.Count.EqualTo(2));
            Assert.That(result.Groups.Count(g => name1.Equals(g.Name) || name2.Equals(g.Name)), Is.EqualTo(2));
        }

        [Test]
        public async Task ShouldListGroupsCursor()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            await _client.CreateGroupAsync(session, $"{Guid.NewGuid()}");
            await _client.CreateGroupAsync(session, $"{Guid.NewGuid()}");

            var result = await _client.ListGroupsAsync(session);
            Assert.NotNull(result);
            Assert.NotNull(result.Cursor);
            result = await _client.ListGroupsAsync(session, null, 10, result.Cursor);

            Assert.NotNull(result);
            Assert.That(result.Groups, Has.Count.GreaterThanOrEqualTo(1));
        }

        [Test]
        public async Task ShouldDeleteGroup()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var name = $"{Guid.NewGuid()}";
            var group = await _client.CreateGroupAsync(session, name);
            await _client.DeleteGroupAsync(session, group.Id);

            var result1 = await _client.ListGroupsAsync(session, name);
            Assert.NotNull(result1);
            Assert.IsEmpty(result1.Groups);

            var result2 = await _client.ListUserGroupsAsync(session);
            Assert.NotNull(result2);
            Assert.IsEmpty(result2.UserGroups);
        }

        [Test]
        public async Task ShouldDeleteGroupInvalid()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            Assert.DoesNotThrowAsync(() => _client.DeleteGroupAsync(session, $"{Guid.NewGuid()}"));
        }

        [Test]
        public async Task ShouldNotDeleteGroupNotSuperAdmin()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var group = await _client.CreateGroupAsync(session1, $"{Guid.NewGuid()}");

            var ex = Assert.ThrowsAsync<HttpRequestException>(() => _client.DeleteGroupAsync(session2, group.Id));
            Assert.AreEqual("400 (Bad Request)", ex.Message);
        }

        
    }
}
