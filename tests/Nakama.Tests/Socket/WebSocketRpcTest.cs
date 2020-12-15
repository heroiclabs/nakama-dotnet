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
    using System.Threading.Tasks;
    using Xunit;
    using TinyJson;

    public class WebSocketRpcTest
    {
        private IClient _client;

        public WebSocketRpcTest()
        {
            _client = ClientUtil.FromSettingsFile();
        }

        [Theory]
        [ClassData(typeof(WebSocketTestData))]
        public async Task ShouldSendRpcRoundtrip(TestAdapterFactory adapterFactory)
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var socket = Nakama.Socket.From(_client, adapterFactory());

            await socket.ConnectAsync(session);

            const string funcid = "clientrpc.rpc";
            var payload = new Dictionary<string, string> {{"hello", "world"}}.ToJson();
            var response = await socket.RpcAsync(funcid, payload);

            Assert.NotNull(response);
            Assert.Equal(funcid, response.Id);
            Assert.Equal(payload, response.Payload);

            await socket.CloseAsync();
        }
    }
}
