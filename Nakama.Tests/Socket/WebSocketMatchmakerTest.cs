// Copyright 2020 The Nakama Authors
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests.Socket
{
    public class WebSocketMatchmakerTest : IAsyncLifetime
    {
        private readonly IClient _client;
        private readonly ISocket _socket;

        public WebSocketMatchmakerTest()
        {
            _client = TestsUtil.FromSettingsFile();
            _socket = Nakama.Socket.From(_client);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS_MATCHMAKER)]
        public async Task ShouldJoinMatchmaker()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.ConnectAsync(session);
            var matchmakerTicket = await _socket.AddMatchmakerAsync("*");

            Assert.NotNull(matchmakerTicket);
            Assert.NotEmpty(matchmakerTicket.Ticket);
        }

        // "Flakey. Needs improvement."
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS_MATCHMAKER)]
        public async Task ShouldJoinAndLeaveMatchmaker()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.ConnectAsync(session);
            var matchmakerTicket = await _socket.AddMatchmakerAsync("*");

            Assert.NotNull(matchmakerTicket);
            Assert.NotEmpty(matchmakerTicket.Ticket);
            await _socket.RemoveMatchmakerAsync(matchmakerTicket);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS_MATCHMAKER)]
        public async Task ShouldCompleteMatchmaker()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket2 = Nakama.Socket.From(_client);

            await _socket.ConnectAsync(session);
            await socket2.ConnectAsync(session2);

            var completer = new TaskCompletionSource<IMatchmakerMatched>();
            var completer2 = new TaskCompletionSource<IMatchmakerMatched>();
            _socket.ReceivedMatchmakerMatched += (state) => completer.SetResult(state);
            socket2.ReceivedMatchmakerMatched += (state) => completer2.SetResult(state);

            var matchmakerTicket = await _socket.AddMatchmakerAsync("*", 2, 2);
            await Task.Delay(2000);
            var matchmakerTicket2 = await socket2.AddMatchmakerAsync("*", 2, 2);

            Assert.NotNull(matchmakerTicket);
            Assert.NotEmpty(matchmakerTicket.Ticket);
            Assert.NotNull(matchmakerTicket2);
            Assert.NotEmpty(matchmakerTicket2.Ticket);

            var result = await completer.Task;
            var result2 = await completer2.Task;
            Assert.NotNull(result);
            Assert.NotNull(result2);
            Assert.NotEmpty(result.Token);
            Assert.NotEmpty(result2.Token);
            Assert.Equal(result.Token, result2.Token);
        }

         [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS_MATCHMAKER)]
        public async Task ShouldNotMatchPartiesWithACombinedAmountOfPlayersAboveMaxCount()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session3 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session4 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client);
            var socket2 = Nakama.Socket.From(_client);
            var socket3 = Nakama.Socket.From(_client);
            var socket4 = Nakama.Socket.From(_client);

            await socket1.ConnectAsync(session1);
            await socket2.ConnectAsync(session2);
            await socket3.ConnectAsync(session3);
            await socket4.ConnectAsync(session4);

            var party1PresenceJoinedTcs = new TaskCompletionSource<IPartyPresenceEvent>();
            socket1.ReceivedPartyPresence += presenceEvt => party1PresenceJoinedTcs.SetResult(presenceEvt);

            var party2PresenceJoinedTcs = new TaskCompletionSource<IPartyPresenceEvent>();
            socket3.ReceivedPartyPresence += presenceEvt => party2PresenceJoinedTcs.SetResult(presenceEvt);

            var mmCompleter1 = new TaskCompletionSource<IMatchmakerMatched>();
            var mmCompleter2 = new TaskCompletionSource<IMatchmakerMatched>();
            socket1.ReceivedMatchmakerMatched += (state) => mmCompleter1.SetResult(state);
            socket3.ReceivedMatchmakerMatched += (state) => mmCompleter2.SetResult(state);

            var party1 = await socket1.CreatePartyAsync(true, 2);
            var party2 = await socket3.CreatePartyAsync(true, 2);

            await socket2.JoinPartyAsync(party1.Id);
            await socket4.JoinPartyAsync(party2.Id);

            await party1PresenceJoinedTcs.Task;
            await party2PresenceJoinedTcs.Task;

            var addPartyResult1 = await socket1.AddMatchmakerPartyAsync(party1.Id, "*", 3, 3);
            var addPartyResult2 = await socket3.AddMatchmakerPartyAsync(party2.Id, "*", 3, 3);

            Assert.NotEmpty(addPartyResult1.Ticket);
            Assert.NotEmpty(addPartyResult2.Ticket);

            await Task.Delay(1000);

            Assert.False(mmCompleter1.Task.IsCompleted);
            Assert.False(mmCompleter2.Task.IsCompleted);

            await socket1.CloseAsync();
            await socket2.CloseAsync();
            await socket3.CloseAsync();
            await socket4.CloseAsync();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS_MATCHMAKER)]
        public async Task ShouldMatchPartiesWithPlayers()
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

            var partyPresenceJoinedTcs = new TaskCompletionSource<IPartyPresenceEvent>();
            socket1.ReceivedPartyPresence += presenceEvt => partyPresenceJoinedTcs.SetResult(presenceEvt);
            
            var mmCompleter1 = new TaskCompletionSource<IMatchmakerMatched>();
            var mmCompleter2 = new TaskCompletionSource<IMatchmakerMatched>();
            socket1.ReceivedMatchmakerMatched += (state) => mmCompleter1.SetResult(state);
            socket3.ReceivedMatchmakerMatched += (state) => mmCompleter2.SetResult(state);
            
            var party1 = await socket1.CreatePartyAsync(true, 2);
            await socket2.JoinPartyAsync(party1.Id);

            await partyPresenceJoinedTcs.Task;

            var addPartyResult = await socket1.AddMatchmakerPartyAsync(party1.Id, "*", 3, 3);
            var addPlayerResult = await socket3.AddMatchmakerAsync( "*", 3, 3);

            Assert.NotEmpty(addPartyResult.Ticket);
            Assert.NotEmpty(addPlayerResult.Ticket);

            var partyMatchResult = await mmCompleter1.Task;
            var playerMatchResult = await mmCompleter2.Task;

            Assert.NotEmpty(partyMatchResult.Users);
            Assert.NotEmpty(playerMatchResult.Users);

            await socket1.CloseAsync();
            await socket2.CloseAsync();
            await socket3.CloseAsync();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS_MATCHMAKER)]
        public async Task ShouldCompleteMatchmakerAsymmetricQuery()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket2 = Nakama.Socket.From(_client);

            await _socket.ConnectAsync(session);
            await socket2.ConnectAsync(session2);

            var completer = new TaskCompletionSource<IMatchmakerMatched>();
            var completer2 = new TaskCompletionSource<IMatchmakerMatched>();
            _socket.ReceivedMatchmakerMatched += (state) => completer.SetResult(state);
            socket2.ReceivedMatchmakerMatched += (state) => completer2.SetResult(state);

            var properties = new Dictionary<string, string>();
            properties.Add("code", "test1");
            var properties2 = new Dictionary<string, string>();
            properties2.Add("code", "test2");

            var matchmakerTicket = await _socket.AddMatchmakerAsync("properties.code:* properties.code:test1^5", 2, 2, properties);
            var matchmakerTicket2 = await socket2.AddMatchmakerAsync("*", 2, 2, properties2);

            Assert.NotNull(matchmakerTicket);
            Assert.NotEmpty(matchmakerTicket.Ticket);
            Assert.NotNull(matchmakerTicket2);
            Assert.NotEmpty(matchmakerTicket2.Ticket);

            await Task.Delay(1000);

            var result = await completer.Task;
            var result2 = await completer2.Task;
            Assert.NotNull(result);
            Assert.NotNull(result2);
            Assert.NotEmpty(result.Token);
            Assert.NotEmpty(result2.Token);
            Assert.Equal(result.Token, result2.Token);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS_MATCHMAKER)]
        public async Task ShouldCompleteMatchmakerSymmetricQueryMidSize()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session3 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket2 = Nakama.Socket.From(_client);
            var socket3 = Nakama.Socket.From(_client);

            await _socket.ConnectAsync(session);
            await socket2.ConnectAsync(session2);
            await socket3.ConnectAsync(session3);

            var completer = new TaskCompletionSource<IMatchmakerMatched>();
            var completer2 = new TaskCompletionSource<IMatchmakerMatched>();
            var completer3 = new TaskCompletionSource<IMatchmakerMatched>();
            _socket.ReceivedMatchmakerMatched += (state) => completer.SetResult(state);
            socket2.ReceivedMatchmakerMatched += (state) => completer2.SetResult(state);
            socket3.ReceivedMatchmakerMatched += (state) => completer3.SetResult(state);

            var properties = new Dictionary<string, string>();
            properties.Add("foo", "bar");
            var properties2 = new Dictionary<string, string>();
            properties2.Add("foo", "bar");
            var properties3 = new Dictionary<string, string>();
            properties3.Add("foo", "bar");

            var query = "+properties.foo:bar";
            var query2 = "+properties.foo:bar";
            var query3 = "+properties.foo:bar";

            var matchmakerTicket = await _socket.AddMatchmakerAsync(query, 3, 3, properties);
            var matchmakerTicket2 = await socket2.AddMatchmakerAsync(query2, 3, 3, properties2);
            var matchmakerTicket3 = await socket3.AddMatchmakerAsync(query3, 3, 3, properties3);

            Assert.NotNull(matchmakerTicket);
            Assert.NotEmpty(matchmakerTicket.Ticket);
            Assert.NotNull(matchmakerTicket2);
            Assert.NotEmpty(matchmakerTicket2.Ticket);
            Assert.NotNull(matchmakerTicket3);
            Assert.NotEmpty(matchmakerTicket3.Ticket);

            await Task.Delay(3000);

            var result = await completer.Task;
            var result2 = await completer2.Task;
            var result3 = await completer3.Task;
            Assert.NotNull(result);
            Assert.NotNull(result2);
            Assert.NotNull(result3);
            Assert.NotEmpty(result.Token);
            Assert.NotEmpty(result2.Token);
            Assert.NotEmpty(result3.Token);
            Assert.Equal(result.Token, result2.Token);
            Assert.Equal(result2.Token, result3.Token);
            Assert.Equal(result.Token, result3.Token);
        }

        Task IAsyncLifetime.InitializeAsync()
        {
            return Task.CompletedTask;
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            return _socket.CloseAsync();
        }
    }
}
