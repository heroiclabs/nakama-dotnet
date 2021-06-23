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
        [Fact]
        private void SharedVarShouldRetainData()
        {
            var testEnv = CreateDefaultEnvironment();
            SyncedTestUserEnvironment hostEnv = testEnv.GetUserEnv(testEnv.Host);
            hostEnv.SharedBools[0].SetValue(true);
            Assert.True(hostEnv.SharedBools[0].GetValue());
            testEnv.Dispose();
        }

        [Fact]
        private void UserVarShouldRetainData()
        {
            var testEnv = CreateDefaultEnvironment();
            SyncedTestUserEnvironment hostEnv = testEnv.GetHostEnv();
            hostEnv.UserBools[0].SetValue(true, testEnv.Host);
            Assert.True(hostEnv.UserBools[0].GetValue(testEnv.Host));
            testEnv.Dispose();
        }

        [Fact]
        private void BadHandshakeShouldFail()
        {
            var mismatchedEnv = new SyncedTestEnvironment(
                new SyncedOpcodes(handshakeOpcode: 0, dataOpcode: 1),
                numClients: 5,
                numTestVars: 1,
                hostIndex: 0);

            mismatchedEnv.Dispose();
        }

        [Fact]
        private void SharedVarShouldSyncData()
        {
            var testEnv = CreateDefaultEnvironment();
            SyncedTestUserEnvironment hostEnv = testEnv.GetHostEnv();
            hostEnv.SharedBools[0].SetValue(true);

            SyncedTestUserEnvironment guestEnv = testEnv.GetRandomGuestEnv();
            Assert.True(guestEnv.SharedBools[0].GetValue());
            testEnv.Dispose();
        }

        private SyncedTestEnvironment CreateDefaultEnvironment()
        {
            return new SyncedTestEnvironment(
                new SyncedOpcodes(handshakeOpcode: 0, dataOpcode: 1),
                numClients: 5,
                numTestVars: 1,
                hostIndex: 0);
        }
    }
}