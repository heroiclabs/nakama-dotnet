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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NakamaSync;
using Xunit;

namespace Nakama.Tests.Sync
{
    public class SyncTest
    {
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void SharedVarShouldRetainData()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            await testEnv.Start();
            SyncTestSharedVars creatorEnv = testEnv.GetCreator().SharedVars;
            creatorEnv.SharedBool.SetValue(true);
            Assert.True(creatorEnv.SharedBool.GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void PresenceVarShouldRetainData()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numPresenceVarCollections: 1,
                numPresenceVarsPerCollection: 1,
                creatorIndex: 0);

            await testEnv.Start();
            SyncTestUserEnvironment creatorEnv = testEnv.GetCreator();
            creatorEnv.PresenceVars.BoolGroupVar.Self.SetValue(true);
            Assert.True(creatorEnv.PresenceVars.BoolGroupVar.GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncData()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            await testEnv.Start();

            var allEnvs = testEnv.GetAllEnvs();

            allEnvs[0].SharedVars.SharedInt.SetValue(5);

            await Task.Delay(1000);

            Assert.Equal(5, allEnvs[1].SharedVars.SharedInt.GetValue());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncDict()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            await testEnv.Start();

            var allEnvs = testEnv.GetAllEnvs();

            var dict = new Dictionary<string, string>();
            dict["hello"] = "world";

            allEnvs[0].SharedVars.SharedDict.SetValue(dict);

            await Task.Delay(1000);

            var env1Dict = allEnvs[1].SharedVars.SharedDict;

            Assert.NotNull(env1Dict.GetValue());
            Assert.True(env1Dict.GetValue().ContainsKey("hello"));

            Assert.Equal("world", env1Dict.GetValue()["hello"]);

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task PresenceVarShouldSyncData()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numPresenceVarCollections: 1,
                numPresenceVarsPerCollection: 1,
                creatorIndex: 0);

            await testEnv.Start();

            SyncTestUserEnvironment creatorEnv = testEnv.GetCreator();
            creatorEnv.PresenceVars.BoolGroupVar.Self.SetValue(true);

            await Task.Delay(2500);

            IUserPresence nonCreatorPresence = testEnv.GetRandomNonCreatorPresence();

            var nonCreatorEnv = testEnv.GetUserEnv(nonCreatorPresence);

            string creatorId = creatorEnv.Self.UserId;
            string nonCreatorId = nonCreatorEnv.Self.UserId;

            var nonCreatorPresenceBools = nonCreatorEnv.PresenceVars.BoolGroupVar.Others;

            var creatorPresenceVarInGuest = nonCreatorPresenceBools.First(var => {
                return var.Presence.UserId == creatorId;
            });

            Assert.True(creatorPresenceVarInGuest.GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void EnvsShouldBeSeparate()
        {
            var testEnv = new SyncTestEnvironment(
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

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void HostShouldBeChosen()
        {
            var testEnv = new SyncTestEnvironment(
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
