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
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests.Socket
{
    // NOTE Test name patterns are: MethodName_StateUnderTest_ExpectedBehavior
    public class WebSocketUserStatusTest
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        private IClient _client;

        public WebSocketUserStatusTest()
        {
            _client = ClientUtil.FromSettingsFile();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async void FollowUsers_NoUsers_AnotherUser(TestAdapterFactory adapterFactory)
        {
            var id = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id);
            var session2 = await _client.AuthenticateCustomAsync(id + "a");

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.TrySetCanceled());
            canceller.CancelAfter(Timeout);

            var socket1 = Nakama.Socket.From(_client, adapterFactory());

            socket1.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            socket1.ReceivedError += e => completer.TrySetException(e);
            await socket1.ConnectAsync(session1);
            await socket1.FollowUsersAsync(new[] {session2.UserId});

            var socket2 = Nakama.Socket.From(_client, adapterFactory());
            await socket2.ConnectAsync(session2);
            await socket2.UpdateStatusAsync("new status change");

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session2.UserId));

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async void FollowUsers_NoUsers_AnotherUserByUsername(TestAdapterFactory adapterFactory)
        {
            var id = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id);
            var session2 = await _client.AuthenticateCustomAsync(id + "a");

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.TrySetCanceled());
            canceller.CancelAfter(Timeout);
            var socket1 = Nakama.Socket.From(_client, adapterFactory());

            socket1.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            socket1.ReceivedError += e =>
            {
                System.Console.WriteLine(e.Message);
                completer.TrySetException(e);
            };

            await socket1.ConnectAsync(session1);

            await socket1.FollowUsersAsync(new string[] { }, new[] {session2.Username});

            var socket2 = Nakama.Socket.From(_client);

            socket2.ReceivedError += e => System.Console.WriteLine(e.Message);
            await socket2.ConnectAsync(session2);

            System.Console.WriteLine("updating status");

            await socket2.UpdateStatusAsync("new status change");

            System.Console.WriteLine("done updating status");


            var result = await completer.Task;

            System.Console.WriteLine("done awaiting task");

            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session2.UserId));

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async void FollowUsers_NoUsers_FollowedSelf(TestAdapterFactory adapterFactory)
        {
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);
            var socket1 = Nakama.Socket.From(_client, adapterFactory());
            await socket1.ConnectAsync(session);

            var statuses = await socket1.FollowUsersAsync(new[] {session.UserId});
            Assert.NotNull(statuses);
            Assert.Empty(statuses.Presences);

            await socket1.CloseAsync();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async void FollowUsers_NoUsers_UserJoinsAndLeaves(TestAdapterFactory adapterFactory)
        {
            var id = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id);
            var session2 = await _client.AuthenticateCustomAsync(id + "a");

            var completer1 = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer1.TrySetCanceled());
            canceller.CancelAfter(Timeout);

            var socket1 = Nakama.Socket.From(_client, adapterFactory());
            socket1.ReceivedStatusPresence += statuses => completer1.TrySetResult(statuses);
            socket1.ReceivedError += e => completer1.TrySetException(e);
            await socket1.ConnectAsync(session1);
            await socket1.FollowUsersAsync(new[] {session2.UserId});

            // Second user comes online and sets status.
            var socket2 = Nakama.Socket.From(_client, adapterFactory());
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
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async void FollowUsers_TwoSessions_HasTwoStatuses(TestAdapterFactory adapterFactory)
        {
            var id1 = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id1);

            var socket1 = Nakama.Socket.From(_client, adapterFactory());

            await socket1.ConnectAsync(session1);

            var id2 = Guid.NewGuid().ToString();
            var session2 = await _client.AuthenticateCustomAsync(id2);

            var socket2 = Nakama.Socket.From(_client, adapterFactory());

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

            await socket2.CloseAsync();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async void FollowUsers_TwoUsers_ThirdUserFollowsBoth(TestAdapterFactory adapterFactory)
        {
            var id1 = Guid.NewGuid().ToString();
            var socket1 = Nakama.Socket.From(_client, adapterFactory());
            //socket1.ReceivedError
            var session1 = await _client.AuthenticateCustomAsync(id1);

            var id2 = Guid.NewGuid().ToString();
            var socket2 = Nakama.Socket.From(_client, adapterFactory());
            //socket2.ReceivedError
            var session2 = await _client.AuthenticateCustomAsync(id2);

            var id3 = Guid.NewGuid().ToString();
            var socket3 = Nakama.Socket.From(_client, adapterFactory());
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

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async void UpdateStatus_NoStatus_HasStatus(TestAdapterFactory adapterFactory)
        {
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.TrySetCanceled());
            canceller.CancelAfter(Timeout);

            var socket1 = Nakama.Socket.From(_client, adapterFactory());
            socket1.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            await socket1.ConnectAsync(session);

            await socket1.UpdateStatusAsync("super status change!");
            var result = await completer.Task;

            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session.UserId));

            await socket1.CloseAsync();
        }
    }
}
