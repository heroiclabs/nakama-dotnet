// Copyright 2022 The Nakama Authors
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

namespace Satori.Tests
{
    public class ClientTest
    {
        private const string ApiKey = "55050223-cffb-4d92-b9fe-8a6219e0f87b";
        public const int TimeoutMilliseconds = 5000;

        private readonly Client _testClient =
            new Client("http", "localhost", 7450, ApiKey, HttpRequestAdapter.WithGzip());

        [Fact(Timeout = TimeoutMilliseconds)]
        public async Task TestAuthenticateAndLogout()
        {
            var session = await _testClient.AuthenticateAsync($"{Guid.NewGuid()}");
            await _testClient.AuthenticateLogoutAsync(session);
            await Assert.ThrowsAsync<ApiResponseException>(() =>
                _testClient.GetExperimentsAsync(session, Array.Empty<string>(), Array.Empty<string>()));
        }

        [Fact(Timeout = TimeoutMilliseconds)]
        public async Task TestGetExperiments()
        {
            var session = await _testClient.AuthenticateAsync($"{Guid.NewGuid()}");
            var experiments = await _testClient.GetAllExperimentsAsync(session);

            Assert.True(experiments.Experiments.Count() == 1);
        }

        [Fact(Timeout = TimeoutMilliseconds)]
        public async Task TestGetFlags()
        {
            var session = await _testClient.AuthenticateAsync($"{Guid.NewGuid()}");
            var flags = await _testClient.GetFlagsAsync(session, new string[] { }, Array.Empty<string>());
            var excludeHiroFlags = flags.Flags.Where(flag => !flag.Name.StartsWith("Hiro"));
            Assert.True(excludeHiroFlags.Count() == 4);
            var namedFlags = await _testClient.GetFlagsAsync(session, new[] { "Min-Build-Number" }, Array.Empty<string>());
            Assert.True(namedFlags.Flags.Count() == 1);
        }

        [Fact(Timeout = TimeoutMilliseconds)]
        public async Task TestGetFlagsDefault()
        {
            var flags = await _testClient.GetFlagsDefaultAsync(new string[] { }, Array.Empty<string>());

            var excludeHiroFlags = flags.Flags.Where(flag => !flag.Name.StartsWith("Hiro"));
            Assert.True(excludeHiroFlags.Count() == 4);
        }

        [Fact(Timeout = TimeoutMilliseconds)]
        public async Task TestSendEvents()
        {
            var session = await _testClient.AuthenticateAsync($"{Guid.NewGuid()}");
            await _testClient.EventAsync(session, new Event("gameFinished", DateTime.UtcNow));
            await _testClient.EventsAsync(session,
                new[] { new Event("adStarted", DateTime.UtcNow), new Event("appLaunched", DateTime.UtcNow) });
        }

        [Fact(Timeout = TimeoutMilliseconds)]
        public async Task TestGetLiveEvent()
        {
            var session = await _testClient.AuthenticateAsync($"{Guid.NewGuid()}");
            var liveEvents = await _testClient.GetLiveEventsAsync(session);
            // should not receive any event because not in the targeted audiences
            Assert.True(!liveEvents.LiveEvents.Any());
        }
    }
}
