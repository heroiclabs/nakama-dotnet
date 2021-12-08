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

using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests.Sync
{
    public class SyncTestHost
    {
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void StickyHostShouldBeChosen()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            await testEnv.StartAll();
            var env1 = testEnv.GetUserEnv(testEnv.GetCreatorPresence());
            var env2 = testEnv.GetUserEnv(testEnv.GetRandomNonCreatorPresence());

            Assert.True(env1.Match.IsSelfHost());
            Assert.False(env2.Match.IsSelfHost());
            Assert.Equal(env1.Match.Self.UserId, env2.Match.GetHostPresence().UserId);

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void StickyHostShouldBeRemoved()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            await testEnv.StartAll();

            var env1 = testEnv.GetUserEnv(testEnv.GetCreatorPresence());
            var env2 = testEnv.GetUserEnv(testEnv.GetRandomNonCreatorPresence());

            await env1.Socket.LeaveMatchAsync(env1.Match);
            await Task.Delay(1000);

            Assert.True(env2.Match.IsSelfHost());

            testEnv.Dispose();
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async void StickyHostCanBeManuallySet()
        {
            var testEnv = new SyncTestEnvironment(
                numClients: 2,
                creatorIndex: 0);

            await testEnv.StartAll();
            var env1 = testEnv.GetUserEnv(testEnv.GetCreatorPresence());
            var env2 = testEnv.GetUserEnv(testEnv.GetRandomNonCreatorPresence());
            env1.Match.SetHost(env2.Self);

            await Task.Delay(1000);

            Assert.False(env1.Match.IsSelfHost());
            Assert.True(env2.Match.IsSelfHost());

            testEnv.Dispose();
        }
    }
}
