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

namespace Nakama.Tests.Socket
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using TinyJson;

    // "Flakey. Needs improvement."
    public class WebSocketMatchTest
    {
        private IClient _client;

        // ReSharper disable RedundantArgumentDefaultValue
        public WebSocketMatchTest()
        {
            _client = ClientUtil.FromSettingsFile();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldCreateMatch(TestAdapterFactory adapterFactory)
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket = Nakama.Socket.From(_client, adapterFactory());

            await socket.ConnectAsync(session);
            var match = await socket.CreateMatchAsync();

            Assert.NotNull(match);
            Assert.NotNull(match.Id);
            Assert.NotEmpty(match.Id);
            Assert.False(match.Authoritative);
            Assert.True(match.Size > 0);

            await socket.CloseAsync();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldCreateMatchAndSecondUserJoin(TestAdapterFactory adapterFactory)
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket1 = Nakama.Socket.From(_client, adapterFactory());
            await socket1.ConnectAsync(session1);

            var socket2 = Nakama.Socket.From(_client, adapterFactory());
            await socket2.ConnectAsync(session2);

            var match1 = await socket1.CreateMatchAsync();
            var match2 = await socket2.JoinMatchAsync(match1.Id);

            Assert.NotNull(match1);
            Assert.NotNull(match2);
            Assert.Equal(match1.Id, match2.Id);
            Assert.Equal(match1.Label, match2.Label);

            Assert.True(match1.Presences.Count() == 0 && match1.Self.UserId == session1.UserId);
            Assert.True(match2.Presences.Count() == 1);

            await socket1.CloseAsync();
            await socket2.CloseAsync();
        }


        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldCreateMatchAndLeave(TestAdapterFactory adapterFactory)
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket = Nakama.Socket.From(_client, adapterFactory());
            await socket.ConnectAsync(session);
            var match = await socket.CreateMatchAsync();

            Assert.NotNull(match);
            Assert.NotNull(match.Id);

            await socket.LeaveMatchAsync(match.Id);
            await socket.CloseAsync();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldCreateMatchAndSendState(TestAdapterFactory adapterFactory)
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var socket = Nakama.Socket.From(_client, adapterFactory());
            await socket.ConnectAsync(session1);

            var socket2 = Nakama.Socket.From(_client, adapterFactory());
            var completer = new TaskCompletionSource<IMatchState>();
            socket2.ReceivedMatchState += (state) => completer.SetResult(state);
            await socket2.ConnectAsync(session2);

            var match = await socket.CreateMatchAsync();
            await socket2.JoinMatchAsync(match.Id);

            var newState = new Dictionary<string, string> {{"hello", "world"}}.ToJson();
            await socket.SendMatchStateAsync(match.Id, 0, newState);

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Equal(newState, Encoding.UTF8.GetString(result.State));

            await socket.CloseAsync();
            await socket2.CloseAsync();
        }
    }
}
