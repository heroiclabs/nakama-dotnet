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
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void PresenceEventBeforeSharedHandshakeData()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            testEnv.GetCreatorEnv().SharedVars.SharedBool.SetValue(true);

            bool receivedValueChanged = false;
            bool receivedPresence = false;
            bool receivedPresenceBeforeValue = false;

            testEnv.GetNonCreatorEnv().SharedVars.SharedBool.OnValueChanged += evt =>
            {
                receivedValueChanged = true;
            };

            testEnv.GetNonCreatorEnv().Socket.ReceivedMatchPresence += evt =>
            {
                receivedPresence = true;
                receivedPresenceBeforeValue = !receivedValueChanged;
            };

            await testEnv.StartAll();

            Assert.True(receivedValueChanged);
            Assert.True(receivedPresence);
            Assert.True(receivedPresenceBeforeValue);

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void PresenceEventBeforeSharedHandshakeDataDeferred()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0,
                null,
                delayRegistration: true);


            var createdBool = testEnv.GetCreatorEnv().SharedVars.SharedBool;
            createdBool.SetValue(true);

            var nonCreatorBool = testEnv.GetNonCreatorEnv().SharedVars.SharedBool;

            await testEnv.StartAll();

            testEnv.GetCreatorEnv().VarRegistry.Register(createdBool);
            testEnv.GetNonCreatorEnv().VarRegistry.Register(nonCreatorBool);

            bool receivedValueChanged = false;
            bool receivedPresenceBeforeValue = false;

            testEnv.GetNonCreatorEnv().SharedVars.SharedBool.OnValueChanged += evt =>
            {
                receivedPresenceBeforeValue = testEnv.GetNonCreatorEnv().Match.GetAllPresences().Count() == 2;
                receivedValueChanged = true;
            };

            await Task.Delay(1000);

            Assert.True(receivedValueChanged);
            Assert.True(receivedPresenceBeforeValue);

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void PresenceEventBeforeGroupHandshakeDataDeferred()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0,
                null,
                delayRegistration: true);

            var createdBool = testEnv.GetCreatorEnv().GroupVars.GroupBool;
            createdBool.Self.SetValue(true);

            var nonCreatorBool = testEnv.GetNonCreatorEnv().GroupVars.GroupBool;

            await testEnv.StartAll();

            testEnv.GetCreatorEnv().VarRegistry.Register(createdBool);
            testEnv.GetNonCreatorEnv().VarRegistry.Register(nonCreatorBool);

            bool receivedValueChanged = false;
            bool receivedPresenceBeforeValue = false;

            var creatorPresence = testEnv.GetCreatorPresence();
            var nonCreatorVar = testEnv.GetNonCreatorEnv().GroupVars.GroupBool.GetVar(creatorPresence);

            nonCreatorVar.OnValueChanged += evt =>
            {
                receivedPresenceBeforeValue = testEnv.GetNonCreatorEnv().Match.GetAllPresences().Count() == 2;
                receivedValueChanged = true;
            };

            await Task.Delay(1000);

            Assert.True(receivedValueChanged);
            Assert.True(receivedPresenceBeforeValue);

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void PresenceEventBeforeSharedHandshakeMatchState()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            testEnv.GetCreatorEnv().SharedVars.SharedBool.SetValue(true);

            bool receivedMatchState = false;
            bool receivedPresence = false;
            bool receivedPresenceBeforeValue = false;

            testEnv.GetNonCreatorEnv().Socket.ReceivedMatchState += evt =>
            {
                receivedMatchState = true;
            };

            testEnv.GetNonCreatorEnv().Socket.ReceivedMatchPresence += evt =>
            {
                receivedPresence = true;
                receivedPresenceBeforeValue = !receivedMatchState;
            };

            await testEnv.StartAll();

            Assert.True(receivedMatchState);
            Assert.True(receivedPresence);
            Assert.True(receivedPresenceBeforeValue);

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void SingleClientShouldHandshake()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 1,
                creatorIndex: 0);

            await testEnv.StartAll();
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void TwoClientsShouldHandshake()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            await testEnv.StartAll();
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void SharedVarShouldRetainData()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            await testEnv.StartAll();
            SyncTestSharedVars creatorEnv = testEnv.GetCreatorEnv().SharedVars;
            creatorEnv.SharedBool.SetValue(true);
            Assert.True(creatorEnv.SharedBool.GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void PresenceVarShouldRetainData()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            await testEnv.StartAll();
            SyncTestUserEnvironment creatorEnv = testEnv.GetCreatorEnv();
            SelfVar<bool> self = creatorEnv.GroupVars.GroupBool.Self;
            self.SetValue(true);
            Assert.True(creatorEnv.GroupVars.GroupBool.Self.GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncData()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            await testEnv.StartAll();

            var allEnvs = testEnv.GetAllUserEnvs();
            allEnvs[0].SharedVars.SharedInt.SetValue(5);

            await Task.Delay(1000);

            Assert.Equal(5, allEnvs[1].SharedVars.SharedInt.GetValue());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncDictPreregistered()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            await testEnv.StartAll();

            var allEnvs = testEnv.GetAllUserEnvs();

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
        private async Task SharedVarShouldSyncDictForClientJoiningLate()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            var match = await testEnv.StartCreate();
            var creatorEnv = testEnv.GetCreatorEnv();

            var dict = new Dictionary<string, string>
            {
                ["hello"] = "world"
            };

            creatorEnv.SharedVars.SharedDict.SetValue(dict);

            await Task.Delay(1000);

            await testEnv.StartJoin(1, match);
            var env1Dict = testEnv.GetNonCreatorEnv().SharedVars.SharedDict;

            Assert.NotNull(env1Dict);
            Assert.True(env1Dict.GetValue().ContainsKey("hello"));
            Assert.Equal("world", env1Dict.GetValue()["hello"]);

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task GroupVarShouldSyncData()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            await testEnv.StartAll();

            SyncTestUserEnvironment creatorEnv = testEnv.GetCreatorEnv();
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
                creatorIndex: 0);

            await testEnv.StartAll();

            List<SyncTestUserEnvironment> allEnvs = testEnv.GetAllUserEnvs();
            var env1 = allEnvs[0];
            var env2 = allEnvs[1];
            Assert.NotEqual(env1.Self.UserId, env2.Self.UserId);
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncDataDeferred1()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0,
                delayRegistration: true
            );

            await testEnv.StartAll();

            var allEnvs = testEnv.GetAllUserEnvs();

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
                creatorIndex: 0,
                delayRegistration: true
            );

            await testEnv.StartAll();

            var allEnvs = testEnv.GetAllUserEnvs();

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
        private async Task SharedVarNonCreatorSharesInitialState()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0,
                delayRegistration: true
            );

            await testEnv.StartAll();

            var allEnvs = testEnv.GetAllUserEnvs();

            bool valueChanged = false;
            allEnvs[0].SharedVars.SharedInt.OnValueChanged += delegate { valueChanged = true; };

            allEnvs[0].VarRegistry.Register(allEnvs[0].SharedVars.SharedInt);
            allEnvs[1].VarRegistry.Register(allEnvs[1].SharedVars.SharedInt);
            allEnvs[1].SharedVars.SharedInt.SetValue(5);

            await Task.Delay(1000);

            Assert.True(valueChanged);
            Assert.Equal(5, allEnvs[0].SharedVars.SharedInt.GetValue());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarNonCreatorSharesInitialStateStaggered1()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0,
                delayRegistration: true
            );

            var allUserEnvs = testEnv.GetAllUserEnvs();

            allUserEnvs[0].VarRegistry.Register(allUserEnvs[0].SharedVars.SharedInt);
            allUserEnvs[0].SharedVars.SharedInt.SetValue(5);

            var match = await testEnv.StartCreate();

            bool valueChanged = false;
            allUserEnvs[1].SharedVars.SharedInt.OnValueChanged += delegate { valueChanged = true; };
            allUserEnvs[1].VarRegistry.Register(allUserEnvs[1].SharedVars.SharedInt);

            await testEnv.StartJoin(envIndex: 1, match);

            await Task.Delay(1000);

            Assert.True(valueChanged);
            Assert.Equal(5, allUserEnvs[1].SharedVars.SharedInt.GetValue());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarNonCreatorSharesInitialStateStaggered2()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0,
                delayRegistration: true
            );

            var allUserEnvs = testEnv.GetAllUserEnvs();

            allUserEnvs[0].VarRegistry.Register(allUserEnvs[0].SharedVars.SharedInt);
            allUserEnvs[0].SharedVars.SharedInt.SetValue(5);

            var match = await testEnv.StartCreate();

            match = await testEnv.StartJoin(envIndex: 1, match);

            await Task.Delay(1000);

            allUserEnvs[1].VarRegistry.Register(allUserEnvs[1].SharedVars.SharedInt);

            await Task.Delay(1000);

            Assert.Equal(5, allUserEnvs[1].SharedVars.SharedInt.GetValue());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarDictionaryShouldMergeValues()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            await testEnv.StartAll();
            var allUserEnvs = testEnv.GetAllUserEnvs();

            var dict1 = new Dictionary<string, string>
            {
                { "Env1", "Foo" }
            };

            var dict2 = new Dictionary<string, string>
            {
                { "Env2", "Bar" }
            };

            allUserEnvs[0].SharedVars.SharedDict.SetValue(dict1);
            allUserEnvs[1].SharedVars.SharedDict.SetValue(dict2);

            await Task.Delay(1000);

            Assert.Equal("Foo", allUserEnvs[0].SharedVars.SharedDict.GetValue()["Env1"]);
            Assert.Equal("Foo", allUserEnvs[1].SharedVars.SharedDict.GetValue()["Env1"]);
            Assert.Equal("Bar", allUserEnvs[0].SharedVars.SharedDict.GetValue()["Env2"]);
            Assert.Equal("Bar", allUserEnvs[1].SharedVars.SharedDict.GetValue()["Env2"]);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task SharedVarDictionaryShouldOverwriteValues()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            await testEnv.StartAll();
            var allUserEnvs = testEnv.GetAllUserEnvs();

            var dict1 = new Dictionary<string, string>
            {
                { "OverwriteMe", "1" }
            };

            var dict2 = new Dictionary<string, string>
            {
                { "OverwriteMe", "2" }
            };

            allUserEnvs[0].SharedVars.SharedDict.SetValue(dict1);

            await Task.Delay(1000);

            allUserEnvs[1].SharedVars.SharedDict.SetValue(dict2);

            await Task.Delay(1000);

            Assert.Equal("2", allUserEnvs[0].SharedVars.SharedDict.GetValue()["OverwriteMe"]);
            Assert.Equal("2", allUserEnvs[1].SharedVars.SharedDict.GetValue()["OverwriteMe"]);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task PresenceVarFactoryUniqueAssignmentsNotDeferred()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0,
                userIdGenerator: null,
                delayRegistration: false
            );

            await testEnv.StartAll();
            IUserPresence nonCreatorPresence = testEnv.GetRandomNonCreatorPresence();

            SyncTestUserEnvironment creatorEnv = testEnv.GetCreatorEnv();
            SyncTestUserEnvironment nonCreatorEnv = testEnv.GetUserEnv(nonCreatorPresence);

            await Task.Delay(1000);
            Assert.True(creatorEnv.GroupVars.GroupBool.Self.Presence.UserId == creatorEnv.Self.UserId);
            Assert.True(creatorEnv.GroupVars.GroupBool.Others.Any(p => p.Presence.UserId == nonCreatorEnv.Self.UserId));
            Assert.False(creatorEnv.GroupVars.GroupBool.Others.Any(var => var.Presence.UserId == creatorEnv.GroupVars.GroupBool.Self.Presence.UserId));

            Assert.True(nonCreatorEnv.GroupVars.GroupBool.Self.Presence.UserId == nonCreatorEnv.Self.UserId);
            Assert.True(nonCreatorEnv.GroupVars.GroupBool.Others.Any(p => p.Presence.UserId == creatorEnv.Self.UserId));
            Assert.False(nonCreatorEnv.GroupVars.GroupBool.Others.Any(var => var.Presence.UserId == nonCreatorEnv.GroupVars.GroupBool.Self.Presence.UserId));

        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task PresenceVarFactoryUniqueAssignmentsDeferred()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0,
                userIdGenerator: null,
                delayRegistration: true
            );

            await testEnv.StartAll();
            IUserPresence nonCreatorPresence = testEnv.GetRandomNonCreatorPresence();

            SyncTestUserEnvironment creatorEnv = testEnv.GetCreatorEnv();
            SyncTestUserEnvironment nonCreatorEnv = testEnv.GetUserEnv(nonCreatorPresence);

            await Task.Delay(1000);

            creatorEnv.VarRegistry.Register(creatorEnv.GroupVars.GroupBool);
            nonCreatorEnv.VarRegistry.Register(nonCreatorEnv.GroupVars.GroupBool);

            IUserPresence creatorBoolSelf = creatorEnv.GroupVars.GroupBool.Self.Presence;

            Assert.True(creatorBoolSelf.UserId == creatorEnv.Self.UserId);
            Assert.Equal(1, creatorEnv.GroupVars.GroupBool.Others.Count());
            Assert.True(creatorEnv.GroupVars.GroupBool.Others.Any(p => p.Presence.UserId == nonCreatorEnv.Self.UserId));
            Assert.True(nonCreatorEnv.GroupVars.GroupBool.Self.Presence.UserId == nonCreatorEnv.Self.UserId);
            Assert.True(nonCreatorEnv.GroupVars.GroupBool.Others.Any(p => p.Presence.UserId == creatorEnv.Self.UserId));
            Assert.False(creatorEnv.GroupVars.GroupBool.Others.Any(var => var.Presence.UserId == creatorEnv.GroupVars.GroupBool.Self.Presence.UserId));
            Assert.False(nonCreatorEnv.GroupVars.GroupBool.Others.Any(var => var.Presence.UserId == nonCreatorEnv.GroupVars.GroupBool.Self.Presence.UserId));
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task PresenceVarShouldSyncDataDeferred()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0,
                userIdGenerator: null,
                delayRegistration: true
            );

            await testEnv.StartAll();

            SyncTestUserEnvironment creatorEnv = testEnv.GetCreatorEnv();

            IUserPresence nonCreatorPresence = testEnv.GetRandomNonCreatorPresence();
            SyncTestUserEnvironment nonCreatorEnv = testEnv.GetUserEnv(nonCreatorPresence);

            creatorEnv.VarRegistry.Register(creatorEnv.GroupVars.GroupBool);
            nonCreatorEnv.VarRegistry.Register(nonCreatorEnv.GroupVars.GroupBool);

            creatorEnv.GroupVars.GroupBool.Self.SetValue(true);

            await Task.Delay(1000);

            string creatorId = creatorEnv.Self.UserId;
            string nonCreatorId = nonCreatorEnv.Self.UserId;

            var nonCreatorSelfBool = nonCreatorEnv.GroupVars.GroupBool.Self;
            var nonCreatorPresenceBools = nonCreatorEnv.GroupVars.GroupBool.Others;

            Assert.False(nonCreatorSelfBool.GetValue());
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
                creatorIndex: 0);

            await testEnv.StartAll();

            var allEnvs = testEnv.GetAllUserEnvs();

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
                creatorIndex: 0);

            await testEnv.StartAll();
            SyncTestSharedVars creatorEnv = testEnv.GetCreatorEnv().SharedVars;
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
                creatorIndex: 0);

            await testEnv.StartAll();
            SyncTestSharedVars creatorEnv = testEnv.GetCreatorEnv().SharedVars;
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
                creatorIndex: 0);

            await testEnv.StartAll();

            IUserPresence nonCreatorPresence = testEnv.GetRandomNonCreatorPresence();

            SyncTestGroupVars creatorEnv = testEnv.GetCreatorEnv().GroupVars;
            SyncTestGroupVars nonCreatorEnv = testEnv.GetUserEnv(nonCreatorPresence).GroupVars;

            bool eventDispatched = false;
            bool oldValueHasKey = true;
            bool newValueHasKey = false;

            var nonCreatorOtherVar = nonCreatorEnv.GroupDict.GetVar(testEnv.GetCreatorPresence());

            nonCreatorOtherVar.OnValueChanged += evt =>
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

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void GroupVarEmitsPresence()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            bool varAdded = false;

            testEnv.GetCreatorEnv().GroupVars.GroupBool.OnPresenceAdded += var =>
            {
                varAdded = true;
            };

            await testEnv.StartAll();
            Assert.True(varAdded);
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void SharedVarGivesInitialState()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            testEnv.GetCreatorEnv().SharedVars.SharedBool.SetValue(true);

            await testEnv.StartAll();

            IUserPresence nonCreator = testEnv.GetRandomNonCreatorPresence();
            var nonCreatorEnv = testEnv.GetTestEnvironment(nonCreator);
            Assert.True(nonCreatorEnv.SharedVars.SharedBool.GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void GroupVarGivesInitialState()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            testEnv.GetCreatorEnv().GroupVars.GroupBool.Self.SetValue(true);

            await testEnv.StartAll();

            IUserPresence nonCreator = testEnv.GetRandomNonCreatorPresence();
            var nonCreatorEnv = testEnv.GetTestEnvironment(nonCreator);

            Assert.True(nonCreatorEnv.GroupVars.GroupBool.GetVar(testEnv.GetCreatorPresence()).GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void SharedVarResets()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);


            await testEnv.StartAll();

            IUserPresence nonCreator = testEnv.GetRandomNonCreatorPresence();
            var creatorEnv = testEnv.GetCreatorEnv();
            var nonCreatorEnv = testEnv.GetTestEnvironment(nonCreator);

            testEnv.GetCreatorEnv().SharedVars.SharedBool.SetValue(true);

            await Task.Delay(1000);

            bool creatorBoolReset = false;
            bool creatorValChangedOnReset = false;
            bool nonCreatorBoolReset = false;
            bool nonCreatorValChangedOnReset = false;

            creatorEnv.SharedVars.SharedBool.OnValueChanged += (evt) =>
            {
                creatorValChangedOnReset = true;
            };

            creatorEnv.SharedVars.SharedBool.OnReset += () =>
            {
                creatorBoolReset = true;
            };

            nonCreatorEnv.SharedVars.SharedBool.OnReset += () =>
            {
                nonCreatorBoolReset = true;
            };

            nonCreatorEnv.SharedVars.SharedBool.OnValueChanged += (evt) =>
            {
                nonCreatorValChangedOnReset = true;
            };

            await testEnv.GetCreatorEnv().Socket.LeaveMatchAsync(creatorEnv.Match);
            await nonCreatorEnv.Socket.LeaveMatchAsync(nonCreatorEnv.Match);

            await Task.Delay(1000);

            Assert.True(creatorBoolReset);
            Assert.True(nonCreatorBoolReset);
            Assert.True(creatorValChangedOnReset);
            Assert.True(nonCreatorValChangedOnReset);
            Assert.False(creatorEnv.SharedVars.SharedBool.GetValue());
            Assert.False(nonCreatorEnv.SharedVars.SharedBool.GetValue());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void GroupVarResets()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            testEnv.GetCreatorEnv().GroupVars.GroupBool.Self.SetValue(true);

            await testEnv.StartAll();

            IUserPresence nonCreator = testEnv.GetRandomNonCreatorPresence();
            var nonCreatorEnv = testEnv.GetTestEnvironment(nonCreator);

            Assert.True(nonCreatorEnv.GroupVars.GroupBool.GetVar(testEnv.GetCreatorPresence()).GetValue());
            testEnv.Dispose();
        }
    }
}
