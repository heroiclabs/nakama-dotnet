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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Satori.Tests
{
    public class ClientIdentifyTest
    {
        private readonly Client _client;
        private readonly ITestOutputHelper _outputHelper;

        public ClientIdentifyTest(ITestOutputHelper helper)
        {
            _client = new Client("http", "127.0.0.1", 7450, "db97a14b-74dc-4809-9967-276799a535c4");
            _outputHelper = helper;
        }

        [Fact]
        public async Task IdentifyEvents()
        {
            var session1 = await _client.AuthenticateAsync("11111111-0000-0000-0000-000000000000");

            var props1 = new Dictionary<string, string> { { "email", "a@b.com" }, { "pushTokenIos", "foo" } };
            var customProps1 = new Dictionary<string, string> { { "earlyAccess", "true" } };
            await _client.UpdatePropertiesAsync(session1, props1, customProps1);

            var events = new[]
                { new Event("awardReceived", DateTime.UtcNow), new Event("inventoryUpdated", DateTime.UtcNow) };
            await _client.EventsAsync(session1, events);

            Thread.Sleep(2000);

            var session2 = await _client.AuthenticateAsync("22222222-0000-0000-0000-000000000000");

            var props2 = new Dictionary<string, string> { { "email", "a@b.com" }, { "pushTokenAndroid", "bar" } };
            var customProps2 = new Dictionary<string, string> { { "earlyAccess", "false" } };
            await _client.UpdatePropertiesAsync(session2, props2, customProps2);

            Thread.Sleep(2000);

            var session = await _client.IdentifyAsync(session1, "22222222-0000-0000-0000-000000000000",
                new Dictionary<string, string>(), new Dictionary<string, string>());

            Assert.NotNull(session);
            Assert.Equal("22222222-0000-0000-0000-000000000000", session.IdentityId);

            var props = await _client.ListPropertiesAsync(session);
            Assert.NotEmpty(props.Default);
            Assert.NotEmpty(props.Custom);

            foreach (var prop in props.Default)
            {
                switch (prop.Key)
                {
                    case "email":
                        Assert.Equal("a@b.com", prop.Value);
                        break;
                    case "pushTokenAndroid":
                        Assert.Equal("bar", prop.Value);
                        break;
                    case "pushTokenIos":
                        Assert.Equal("foo", prop.Value);
                        break;
                }
            }

            foreach (var prop in props.Custom)
            {
                switch (prop.Key)
                {
                    case "earlyAccess":
                        Assert.Equal("false", prop.Value);
                        break;
                }
            }
        }
    }
}
