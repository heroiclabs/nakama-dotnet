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
            await testEnv.StartMatch(CreateDefaultErrorHandler());
            SyncTestUserEnvironment creatorEnv = testEnv.GetCreatorEnv();
            creatorEnv.SharedBools[0].SetValue(true);
            Assert.True(creatorEnv.SharedBools[0].GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task UserVarShouldRetainData()
        {
            var testEnv = CreateDefaultEnvironment();
            await testEnv.StartMatch(CreateDefaultErrorHandler());
            SyncTestUserEnvironment creatorEnv = testEnv.GetCreatorEnv();
            creatorEnv.UserBools[0].SetValue(true, testEnv.GetCreatorPresence());
            Assert.True(creatorEnv.UserBools[0].GetValue(testEnv.GetCreatorPresence()));
            testEnv.Dispose();
        }

        //todo unskip test
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS, Skip = "todo fix this")]
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
                creatorIndex: 0,
                idGenerator);


            await mismatchedEnv.StartMatch(CreateDefaultErrorHandler());

            //await Assert.ThrowsAsync<InvalidOperationException>(() => );

            mismatchedEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncData()
        {
            var testEnv = CreateDefaultEnvironment();
            await testEnv.StartMatch(CreateDefaultErrorHandler());

            SyncTestUserEnvironment creatorEnv = testEnv.GetCreatorEnv();
            creatorEnv.SharedBools[0].SetValue(true);

            SyncTestUserEnvironment guestEnv = testEnv.GetGuestEnv(testEnv.GetRandomGuestPresence());

            await Task.Delay(2500);

            Assert.True(guestEnv.SharedBools[0].GetValue());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task UserVarShouldSyncData()
        {
            var testEnv = CreateDefaultEnvironment();
            await testEnv.StartMatch(CreateDefaultErrorHandler());

            SyncTestUserEnvironment creatorEnv = testEnv.GetCreatorEnv();
            creatorEnv.UserBools[0].SetValue(true, testEnv.GetCreatorPresence());

            IUserPresence guestPresence = testEnv.GetGuests()[0];
            SyncTestUserEnvironment guestEnv = testEnv.GetUserEnv(guestPresence);
            await Task.Delay(2500);

            Assert.True(guestEnv.UserBools[0].HasValue(testEnv.GetCreatorPresence()));
            Assert.True(guestEnv.UserBools[0].GetValue(testEnv.GetCreatorPresence()));

            testEnv.Dispose();
        }


        // todo test variable status is intact after user leaves and then rejoins match (should pick up from
        // where they left off.)
        private SyncTestEnvironment CreateDefaultEnvironment()
        {
            return new SyncTestEnvironment(
                new SyncOpcodes(handshakeRequestOpcode: 0, handshakeResponseOpcode: 1, dataOpcode: 2),
                numClients: 2,
                numTestVars: 1,
                creatorIndex: 0);
        }

        private SyncErrorHandler CreateDefaultErrorHandler()
        {
            return (e) => new StdoutLogger().ErrorFormat(e.Message);
        }
    }
}
