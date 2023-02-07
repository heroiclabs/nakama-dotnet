// Copyright 2021 The Nakama Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Threading;

namespace Nakama.Tests.Socket
{
    public class WebSocketPartyTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IClient _client;

        public WebSocketPartyTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _client = TestsUtil.FromSettingsFile();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldCreateParty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket = Nakama.Socket.From(_client);
            await socket.ConnectAsync(session);

            var result = await socket.CreatePartyAsync(false, 1);

            Assert.NotNull(result);
            Assert.NotNull(result.Self);
            Assert.Equal(session.UserId, result.Self.UserId);
            Assert.Equal(session.Username, result.Self.Username);

            await socket.CloseAsync();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldReceiveJoinEvent()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket = Nakama.Socket.From(_client);
            await socket.ConnectAsync(session);

            var createdParty = await socket.CreatePartyAsync(true, 2);

            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket2 = Nakama.Socket.From(_client);
            await socket2.ConnectAsync(session2);

            var partyReceivedTcs = new TaskCompletionSource<IParty>();

            socket2.JoinPartyAsync(createdParty.Id);

            socket2.ReceivedParty += party =>
            {
                partyReceivedTcs.SetResult(party);
            };

            var joinedParty = await partyReceivedTcs.Task;

            Assert.NotNull(joinedParty);
            Assert.NotNull(joinedParty.Self);
            Assert.Equal(session2.UserId, joinedParty.Self.UserId);
            Assert.Equal(session2.Username, joinedParty.Self.Username);

            await socket.CloseAsync();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldAddAndRemovePartyFromMatchmaker()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);

            var partyJoinRequestTcs = new TaskCompletionSource<IPartyJoinRequest>();
            socket1.ReceivedPartyJoinRequest += request => partyJoinRequestTcs.SetResult(request);

            var partyPresenceJoinedTcs = new TaskCompletionSource<IPartyPresenceEvent>();
            socket1.ReceivedPartyPresence += presenceEvt => partyPresenceJoinedTcs.SetResult(presenceEvt);

            var party = await socket1.CreatePartyAsync(false, 2);
            Assert.NotNull(party);
            Assert.NotEmpty(party.Id);
            Assert.False(party.Open);

            await socket2.JoinPartyAsync(party.Id);

            var joinRequest = await partyJoinRequestTcs.Task;

            await socket1.AcceptPartyMemberAsync(joinRequest.PartyId, joinRequest.Presences.First());

            await partyPresenceJoinedTcs.Task;

            var result = await socket1.AddMatchmakerPartyAsync(party.Id, "*", 2, 2);

            Assert.NotEmpty(result.Ticket);
            await socket1.RemoveMatchmakerPartyAsync(party.Id, result.Ticket);

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldPromoteMember()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);

            var partyJoinRequestTcs = new TaskCompletionSource<IPartyJoinRequest>();
            socket1.ReceivedPartyJoinRequest += request => partyJoinRequestTcs.SetResult(request);

            var partyPromoteTcs = new TaskCompletionSource<IPartyLeader>();
            socket1.ReceivedPartyLeader += newLeader => partyPromoteTcs.SetResult(newLeader);

            var party = await socket1.CreatePartyAsync(false, 2);
            Assert.NotNull(party);
            Assert.NotEmpty(party.Id);
            Assert.False(party.Open);

            await socket2.JoinPartyAsync(party.Id);

            var joinRequest = await partyJoinRequestTcs.Task;
            _testOutputHelper.WriteLine(joinRequest.ToString());

            var partyPresenceJoinedTcs = new TaskCompletionSource<IPartyPresenceEvent>();
            socket1.ReceivedPartyPresence += presenceEvt => partyPresenceJoinedTcs.SetResult(presenceEvt);

            await socket1.AcceptPartyMemberAsync(joinRequest.PartyId, joinRequest.Presences.First());
            var partyPresenceEvent = await partyPresenceJoinedTcs.Task;
            _testOutputHelper.WriteLine(partyPresenceEvent.ToString());

            await socket1.PromotePartyMemberAsync(party.Id, partyPresenceEvent.Joins.First());

            var promotedLeader = await partyPromoteTcs.Task;
            _testOutputHelper.WriteLine(promotedLeader.ToString());

            Assert.NotNull(promotedLeader);
            Assert.Equal(session2.UserId, promotedLeader.Presence.UserId);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldSendAndReceivePartyData()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);

            var party = await socket1.CreatePartyAsync(true, 2);

            await socket2.JoinPartyAsync(party.Id);

            var partyDataTcs = new TaskCompletionSource<IPartyData>();

            socket1.ReceivedPartyData += (data) => partyDataTcs.SetResult(data);

            await socket2.SendPartyDataAsync(party.Id, 0, System.Text.Encoding.UTF8.GetBytes("hello world"));

            await partyDataTcs.Task;

            Assert.Equal("hello world", System.Text.Encoding.UTF8.GetString(partyDataTcs.Task.Result.Data));

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldJoinClosedParty()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);

            var party = await socket1.CreatePartyAsync(false, 2);

            var requestedJoinTcs = new TaskCompletionSource<IPartyJoinRequest>();
            socket1.ReceivedPartyJoinRequest += (request) => requestedJoinTcs.SetResult(request);
            await socket2.JoinPartyAsync(party.Id);

            await requestedJoinTcs.Task;

            var acceptedTcs = new TaskCompletionSource<IParty>();

            socket2.ReceivedParty += (party) => acceptedTcs.SetResult(party);

            foreach (var presence in requestedJoinTcs.Task.Result.Presences)
            {
                await socket1.AcceptPartyMemberAsync(requestedJoinTcs.Task.Result.PartyId, presence);
            }

            await acceptedTcs.Task;

            Assert.True(acceptedTcs.Task.Result.Id == party.Id);
            Assert.True(acceptedTcs.Task.Result.Self.UserId == session2.UserId);

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldNotJoinPastMaxSize()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session3 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);
            var socket3 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);
            await socket3.ConnectAsync(session3);

            var party = await socket1.CreatePartyAsync(true, 2);

            await socket2.JoinPartyAsync(party.Id);
            await Assert.ThrowsAsync<WebSocketException>(() => socket3.JoinPartyAsync(party.Id));

            await socket1.CloseAsync();
            await socket2.CloseAsync();
            await socket3.CloseAsync();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task LeaderShouldBeInInitialPresences()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);

            var party = await socket1.CreatePartyAsync(true, 2);

            Assert.Single(party.Presences);
            Assert.Equal(party.Leader.UserId, party.Presences.First().UserId);

            await socket1.CloseAsync();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task PresencesInitializedWithConcurrentJoins()
        {
            const int numMembers = 5;

            var leaderSession = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var leaderSocket = Nakama.Socket.From(_client);
            await leaderSocket.ConnectAsync(leaderSession);

            var memberSessions = new ISession[numMembers];
            var memberSockets = new Nakama.ISocket[numMembers];

            IParty party = await leaderSocket.CreatePartyAsync(true, numMembers + 1);

            var memberPartyObjects = new IParty[numMembers];

            int partyObjCounter = 0;

            for (int i = 0; i < numMembers; i++)
            {
                memberSessions[i] = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
                memberSockets[i] = (Nakama.Socket.From(_client));
                await memberSockets[i].ConnectAsync(memberSessions[i]);

                memberSockets[i].ReceivedParty += party => {
                    memberPartyObjects[partyObjCounter] = party;
                    Interlocked.Increment(ref partyObjCounter);
                };

                memberSockets[i].JoinPartyAsync(party.Id);
            }

            while (partyObjCounter < numMembers)
            {
                await Task.Delay(25);
            }

            // includes duplicates
            var combinedPresences = memberPartyObjects.SelectMany(party => party.Presences);

            foreach (var presence in combinedPresences)
            {
                Assert.False(string.IsNullOrEmpty(presence.UserId));
                Assert.False(string.IsNullOrEmpty(presence.Username));
                Assert.False(string.IsNullOrEmpty(presence.SessionId));
            }

            await leaderSocket.CloseAsync();

            foreach (var memberSocket in memberSockets)
            {
                await memberSocket.CloseAsync();
            }
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldBootThenClose()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session3 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);
            var socket3 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);
            await socket3.ConnectAsync(session3);

            var party = await socket1.CreatePartyAsync(true, 3);

            var socket2PresenceTcs = new TaskCompletionSource<IUserPresence>();

            socket1.ReceivedPartyPresence += presences => {

                var session2Join = presences.Joins.FirstOrDefault(presence => presence.UserId == session2.UserId);
                if (session2Join != null)
                {
                    socket2PresenceTcs.SetResult(session2Join);
                }
            };

            await socket2.JoinPartyAsync(party.Id);
            await socket3.JoinPartyAsync(party.Id);

            await socket2PresenceTcs.Task;

            var socket2CloseTcs = new TaskCompletionSource();
            var socket3CloseTcs = new TaskCompletionSource();

            socket2.ReceivedPartyClose += (close) => socket2CloseTcs.SetResult();

            await socket1.RemovePartyMemberAsync(party.Id, socket2PresenceTcs.Task.Result);
            await socket2CloseTcs.Task;

            socket3.ReceivedPartyClose += (close) => socket3CloseTcs.SetResult();

            await socket1.ClosePartyAsync(party.Id);
            await socket3CloseTcs.Task;

            await socket1.CloseAsync();
            await socket2.CloseAsync();
            await socket3.CloseAsync();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task LeaderAndMembersShouldReceiveTicket()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);

            var party = await socket1.CreatePartyAsync(true, 2);

            await socket2.JoinPartyAsync(party.Id);

            var ticketTcs = new TaskCompletionSource<IPartyMatchmakerTicket>();

            socket2.ReceivedPartyMatchmakerTicket += (ticket) => ticketTcs.SetResult(ticket);

            var ticket = await socket1.AddMatchmakerPartyAsync(party.Id, "*", 2, 2);
            await ticketTcs.Task;

            Assert.Equal(ticketTcs.Task.Result.Ticket, ticket.Ticket);

            await socket1.RemoveMatchmakerPartyAsync(party.Id, ticket.Ticket);

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }
    }
}
