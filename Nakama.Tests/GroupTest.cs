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

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests.Api
{
    public class GroupTest
    {
        private IClient _client;
        private ISocket _socket;

        public GroupTest()
        {
            _client = TestsUtil.FromSettingsFile();
            _socket = Nakama.Socket.From(_client);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
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
            Assert.Equal(1, group.EdgeCount);
            Assert.Equal(name, group.Name);
            Assert.Equal(desc, group.Description);
            Assert.Equal(avatarUrl, group.AvatarUrl);
            Assert.Equal(langTag, group.LangTag);
            Assert.Equal(open, group.Open);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
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
            Assert.Equal(1, group.EdgeCount);
            Assert.True(group.Open);
            Assert.Equal(name, group.Name);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldNotCreateGroup()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var name = $"{Guid.NewGuid()}";
            await _client.CreateGroupAsync(session, name);

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.CreateGroupAsync(session, name));
            Assert.Equal((int) HttpStatusCode.Conflict, ex.StatusCode);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldListGroups()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            // Must create at least one group.
            await _client.CreateGroupAsync(session, $"{Guid.NewGuid()}");
            var result = await _client.ListGroupsAsync(session);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Groups);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldListGroupsNameFilter()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var name = $"{Guid.NewGuid()}";
            await _client.CreateGroupAsync(session, name);
            var result = await _client.ListGroupsAsync(session, name, 1);

            Assert.NotNull(result);
            Assert.True(result.Groups.Count() == 1);
            Assert.True(result.Groups.Count(g => name.Equals(g.Name)) == 1);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
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
            Assert.True(result.Groups.Count() == 2);
            Assert.True(result.Groups.Count(g => name1.Equals(g.Name) || name2.Equals(g.Name)) == 2);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
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
            Assert.True(result.Groups.Count() >= 1);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldDeleteGroup()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var name = $"{Guid.NewGuid()}";
            var group = await _client.CreateGroupAsync(session, name);
            await _client.DeleteGroupAsync(session, group.Id);

            var result1 = await _client.ListGroupsAsync(session, name);
            Assert.NotNull(result1);
            Assert.Empty(result1.Groups);

            var result2 = await _client.ListUserGroupsAsync(session);
            Assert.NotNull(result2);
            Assert.Empty(result2.UserGroups);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldDeleteGroupInvalid()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() =>
                _client.DeleteGroupAsync(session, $"{Guid.NewGuid()}"));
            Assert.Equal((int) HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldNotDeleteGroupNotSuperAdmin()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var group = await _client.CreateGroupAsync(session1, $"{Guid.NewGuid()}");

            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => _client.DeleteGroupAsync(session2, group.Id));
            Assert.Equal((int) HttpStatusCode.BadRequest, ex.StatusCode);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldPromoteAndDemoteUsers()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session3 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var group = await _client.CreateGroupAsync(session1, $"{Guid.NewGuid()}");

            await _client.AddGroupUsersAsync(session1, group.Id, new string[]{session2.UserId, session3.UserId});
            await _client.PromoteGroupUsersAsync(session1, group.Id, new string[]{session2.UserId, session3.UserId});

            var admins = await _client.ListGroupUsersAsync(session1, group.Id, state: 1, limit: 2);

            Assert.Equal(2, admins.GroupUsers.Count());

            await _client.DemoteGroupUsersAsync(session1, group.Id, new string[]{session2.UserId, session3.UserId});

            admins = await _client.ListGroupUsersAsync(session1, group.Id, state: 1, limit: 2);
            Assert.Empty(admins.GroupUsers);

            var members = await _client.ListGroupUsersAsync(session1, group.Id, state: 2, limit: 2);
            Assert.Equal(2, members.GroupUsers.Count());
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldBanUsers()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session3 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var group = await _client.CreateGroupAsync(session1, $"{Guid.NewGuid()}");

            await _client.AddGroupUsersAsync(session1, group.Id, new string[]{session2.UserId, session3.UserId});
            await _client.BanGroupUsersAsync(session1, group.Id, new []{session2.UserId, session3.UserId});
            var remainingMembers = await _client.ListGroupUsersAsync(session1, group.Id, state: null, limit: 100);
            Assert.Single(remainingMembers.GroupUsers);

            await _client.JoinGroupAsync(session2, group.Id);

            remainingMembers = await _client.ListGroupUsersAsync(session1, group.Id, state: null, limit: 100);
            Assert.Single(remainingMembers.GroupUsers);

            var groupList = await _client.ListUserGroupsAsync(session2, null, 100);
            Assert.Empty(groupList.UserGroups);
        }
    }
}
