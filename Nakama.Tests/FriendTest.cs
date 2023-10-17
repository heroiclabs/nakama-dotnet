/**
 * Copyright 2023 The Nakama Authors
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
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests.Api
{
    public class FriendTest
    {
        private IClient _client;
        private ISocket _socket;

        public FriendTest()
        {
            _client = TestsUtil.FromSettingsFile();
            _socket = Nakama.Socket.From(_client);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task AddingBannedFriendShouldNoop()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            await _socket.ConnectAsync(session2);

            IApiNotification? session2Notif = null;

            _socket.ReceivedNotification += (IApiNotification notif) => {
                session2Notif = notif;
            };

            await _client.BlockFriendsAsync(session, new string[]{session2.UserId});
            await _client.AddFriendsAsync(session, new string[]{session2.UserId});
            var friendList = await _client.ListFriendsAsync(session);
            Assert.Single(friendList.Friends);
            Assert.Equal(3, friendList.Friends.First().State); // banned
            Assert.Null(session2Notif);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task FriendsShouldBeAddedAndAccepted()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket2 = Nakama.Socket.From(_client);

            await _socket.ConnectAsync(session);
            await socket2.ConnectAsync(session2);

            IApiNotification? session1Notif = null;

            _socket.ReceivedNotification += (IApiNotification notif) => {
                session1Notif = notif;
            };

            IApiNotification? session2Notif = null;

            socket2.ReceivedNotification += (IApiNotification notif) => {
                session2Notif = notif;
            };

            await _client.AddFriendsAsync(session, new string[]{session2.UserId});

            await Task.Delay(1000);
            Assert.NotNull(session2Notif);

            var friendList = await _client.ListFriendsAsync(session, 1); // has sent invitation
            Assert.Single(friendList.Friends);

            await _client.AddFriendsAsync(session2, new string[]{session.UserId});
            await Task.Delay(1000);
            Assert.NotNull(session1Notif);

            friendList = await _client.ListFriendsAsync(session, 0); // friends

            Assert.Single(friendList.Friends);
        }
    }
}
