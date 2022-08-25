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
    public class SatoriTest
    {
        private const string _API_KEY = "bb4b2da1-71ba-429e-b5f3-36556abbf4c9";
        public const int _TIMEOUT_MILLISECONDS = 5000;

        private readonly Client _testClient = new Client("http", "localhost", 7450, _API_KEY, Nakama.HttpRequestAdapter.WithGzip());

        [Fact(Timeout = _TIMEOUT_MILLISECONDS)]
        public async Task TestAuthenticateAndLogout()
        {
            var session = await _testClient.AuthenticateAsync($"{Guid.NewGuid()}");
            await _testClient.AuthenticateLogoutAsync(session);
            await Assert.ThrowsAsync<Nakama.ApiResponseException>(() => _testClient.GetExperimentsAsync(session, new string[]{}));
        }

        [Fact(Timeout = _TIMEOUT_MILLISECONDS)]
        public async Task TestGetExperiments()
        {
            var session = await _testClient.AuthenticateAsync($"{Guid.NewGuid()}");
            var experiments = await _testClient.GetExperimentsAsync(session);

            Assert.True(experiments.Experiments.Count() == 1);
        }

        [Fact(Timeout = _TIMEOUT_MILLISECONDS)]
        public async Task TestGetFlags()
        {
            var session = await _testClient.AuthenticateAsync($"{Guid.NewGuid()}");
            var flags = await _testClient.GetFlagsAsync(session, new string[]{});
            Assert.True(flags.Flags.Count() == 3);
            var namedFlags = await _testClient.GetFlagsAsync(session, new string[]{"MinBuildNumber"});
            Assert.True(namedFlags.Flags.Count() == 1);
        }

        [Fact(Timeout = _TIMEOUT_MILLISECONDS)]
        public async Task TestGetFlagsDefault()
        {
            var flags = await _testClient.GetFlagsDefaultAsync(_API_KEY, new string[]{});
            Assert.True(flags.Flags.Count() == 3);
        }

        [Fact(Timeout = _TIMEOUT_MILLISECONDS)]
        public async Task TestSendEvents()
        {
            var session = await _testClient.AuthenticateAsync($"{Guid.NewGuid()}");
            await _testClient.SendEventAsync(session, new Event("event", DateTime.UtcNow));
            await _testClient.SendEventsAsync(session, new Event[]{new Event("event1", DateTime.UtcNow), new Event("event2", DateTime.UtcNow)});
        }

        [Fact(Timeout = _TIMEOUT_MILLISECONDS)]
        public async Task TestGetLiveEvent()
        {
            var session = await _testClient.AuthenticateAsync($"{Guid.NewGuid()}");
            var liveEvents = await _testClient.GetLiveEventsAsync(session);
            // should not receive any event because not in the targeted audiences
            Assert.True(liveEvents.LiveEvents.Count() == 0);
        }
    }
}
