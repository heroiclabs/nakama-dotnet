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
using System.Collections.Generic;
using System.Threading.Tasks;
using Nakama.TinyJson;
using Xunit;

namespace Nakama.Tests.Socket
{
    public class WebSocketRpcTest : IAsyncLifetime
    {
        private readonly IClient _client;
        private readonly ISocket _socket;

        public WebSocketRpcTest()
        {
            _client = TestsUtil.FromSettingsFile();
            _socket = Nakama.Socket.From(_client);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldSendRpcRoundtrip()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _socket.ConnectAsync(session);

            const string funcId = "clientrpc.rpc";
            var payload = new Dictionary<string, string> {{"hello", "world"}}.ToJson();
            var response = await _socket.RpcAsync(funcId, payload);

            Assert.NotNull(response);
            Assert.Equal(funcId, response.Id);
            Assert.Equal(payload, response.Payload);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public Task DisposeAsync() => _socket.CloseAsync();
    }
}
