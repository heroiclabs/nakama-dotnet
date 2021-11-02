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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NakamaSync;
using Xunit;

namespace Nakama.Tests.Sync
{
    public class SyncTest
    {
        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private async void SharedVarShouldRetainData()
        {
            var testEnv = new SyncTestEnvironment(
                SyncTestsUtil.DefaultOpcodes(),
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            await testEnv.Start();
            SyncTestSharedVars creatorEnv = testEnv.GetCreator().SharedVars;
            creatorEnv.SharedBools[0].SetValue(true);
            Assert.True(creatorEnv.SharedBools[0].GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private async void OtherVarShouldRetainData()
        {
            var testEnv = new SyncTestEnvironment(
                SyncTestsUtil.DefaultOpcodes(),
                numClients: 2,
                numOtherVarCollections: 1,
                numOtherVarsPerCollection: 1,
                creatorIndex: 0);

            await testEnv.Start();
            SyncTestUserEnvironment creatorEnv = testEnv.GetCreator();
            creatorEnv.OtherVars.BoolSelfVars["presenceBools_0"].SetValue(true);
            Assert.True(creatorEnv.OtherVars.BoolSelfVars["presenceBools_0"].GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private async Task BadHandshakeShouldFail()
        {
            VarIdGenerator idGenerator = (string userId, string varName, int varId) => {

                // create "mismatched" keys, i.e., keys with different ids for each client, to
                // simulate clients using different app binaries.
                return userId + varName + varId.ToString();
            };

            bool threwError = false;

            SyncErrorHandler errorHandler = e =>
            {
                if (e is HandshakeFailedException)
                {
                    threwError = true;
                }
            };

            var mismatchedEnv = new SyncTestEnvironment(
                SyncTestsUtil.DefaultOpcodes(),
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0,
                null,
                idGenerator);


            await mismatchedEnv.Start(errorHandler);

            await Task.Delay(2000);
            Assert.True(threwError);
            mismatchedEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncData()
        {
            var testEnv = new SyncTestEnvironment(
                SyncTestsUtil.DefaultOpcodes(),
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            await testEnv.Start();

            var allEnvs = testEnv.GetAllEnvs();

            allEnvs[0].SharedVars.SharedInts[0].SetValue(5);

            await Task.Delay(1000);

            Assert.Equal(5, allEnvs[1].SharedVars.SharedInts[0].GetValue());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private async Task OtherVarShouldSyncData()
        {
            var testEnv = new SyncTestEnvironment(
                SyncTestsUtil.DefaultOpcodes(),
                numClients: 2,
                numOtherVarCollections: 1,
                numOtherVarsPerCollection: 1,
                creatorIndex: 0);

            await testEnv.Start();

            SyncTestUserEnvironment creatorEnv = testEnv.GetCreator();
            creatorEnv.OtherVars.BoolSelfVars["presenceBools_0"].SetValue(true);

            await Task.Delay(2500);

            IUserPresence nonCreatorPresence = testEnv.GetRandomNonCreatorPresence();

            var nonCreatorEnv = testEnv.GetUserEnv(nonCreatorPresence);

            string creatorId = creatorEnv.Self.UserId;
            string nonCreatorId = nonCreatorEnv.Self.UserId;

            var nonCreatorPresenceBools = nonCreatorEnv.OtherVars.BoolOtherVars["presenceBools_0"];

            var creatorOtherVarInGuest = nonCreatorPresenceBools.First(var => {
                return var.Presence.UserId == creatorId;
            });

            Assert.True(nonCreatorPresenceBools[0].GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private async void EnvsShouldBeSeparate()
        {
            var testEnv = new SyncTestEnvironment(
                SyncTestsUtil.DefaultOpcodes(),
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            await testEnv.Start();

            List<SyncTestUserEnvironment> allEnvs = testEnv.GetAllEnvs();
            var env1 = allEnvs[0];
            var env2 = allEnvs[1];
            Assert.NotEqual(env1.Self.UserId, env2.Self.UserId);
            testEnv.Dispose();
        }


        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private async void HostShouldBeChosen()
        {
            var testEnv = new SyncTestEnvironment(
                SyncTestsUtil.DefaultOpcodes(),
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            await testEnv.Start();
            var env1 = testEnv.GetUserEnv(testEnv.GetCreatorPresence());
            var env2 = testEnv.GetUserEnv(testEnv.GetRandomNonCreatorPresence());
            Assert.NotEqual(env1.Self.UserId, env2.Self.UserId);
            Assert.True(env1.Match.IsSelfHost() || env2.Match.IsSelfHost());
            testEnv.Dispose();
        }
    }
}
