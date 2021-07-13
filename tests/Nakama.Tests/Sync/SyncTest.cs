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

using System;
using System.Threading.Tasks;
using NakamaSync;
using Xunit;

namespace Nakama.Tests.Socket
{
    public class SyncTest
    {
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldRetainData()
        {
            var testEnv = CreateDefaultEnvironment();
            await testEnv.StartMatch();
            SyncTestUserEnvironment hostEnv = testEnv.GetHostEnv();
            hostEnv.SharedBools[0].SetValue(true);
            Assert.True(hostEnv.SharedBools[0].GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task UserVarShouldRetainData()
        {
            var testEnv = CreateDefaultEnvironment();
            await testEnv.StartMatch();
            SyncTestUserEnvironment hostEnv = testEnv.GetHostEnv();
            hostEnv.UserBools[0].SetValue(true, testEnv.GetHostPresence());
            Assert.True(hostEnv.UserBools[0].GetValue(testEnv.GetHostPresence()));
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task BadHandshakeShouldFail()
        {
            VarIdGenerator idGenerator = (string userId, string varName, int varId) => {

                // create "mismatched" keys, i.e., keys with different ids for each client, to
                // simulate clients using different app binaries.
                return userId + varName + varId.ToString();
            };

            var mismatchedEnv = new SyncTestEnvironment(
                new SyncOpcodes(handshakeRequestOpcode: 0, handshakeResponseOpcode: 1, dataOpcode: 2),
                numClients: 2,
                numTestVars: 1,
                hostIndex: 0,
                idGenerator);

            await Assert.ThrowsAsync<InvalidOperationException>(() => mismatchedEnv.StartMatch());

            mismatchedEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncData()
        {
            var testEnv = CreateDefaultEnvironment();
            await testEnv.StartMatch();

            SyncTestUserEnvironment hostEnv = testEnv.GetHostEnv();
            hostEnv.SharedBools[0].SetValue(true);

            SyncTestUserEnvironment guestEnv = testEnv.GetRandomGuestEnv();
            Assert.True(guestEnv.SharedBools[0].GetValue());

            testEnv.Dispose();
        }

        private SyncTestEnvironment CreateDefaultEnvironment()
        {
            return new SyncTestEnvironment(
                new SyncOpcodes(handshakeRequestOpcode: 0, handshakeResponseOpcode: 1, dataOpcode: 2),
                numClients: 5,
                numTestVars: 1,
                hostIndex: 0);
        }
    }
}
