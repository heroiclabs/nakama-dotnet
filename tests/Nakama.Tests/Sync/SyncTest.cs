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
            creatorEnv.GroupVars.GroupBool.Self.SetValue(true);
            Assert.True(creatorEnv.GroupVars.GroupBool.Self.GetValue());
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
        private async Task GroupVarShouldSyncData()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numPresenceVarCollections: 1,
                numPresenceVarsPerCollection: 1,
                creatorIndex: 0);

            await testEnv.Start();

            SyncTestUserEnvironment creatorEnv = testEnv.GetCreator();
            creatorEnv.GroupVars.GroupBool.Self.SetValue(true);

            var guestPresenceInCreatorEnv = testEnv.GetRandomNonCreatorPresence();

            await Task.Delay(1000);

            IUserPresence nonCreatorPresence = testEnv.GetRandomNonCreatorPresence();
            SyncTestUserEnvironment nonCreatorEnv = testEnv.GetUserEnv(nonCreatorPresence);

            string creatorId = creatorEnv.Self.UserId;
            string nonCreatorId = nonCreatorEnv.Self.UserId;

            var nonCreatorPresenceBools = nonCreatorEnv.GroupVars.GroupBool.Others;

            Assert.True(nonCreatorPresenceBools.Any());

            var creatorPresenceVarInGuest = nonCreatorPresenceBools.First(var => {
                return var.Presence.UserId == creatorId;
            });

            Assert.True(creatorPresenceVarInGuest.GetValue());
            Assert.False(creatorEnv.GroupVars.GroupBool.GetVar(guestPresenceInCreatorEnv).GetValue());
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

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncDataDeferred1()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0,
                delayRegistration: true
            );

            await testEnv.Start();

            var allEnvs = testEnv.GetAllEnvs();

            bool valueChanged = false;
            allEnvs[1].SharedVars.SharedInt.OnValueChanged += delegate { valueChanged = true; };

            allEnvs[0].SharedVars.SharedInt.SetValue(5);
            allEnvs[0].VarRegistry.Register(allEnvs[0].SharedVars.SharedInt);
            allEnvs[1].VarRegistry.Register(allEnvs[1].SharedVars.SharedInt);

            await Task.Delay(1000);

            Assert.True(valueChanged);
            Assert.Equal(5, allEnvs[1].SharedVars.SharedInt.GetValue());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncDataDeferred2()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0,
                delayRegistration: true
            );

            await testEnv.Start();

            var allEnvs = testEnv.GetAllEnvs();

            bool valueChanged = false;
            allEnvs[1].SharedVars.SharedInt.OnValueChanged += delegate { valueChanged = true; };

            allEnvs[0].VarRegistry.Register(allEnvs[0].SharedVars.SharedInt);
            allEnvs[1].VarRegistry.Register(allEnvs[1].SharedVars.SharedInt);
            allEnvs[0].SharedVars.SharedInt.SetValue(5);

            await Task.Delay(1000);


            Assert.True(valueChanged);
            Assert.Equal(5, allEnvs[1].SharedVars.SharedInt.GetValue());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task PresenceVarShouldSyncDataDeferred()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numPresenceVarCollections: 1,
                numPresenceVarsPerCollection: 1,
                creatorIndex: 0,
                userIdGenerator: null,
                delayRegistration: true
            );

            await testEnv.Start();

            SyncTestUserEnvironment creatorEnv = testEnv.GetCreator();

            IUserPresence nonCreatorPresence = testEnv.GetRandomNonCreatorPresence();
            SyncTestUserEnvironment nonCreatorEnv = testEnv.GetUserEnv(nonCreatorPresence);

            creatorEnv.VarRegistry.Register(creatorEnv.GroupVars.GroupBool);
            nonCreatorEnv.VarRegistry.Register(nonCreatorEnv.GroupVars.GroupBool);

            creatorEnv.GroupVars.GroupBool.Self.SetValue(true);

            await Task.Delay(1000);

            string creatorId = creatorEnv.Self.UserId;
            string nonCreatorId = nonCreatorEnv.Self.UserId;

            var nonCreatorPresenceBools = nonCreatorEnv.GroupVars.GroupBool.Others;

            Assert.True(nonCreatorPresenceBools.Any());
            Assert.False(nonCreatorEnv.GroupVars.GroupBool.Self.GetValue());

            var creatorPresenceVarInGuest = nonCreatorPresenceBools.First(var => {
                return var.Presence.UserId == creatorId;
            });

            Assert.True(creatorPresenceVarInGuest.GetValue());
            Assert.False(creatorEnv.GroupVars.GroupBool.Others.Any(var => var.GetValue()));
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncAnonymousData()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            await testEnv.Start();

            var allEnvs = testEnv.GetAllEnvs();

            var dict = new Dictionary<object, object>();
            dict["keyInt"] = 5;
            dict["keyString"] = "hello world";
            dict["keyBool"] = true;

            allEnvs[0].SharedVars.SharedAnonymousDict.SetValue(dict);

            await Task.Delay(1000);

            Assert.Equal(5, allEnvs[1].SharedVars.SharedAnonymousDict.GetValue()["keyInt"]);
            Assert.Equal("hello world", allEnvs[1].SharedVars.SharedAnonymousDict.GetValue()["keyString"]);
            Assert.Equal(true, allEnvs[1].SharedVars.SharedAnonymousDict.GetValue()["keyBool"]);

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void SharedVarShouldEmitDelta()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            await testEnv.Start();
            SyncTestSharedVars creatorEnv = testEnv.GetCreator().SharedVars;
            bool oldValue = true;
            bool newValue = false;

            creatorEnv.SharedBool.OnValueChanged += evt => {
                oldValue = evt.ValueChange.OldValue;
                newValue = evt.ValueChange.NewValue;
            };

            creatorEnv.SharedBool.SetValue(true);
            Assert.False(oldValue);
            Assert.True(newValue);
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void SharedVarShouldEmitDictDelta()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            await testEnv.Start();
            SyncTestSharedVars creatorEnv = testEnv.GetCreator().SharedVars;
            bool oldValueHasKey = true;
            bool newValueHasKey = false;


            creatorEnv.SharedDict.OnValueChanged += evt => {
                oldValueHasKey = evt.ValueChange.OldValue != null && evt.ValueChange.OldValue.ContainsKey("hello");
                newValueHasKey = evt.ValueChange.NewValue.ContainsKey("hello");
            };

            creatorEnv.SharedDict.SetValue(new Dictionary<string, string>{{"hello", "world"}});

            Assert.False(oldValueHasKey);
            Assert.True(newValueHasKey);

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void GroupVarShouldEmitDictDelta()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                numPresenceVarCollections: 1,
                numPresenceVarsPerCollection: 1,
                creatorIndex: 0);

            await testEnv.Start();
            SyncTestGroupVars creatorEnv = testEnv.GetCreator().GroupVars;

            IUserPresence otherPresence = testEnv.GetRandomNonCreatorPresence();
            SyncTestGroupVars nonCreatorEnv = testEnv.GetUserEnv(otherPresence).GroupVars;

            bool eventDispatched = false;
            bool oldValueHasKey = true;
            bool newValueHasKey = false;

            var nonCreatorVar = nonCreatorEnv.GroupDict.GetVar(otherPresence);

            nonCreatorVar.OnValueChanged += evt =>
            {
                eventDispatched = true;
                oldValueHasKey = evt.ValueChange.OldValue != null && evt.ValueChange.OldValue.ContainsKey("hello");
                newValueHasKey = evt.ValueChange.NewValue.ContainsKey("hello");
            };

            creatorEnv.GroupDict.Self.SetValue(new Dictionary<string, string>{{"hello", "world"}});

            await Task.Delay(1000);

            Assert.True(eventDispatched);
            Assert.False(oldValueHasKey);
            Assert.True(newValueHasKey);

            testEnv.Dispose();
        }
    }
}
