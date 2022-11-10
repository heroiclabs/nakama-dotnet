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

namespace Nakama.Tests.Api
{
    using System;
    using System.Threading.Tasks;
    using Xunit;

    // NOTE: Requires Lua modules from server repo.

    public class RpcTest
    {
        private IClient _client;

        // ReSharper disable RedundantArgumentDefaultValue

        public RpcTest()
        {
            _client = TestsUtil.FromSettingsFile();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldRpcRoundtrip()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            const string funcid = "clientrpc.rpc";
            const string payload = "{\"hello\": \"world\"}";
            var rpc = await _client.RpcAsync(session, funcid, payload);

            Assert.NotNull(rpc);
            Assert.Equal(payload, rpc.Payload);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldRpcGet()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            const string funcid = "clientrpc.rpc_get";
            var rpc = await _client.RpcAsync(session, funcid);

            Assert.NotNull(rpc);
            Assert.Equal("{\"message\":\"PONG\"}", rpc.Payload);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldRpcGetRoundtrip()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            const string funcid = "clientrpc.rpc";
            const string payload = "{\"hello\": \"world\"}";
            var rpc = await _client.RpcAsync(session, funcid, payload);

            Assert.NotNull(rpc);
            Assert.Equal(payload, rpc.Payload);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldRpcWithoutSession()
        {
            // Http Key is used often for server to server function calls
            const string httpkey = "defaulthttpkey";
            const string funcid = "clientrpc.rpc_get";
            var rpc = await _client.RpcAsync(httpkey, funcid);

            Assert.NotNull(rpc);
            Assert.Equal("{\"message\":\"PONG\"}", rpc.Payload);
        }
        
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ShouldRpcGetRoundtripWithoutSession()
        {
            // Http Key is used often for server to server function calls
            const string httpkey = "defaulthttpkey";
            const string funcid = "clientrpc.rpc";
            const string payload = "{\"hello\": \"world\"}";
            var rpc = await _client.RpcAsync(httpkey, funcid, payload);

            Assert.NotNull(rpc);
            Assert.Equal(payload, rpc.Payload);
        }
    }
}
