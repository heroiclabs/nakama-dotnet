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
using System.Linq;
using System.Threading.Tasks;
using NakamaSync;
using Xunit;

namespace Nakama.Tests.Socket
{
    public class SyncTest
    {
        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private void SharedVarShouldRetainData()
        {
            var testEnv = new SyncTestEnvironment(
                new SyncOpcodes(handshakeRequestOpcode: 0, handshakeResponseOpcode: 1, dataOpcode: 2),
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            testEnv.StartViaMatchmaker();
            SyncTestSharedVars creatorEnv = testEnv.GetCreator().SharedVars;
            creatorEnv.SharedBools[0].SetValue(true);
            Assert.True(creatorEnv.SharedBools[0].GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private void PresenceVarShouldRetainData()
        {
            var testEnv = new SyncTestEnvironment(
                new SyncOpcodes(handshakeRequestOpcode: 0, handshakeResponseOpcode: 1, dataOpcode: 2),
                numClients: 2,
                numPresenceVarCollections: 1,
                numPresenceVarsPerCollection: 1,
                creatorIndex: 0);

            testEnv.StartViaMatchmaker();
            SyncTestUserEnvironment creatorEnv = testEnv.GetCreator();
            creatorEnv.PresenceVars.BoolSelfVars["presenceBools_0"].SetValue(true);
            Assert.True(creatorEnv.PresenceVars.BoolSelfVars["presenceBools_0"].GetValue());
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
                new SyncOpcodes(handshakeRequestOpcode: 0, handshakeResponseOpcode: 1, dataOpcode: 2),
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0,
                null,
                idGenerator);


            mismatchedEnv.StartViaMatchmaker(errorHandler);

            await Task.Delay(2000);
            Assert.True(threwError);
            mismatchedEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private async Task SharedVarShouldSyncData()
        {
            var testEnv = new SyncTestEnvironment(
                new SyncOpcodes(handshakeRequestOpcode: 0, handshakeResponseOpcode: 1, dataOpcode: 2),
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            testEnv.StartViaMatchmaker();

            SyncTestSharedVars creatorEnv = testEnv.GetCreator().SharedVars;
            creatorEnv.SharedBools[0].SetValue(true);

            await Task.Delay(3000);

            SyncTestSharedVars guestEnv = testEnv.GetTestEnvironment(testEnv.GetRandomNonCreatorPresence()).SharedVars;
            Assert.True(guestEnv.SharedBools[0].GetValue());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private async Task PresenceVarShouldSyncData()
        {
            var testEnv = new SyncTestEnvironment(
                new SyncOpcodes(handshakeRequestOpcode: 0, handshakeResponseOpcode: 1, dataOpcode: 2),
                numClients: 2,
                numPresenceVarCollections: 1,
                numPresenceVarsPerCollection: 1,
                creatorIndex: 0);

            testEnv.StartViaMatchmaker();

            SyncTestUserEnvironment creatorEnv = testEnv.GetCreator();
            creatorEnv.PresenceVars.BoolSelfVars["presenceBools_0"].SetValue(true);

            await Task.Delay(2500);

            IUserPresence nonCreatorPresence = testEnv.GetRandomNonCreatorPresence();

            var nonCreatorEnv = testEnv.GetUserEnv(nonCreatorPresence);

            string creatorId = creatorEnv.Self.UserId;
            string nonCreatorId = nonCreatorEnv.Self.UserId;
            System.Console.WriteLine($"Creator id {creatorId}");
            System.Console.WriteLine($"Non creator id {nonCreatorId}");

            var nonCreatorPresenceBools = nonCreatorEnv.PresenceVars.BoolPresenceVars["presenceBools_0"];

            var creatorPresenceVarInGuest = nonCreatorPresenceBools.First(var => {
                System.Console.WriteLine($"Var presence id: {var.Presence.UserId}");

                return var.Presence.UserId == creatorId;
            });

            Assert.True(nonCreatorPresenceBools[0].GetValue());
            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.MATCHMAKER_TIMEOUT_MILLISECONDS)]
        private void HostShouldBeChosen()
        {
            var testEnv = new SyncTestEnvironment(
                new SyncOpcodes(handshakeRequestOpcode: 0, handshakeResponseOpcode: 1, dataOpcode: 2),
                numClients: 2,
                numSharedVars: 1,
                creatorIndex: 0);

            testEnv.StartViaMatchmaker();
            SyncTestSharedVars creatorEnv = testEnv.GetCreator().SharedVars;
            creatorEnv.SharedBools[0].SetValue(true);
            Assert.True(creatorEnv.SharedBools[0].GetValue());
            testEnv.Dispose();
        }

        private SyncErrorHandler CreateDefaultErrorHandler()
        {
            return (e) => new StdoutLogger().ErrorFormat(e.Message);
        }
    }
}
