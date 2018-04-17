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
    public class SessionTest
    {
        private const string AuthToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1MTY5MTA5NzMsInVpZCI6ImY0MTU4ZjJiLTgwZjMtNDkyNi05NDZiLWE4Y2NmYzE2NTQ5MCIsInVzbiI6InZUR2RHSHl4dmwifQ.gzLaMQPaj5wEKoskOSALIeJLOYXEVFoPx3KY0Jm1EVU";

        [Test]
        public void ShouldBeExpired()
        {
            var session = Session.Restore(AuthToken);
            Assert.NotNull(session.AuthToken);
            Assert.AreEqual(AuthToken, session.AuthToken);
            Assert.IsTrue(session.IsExpired);
            Assert.AreEqual(1516910973, session.ExpireTime);
            Assert.AreNotEqual(0, session.CreateTime);
        }

        [Test]
        public void ShouldHaveUsernameAndUserId()
        {
            var session = Session.Restore(AuthToken);
            Assert.NotNull(session.AuthToken);
            Assert.AreEqual(AuthToken, session.AuthToken);
            Assert.NotNull(session.Username);
            Assert.AreEqual("vTGdGHyxvl", session.Username);
            Assert.NotNull(session.UserId);
            Assert.AreEqual("f4158f2b-80f3-4926-946b-a8ccfc165490", session.UserId);
        }
    }
}
