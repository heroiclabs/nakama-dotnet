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

namespace Nakama.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class ClientTest
    {
        [Test]
        public void ShouldCreateClientDefaults()
        {
            IClient client = new Client();

            Assert.AreEqual("127.0.0.1", client.Host);
            Assert.AreEqual(7350, client.Port);
            Assert.AreEqual("defaultkey", client.ServerKey);
        }

        [Test]
        public void ShouldSetCustomLogger()
        {
            const string serverkey = "somesecretkey";
            const string host = "127.0.0.2";
            const int port = 443;
            const bool secure = true;

            var client = new Client(serverkey, host, port, secure);
            var logger = new SystemConsoleLogger();
            client.Logger = logger;

            Assert.AreEqual(serverkey, client.ServerKey);
            Assert.AreEqual(host, client.Host);
            Assert.AreEqual(port, client.Port);
            Assert.AreEqual(secure, client.Secure);
            Assert.AreSame(logger, client.Logger);
        }

        [Test]
        public void ShouldSetRetriesAndTraceAndTimeout()
        {
            const int retries = 5;
            const int timeout = 10000;
            var client = new Client
            {
                Retries = retries,
                Trace = true,
                Timeout = timeout
            };

            Assert.AreEqual(retries, client.Retries);
            Assert.AreEqual(timeout, client.Timeout);
            Assert.IsTrue(client.Trace);
        }
    }
}
