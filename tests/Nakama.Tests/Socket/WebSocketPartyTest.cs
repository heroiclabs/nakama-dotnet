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
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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

        [Fact]
        public async Task ShouldCreateAndJoinParty()
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

        [Fact]
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

            var partyMatchmakingTcs = new TaskCompletionSource<IPartyMatchmakerTicket>();
            socket1.ReceivedPartyMatchmakerTicket += matchmakerTicket => partyMatchmakingTcs.SetResult(matchmakerTicket);

            var party = await socket1.CreatePartyAsync(false, 2);
            Assert.NotNull(party);
            Assert.NotEmpty(party.Id);
            Assert.False(party.Open);

            await socket2.JoinPartyAsync(party.Id);

            var joinRequest = await partyJoinRequestTcs.Task;

            await socket1.AcceptPartyMemberAsync(joinRequest.PartyId, joinRequest.Presences.First());

            await partyPresenceJoinedTcs.Task;

            await socket1.AddMatchmakerPartyAsync(party.Id, "*", 2, 2);
            var result = await partyMatchmakingTcs.Task;

            Assert.NotEmpty(result.Ticket);
            await socket1.RemoveMatchmakerPartyAsync(party.Id, result.Ticket);

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }

        [Fact]
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

            await socket1.PromotePartyMember(party.Id, partyPresenceEvent.Joins.First());

            var promotedLeader = await partyPromoteTcs.Task;
            _testOutputHelper.WriteLine(promotedLeader.ToString());

            Assert.NotNull(promotedLeader);
            Assert.Equal(session2.UserId, promotedLeader.Presence.UserId);
        }
    }
}
