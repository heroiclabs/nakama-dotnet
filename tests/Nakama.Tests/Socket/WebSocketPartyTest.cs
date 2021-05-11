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
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests.Socket
{
    public class WebSocketPartyTest
    {
        private IClient _client;

        public WebSocketPartyTest()
        {
            _client = ClientUtil.FromSettingsFile();
        }

        [Fact]
        public async Task ShouldCreateAndJoinParty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            System.Console.WriteLine("creating socket");
            var socket = Nakama.Socket.From(_client);
            System.Console.WriteLine("done creating socket");

            await socket.ConnectAsync(session);
            System.Console.WriteLine("done connecting");

            var completer = new TaskCompletionSource<IParty>();
            socket.ReceivedParty += (party) => completer.SetResult(party);
            socket.CreatePartyAsync(false, 1);

            var result = await completer.Task;
            System.Console.WriteLine("done getting result");

            Assert.NotNull(result);
            Assert.Equal(session.UserId, result.Self.UserId);

            await socket.CloseAsync();
        }

        [Fact]
        public async Task ShouldAddAndRemoveFromMatchmaker()
        {
            var client2 = ClientUtil.FromSettingsFile();

            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await client2.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(client2);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);

            var partyCreatedTCS = new TaskCompletionSource<IParty>();

            socket1.ReceivedParty += (party) => partyCreatedTCS.SetResult(party);
            socket1.CreatePartyAsync(false, 1);

            var party = await partyCreatedTCS.Task;

            var partyJoinRequestTCS = new TaskCompletionSource<IPartyJoinRequest>();
            socket1.ReceivedPartyJoinRequest += (request) => partyJoinRequestTCS.SetResult(request);

            socket2.JoinPartyAsync(party.PartyId);
            var joinRequest = await partyJoinRequestTCS.Task;

            socket1.AcceptPartyMemberAsync(joinRequest.PartyId, joinRequest.Presences[0]);

            var partyPresenceJoinedTCS = new TaskCompletionSource<IPartyPresenceEvent>();
            socket1.ReceivedPartyPresence += (presenceEvt) => partyPresenceJoinedTCS.SetResult(presenceEvt);

            await partyJoinRequestTCS.Task;

            var partyMatchmakingTCS = new TaskCompletionSource<IPartyMatchmakerTicket>();

            socket1.ReceivedPartyMatchmakerTicket += (ticket) => partyMatchmakingTCS.SetResult(ticket);

            socket1.AddMatchmakerPartyAsync(party.PartyId, "*", 2, 2);

            IPartyMatchmakerTicket ticket = await partyMatchmakingTCS.Task;

            Assert.False(string.IsNullOrEmpty(ticket.Ticket));
            await socket1.RemoveMatchmakerPartyAsync(party.PartyId, ticket.Ticket);
            await socket1.CloseAsync();
        }

        [Fact]
        public async Task ShouldPromoteMember()
        {
            var client2 = ClientUtil.FromSettingsFile();

            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await client2.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client);

            var socket2 = Nakama.Socket.From(client2);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);

            var partyCreatedTCS = new TaskCompletionSource<IParty>();

            socket1.ReceivedParty += (party) => partyCreatedTCS.SetResult(party);
            socket1.CreatePartyAsync(false, 1);

            var party = await partyCreatedTCS.Task;

            var partyJoinRequestTCS = new TaskCompletionSource<IPartyJoinRequest>();
            socket1.ReceivedPartyJoinRequest += (request) => partyJoinRequestTCS.SetResult(request);

            socket2.JoinPartyAsync(party.PartyId);
            var joinRequest = await partyJoinRequestTCS.Task;

            socket1.AcceptPartyMemberAsync(joinRequest.PartyId, joinRequest.Presences[0]);

            var partyPresenceJoinedTCS = new TaskCompletionSource<IPartyPresenceEvent>();
            socket1.ReceivedPartyPresence += (presenceEvt) => partyPresenceJoinedTCS.SetResult(presenceEvt);

            IPartyPresenceEvent socket2User = await partyPresenceJoinedTCS.Task;

            var partyPromoteTCS = new TaskCompletionSource<IPartyLeader>();
            socket1.ReceivedPartyLeader += (leader) => partyPromoteTCS.SetResult(leader);

            socket1.PromotePartyMember(party.PartyId, socket2User.Joins[0]);

            IPartyLeader newLeader = await partyPromoteTCS.Task;
            Assert.True(newLeader.Presence.UserId == session2.UserId);
        }
    }
}