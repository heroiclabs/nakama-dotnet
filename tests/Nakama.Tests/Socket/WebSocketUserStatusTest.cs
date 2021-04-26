/**
 * Copyright 2021 The Nakama Authors
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
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests.Socket
{
    public class WebSocketUserStatusTest
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        private IClient _client;

        public WebSocketUserStatusTest()
        {
            _client = ClientUtil.FromSettingsFile();
        }

        [Fact]
        public async void FollowUsers_NoUsers_AnotherUser()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            var session1 = await _client.AuthenticateCustomAsync(id1);
            var session2 = await _client.AuthenticateCustomAsync(id2);

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.TrySetCanceled());
            canceller.CancelAfter(Timeout);

            var socket1 = Nakama.Socket.From(_client);
            socket1.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            socket1.ReceivedError += e => canceller.Cancel();
            await socket1.ConnectAsync(session1);
            await socket1.FollowUsersAsync(new[] {session2.UserId});

            var socket2 = Nakama.Socket.From(_client);
            await socket2.ConnectAsync(session2);
            await socket2.UpdateStatusAsync("new status change");

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session2.UserId));

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }

        [Fact]
        public async void FollowUsers_NoUsers_AnotherUserByUsername()
        {
            var id = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id);
            var session2 = await _client.AuthenticateCustomAsync(id + "a");

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.TrySetCanceled());
            canceller.CancelAfter(Timeout);

            var socket1 = Nakama.Socket.From(_client);
            socket1.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            socket1.ReceivedError += e => completer.TrySetException(e);
            await socket1.ConnectAsync(session1);
            await socket1.FollowUsersAsync(new string[] { }, new[] {session2.Username});

            var socket2 = Nakama.Socket.From(_client);
            await socket2.ConnectAsync(session2);
            await socket2.UpdateStatusAsync("new status change");

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session2.UserId));

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }

        [Fact]
        public async void FollowUsers_NoUsers_FollowedSelf()
        {
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);

            var socket1 = Nakama.Socket.From(_client);
            await socket1.ConnectAsync(session);

            var statuses = await socket1.FollowUsersAsync(new[] {session.UserId});

            Assert.NotNull(statuses);
            Assert.Empty(statuses.Presences);

            await socket1.CloseAsync();
        }

        [Fact]
        public async void FollowUsers_NoUsers_UserJoinsAndLeaves()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id1);
            var session2 = await _client.AuthenticateCustomAsync(id2);

            var completer1 = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer1.TrySetCanceled());
            canceller.CancelAfter(Timeout);

            var socket1 = Nakama.Socket.From(_client);
            socket1.ReceivedStatusPresence += statuses => completer1.TrySetResult(statuses);
            socket1.ReceivedError += e => completer1.TrySetException(e);
            await socket1.ConnectAsync(session1);
            await socket1.FollowUsersAsync(new[] {session2.UserId});

            // Second user comes online and sets status.
            var socket2 = Nakama.Socket.From(_client);
            await socket2.ConnectAsync(session2);
            await socket2.UpdateStatusAsync("new status change");

            var result1 = await completer1.Task;
            Assert.NotNull(result1);
            Assert.Empty(result1.Leaves);
            Assert.Contains(result1.Joins, joined => joined.UserId.Equals(session2.UserId));

            var completer2 = new TaskCompletionSource<IStatusPresenceEvent>();
            socket1.ReceivedStatusPresence += statuses => completer2.SetResult(statuses);

            // Second user drops offline.
            await socket2.CloseAsync();
            var result2 = await completer2.Task;
            Assert.NotNull(result2);
            Assert.Empty(result2.Joins);
            Assert.Contains(result2.Leaves, left => left.UserId.Equals(session2.UserId));

            await socket1.CloseAsync();
        }

        [Fact]
        public async void FollowUsers_TwoSessions_HasTwoStatuses()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            var session1 = await _client.AuthenticateCustomAsync(id1);
            var session2 = await _client.AuthenticateCustomAsync(id2);

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);

            // Both sockets for single user set statuses.
            const string status1 = "user 2 socket 1 status.";
            await socket1.UpdateStatusAsync(status1);
            const string status2 = "user 2 socket 2 status.";
            await socket2.UpdateStatusAsync(status2);

            var statuses = await socket1.FollowUsersAsync(new[] {session2.UserId});
            Assert.NotNull(statuses);
            Assert.Contains(statuses.Presences,
                presence => presence.Status.Equals(status1) || presence.Status.Equals(status2));

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }

        [Fact]
        public async void FollowUsers_TwoUsers_ThirdUserFollowsBoth()
        {
            var id1 = Guid.NewGuid().ToString();
            var socket1 = Nakama.Socket.From(_client);
            //socket1.ReceivedError
            var session1 = await _client.AuthenticateCustomAsync(id1);

            var id2 = Guid.NewGuid().ToString();
            var socket2 = Nakama.Socket.From(_client);
            //socket2.ReceivedError
            var session2 = await _client.AuthenticateCustomAsync(id2);

            var id3 = Guid.NewGuid().ToString();
            var socket3 = Nakama.Socket.From(_client);
            //socket3.ReceivedError
            var session3 = await _client.AuthenticateCustomAsync(id3);

            // Two users come online. Each publishes a status.
            await socket1.ConnectAsync(session1);
            await socket1.UpdateStatusAsync("user 1 status.");
            await socket2.ConnectAsync(session2);
            await socket2.UpdateStatusAsync("user 2 status.");

            // Third user comes online and follows both users.
            await socket3.ConnectAsync(session3);
            var statuses = await socket3.FollowUsersAsync(new[] {session1.UserId, session2.UserId});
            Assert.NotNull(statuses);
            Assert.NotEmpty(statuses.Presences);
            Assert.Contains(statuses.Presences,
                presence => presence.UserId.Equals(session1.UserId) || presence.UserId.Equals(session2.UserId));

            // Dispose
            await socket1.CloseAsync();
            await socket2.CloseAsync();
            await socket3.CloseAsync();
        }

        [Fact]
        public async void UpdateStatus_NoStatus_HasStatus()
        {
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.TrySetCanceled());
            canceller.CancelAfter(Timeout);

            var socket1 = Nakama.Socket.From(_client);
            socket1.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            socket1.ReceivedError += e => completer.TrySetException(e);
            await socket1.ConnectAsync(session);

            await socket1.UpdateStatusAsync("super status change!");
            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session.UserId));

            await socket1.CloseAsync();
        }

        [Fact (Skip = "Long-running test")]
        public async void TestFollowMassiveNumberOfUsers()
        {
            const int numFollowees = 500;

            var id1 = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id1);
            var socket1 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);

            var followeeTasks = new List<Task>();
            var followeeSessions = new List<ISession>();

            for (int i = 0; i < numFollowees; i++)
            {
                ISocket socket = null;
                var followeeId = Guid.NewGuid().ToString();

                var followeeTask = await _client.AuthenticateCustomAsync(followeeId)
                .ContinueWith(async session => {
                    followeeSessions.Add(session.Result);
                    socket = Nakama.Socket.From(_client);
                    await socket.ConnectAsync(session.Result);
                    await socket.UpdateStatusAsync("status for " + i.ToString());
                });

                followeeTasks.Add(followeeTask);
            }


            Task.WaitAll(followeeTasks.ToArray());

            IStatus statuses = null;

            try
            {
                statuses = await socket1.FollowUsersAsync(followeeSessions.Select(session => session.UserId));
            }
            catch (ApiResponseException e)
            {
                throw e;
            }

            Assert.Equal(numFollowees, statuses.Presences.Count());

            await socket1.CloseAsync();
        }

        [Fact]
        public async void TestUserDoesNotReceiveUpdatedAfterUnfollow()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            var session1 = await _client.AuthenticateCustomAsync(id1);
            var session2 = await _client.AuthenticateCustomAsync(id2);

            var waitForStatusPresence = new TaskCompletionSource<IStatusPresenceEvent>();

            var socket1 = Nakama.Socket.From(_client);

            Action<IStatusPresenceEvent> receivedPresenceWhileFollowing = (statuses) => waitForStatusPresence.SetResult(statuses);
            socket1.ReceivedStatusPresence += receivedPresenceWhileFollowing;

            socket1.ReceivedError += e => {
                waitForStatusPresence.TrySetException(e);
            };

            await socket1.ConnectAsync(session1);
            await socket1.FollowUsersAsync(new[] {session2.UserId});

            var socket2 = Nakama.Socket.From(_client);

            await socket2.ConnectAsync(session2);

            var cancelAfterTimeout = new CancellationTokenSource();
            cancelAfterTimeout.Token.Register(() => waitForStatusPresence.TrySetException(
                new Exception("Timeout while waiting for socket two to come online.")));
            cancelAfterTimeout.CancelAfter(Timeout);

            await socket2.UpdateStatusAsync("new status change");
            await waitForStatusPresence.Task;
            socket1.ReceivedStatusPresence -= receivedPresenceWhileFollowing;

            await socket1.UnfollowUsersAsync(new []{session2.UserId});
            await socket2.UpdateStatusAsync("new status change that should not be received");

            var ensureNoStatusPresence = new TaskCompletionSource<IStatusPresenceEvent>();

            socket1.ReceivedStatusPresence += status =>
            {
                if (status.Joins.Any(join => join.UserId == session2.UserId))
                {
                    throw new Exception("Received user leave presence after unfollowing.");
                }
            };

            await Task.Delay(Timeout);

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }

        [Fact]
        public async void TestUserFollowSameUserTwice()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            var session1 = await _client.AuthenticateCustomAsync(id1);
            var session2 = await _client.AuthenticateCustomAsync(id2);

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);

            await socket1.FollowUsersAsync(new string[]{session2.UserId});
            await socket1.FollowUsersAsync(new string[]{session2.UserId});

            int numStatusesReceived = 0;

            socket1.ReceivedStatusPresence += status => {
                numStatusesReceived++;
            };

            await socket2.UpdateStatusAsync("this should only be dispatched once");

            await Task.Delay(Timeout);

            Assert.Equal(numStatusesReceived, 1);

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }

        [Fact]
        public async void TestUnfollowSelf()
        {
            var id1 = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id1);
            var socket1 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);

            bool receivedOwnPresence = false;

            socket1.ReceivedStatusPresence += status => {
                receivedOwnPresence = true;
            };

            await socket1.UnfollowUsersAsync(new string[]{session1.UserId});
            await socket1.UpdateStatusAsync("this should still be received by the user");
            await Task.Delay(Timeout);

            Assert.True(receivedOwnPresence);

            await socket1.CloseAsync();
        }

        [Fact]
        public async void TestFollowNonExistentUser()
        {
            var id1 = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id1);
            var socket1 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);
            await Assert.ThrowsAsync<WebSocketException>(
                () => socket1.FollowUsersAsync(new string[]{"does_not_exist"}));

            await socket1.CloseAsync();
        }

        [Fact (Skip = "investigate this!")]
        public async void TestRepeatedOnlineOffline()
        {
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();

            var session1 = await _client.AuthenticateCustomAsync(id1);
            var session2 = await _client.AuthenticateCustomAsync(id2);

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);

            int numJoinsReceived = 0;
            int numLeavesReceived = 0;

            socket1.ReceivedStatusPresence += status =>
            {
                if (status.Joins.Any(join => join.UserId == session2.UserId))
                {
                    numJoinsReceived++;
                }

                if (status.Leaves.Any(leave => leave.UserId == session2.UserId))
                {
                    numLeavesReceived++;
                }
            };

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);

            await socket1.FollowUsersAsync(new string[]{session2.UserId});
            await socket2.UpdateStatusAsync("I am going to spam socket 1 (first time)");
            await socket2.CloseAsync();

            await socket2.ConnectAsync(session2);
            await socket2.UpdateStatusAsync("I am going to spam socket 1 (second time)");
            await socket2.CloseAsync();

            await socket2.ConnectAsync(session2);
            await socket2.UpdateStatusAsync("I am going to spam socket 1 (third time)");
            await socket2.CloseAsync();

            await Task.Delay(Timeout);

            Assert.Equal(3, numLeavesReceived);
            Assert.Equal(3, numJoinsReceived);

            await socket1.CloseAsync();
        }
    }
}
