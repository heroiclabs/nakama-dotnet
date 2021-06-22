/**
 * Copyright 2021 The Nakama Authors
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

using NakamaSync;
using Xunit;

namespace Nakama.Tests.Socket
{
    public class WebSocketSyncedTest
    {
        private readonly SyncedTestEnvironment _testEnv;

        public WebSocketSyncedTest()
        {
            _testEnv = new SyncedTestEnvironment(
                new SyncedOpcodes(handshakeOpcode: 0, dataOpcode: 1),
                numClients: 5,
                numTestVars: 1,
                hostIndex: 0);
        }

        [Fact]
        private void SharedVarShouldRetainData()
        {
            SyncedTestUserEnvironment hostEnv = _testEnv.GetUserEnv(_testEnv.Host);
            hostEnv.SharedBools[0].SetValue(true);
            Assert.True(hostEnv.SharedBools[0].GetValue());
        }

        [Fact]
        private void UserVarShouldRetainData()
        {
            SyncedTestUserEnvironment hostEnv = _testEnv.GetHostEnv();
            hostEnv.UserBools[0].SetValue(true, _testEnv.Host);
            Assert.True(hostEnv.UserBools[0].GetValue(_testEnv.Host));
        }

        [Fact]
        private void BadHandshakeShouldFail()
        {

        }

        [Fact]
        private void SharedVarShouldSyncData()
        {
            SyncedTestUserEnvironment hostEnv = _testEnv.GetHostEnv();
            hostEnv.SharedBools[0].SetValue(true);

            SyncedTestUserEnvironment guestEnv = _testEnv.GetRandomGuestEnv();
            Assert.True(guestEnv.SharedBools[0].GetValue());
        }

        public void Dispose()
        {
            _testEnv.Dispose();
        }
    }
}
