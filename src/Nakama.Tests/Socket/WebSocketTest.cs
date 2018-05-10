/**
 * Copyright 2018 The Nakama Authors
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
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class WebSocketTest
    {
        [Test]
        public async Task ShouldConnect()
        {
            var customid = Guid.NewGuid();

            IClient client = new Client();
            var session = await client.AuthenticateCustomAsync(customid.ToString());
            var socket = await client.CreateWebSocketAsync(session);

            Assert.NotNull(session);
            Assert.NotNull(session.UserId);
            Assert.NotNull(session.Username);
            Assert.NotNull(socket);

            await socket.DisconnectAsync();
        }
    }
}
