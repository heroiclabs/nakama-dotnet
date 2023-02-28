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
    public class PresenceUtilTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IClient _client;

        public PresenceUtilTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _client = TestsUtil.FromSettingsFile();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldUpdatePresencesMatch()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket1 = Nakama.Socket.From(_client);
            await socket1.ConnectAsync(session);

            var createdParty = await socket1.CreatePartyAsync(true, 2);

            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket2 = Nakama.Socket.From(_client);
            await socket2.ConnectAsync(session2);

            var partyPresenceEventTcs = new TaskCompletionSource<IPartyPresenceEvent>();

            socket1.ReceivedPartyPresence += presenceEvent =>
            {
                createdParty.UpdatePresences(presenceEvent);
                partyPresenceEventTcs.SetResult(presenceEvent);
            };

            socket2.JoinPartyAsync(createdParty.Id);

            System.Console.WriteLine("waiting 2");
            await partyPresenceEventTcs.Task;
            System.Console.WriteLine("waiting 3");

            Assert.Equal(2, createdParty.Presences.Count());
            await socket1.CloseAsync();
        }
    }
}
