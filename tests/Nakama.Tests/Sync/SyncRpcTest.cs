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
    public class SyncRpcTest
    {
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task TestLocalRpcNoImplicit()
        {
            var testEnv = new SyncTestEnvironment(numClients: 2, creatorIndex: 0);
            await testEnv.StartViaName("testName");

            var allEnvs = testEnv.GetAllUserEnvs();
            allEnvs[0].Rpcs.Invoke
            (
                new IUserPresence[]{allEnvs[0].Self},"TestRpcDelegateNoImplicit",
                new object[]{"param1", 1, true, new SyncTestRpcObjectNoImplicit{TestMember = "paramMember"}},
                new object[]{}
            );

            await Task.Delay(1000);

            Assert.Equal("param1", allEnvs[0].Rpcs.Param1Result);
            Assert.Equal(1, allEnvs[0].Rpcs.Param2Result);
            Assert.Equal(true, allEnvs[0].Rpcs.Param3Result);
            Assert.Equal("paramMember", allEnvs[0].Rpcs.Param4Result.TestMember);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task TestRpcNoImplicit()
        {
            var testEnv = new SyncTestEnvironment(numClients: 2, creatorIndex: 0);

            await testEnv.StartViaName("testName");
            var allEnvs = testEnv.GetAllUserEnvs();

            allEnvs[0].Rpcs.Invoke
            (
                new IUserPresence[]{allEnvs[1].Self},
                "TestRpcDelegateNoImplicit",
                new object[]{"param1", 1, true, new SyncTestRpcObjectNoImplicit{TestMember = "paramMember"}},
                new object[]{}
            );

            await Task.Delay(1000);

            Assert.Equal("param1", allEnvs[1].Rpcs.Param1Result);
            Assert.Equal(1, allEnvs[1].Rpcs.Param2Result);
            Assert.Equal(true, allEnvs[1].Rpcs.Param3Result);
            Assert.Equal("paramMember", allEnvs[1].Rpcs.Param4Result.TestMember);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task TestLocalRpcImplicitOperator()
        {
            var testEnv = new SyncTestEnvironment(numClients: 2, creatorIndex: 0);
            await testEnv.StartViaName("testName");

            var allEnvs = testEnv.GetAllUserEnvs();
            allEnvs[0].Rpcs.Invoke
            (
                new IUserPresence[]{allEnvs[0].Self}, "TestRpcDelegateImplicit",
                new object[]{"param1", 1, true, new SyncTestRpcObjectImplicit{TestMember = "paramMember"}},
                new object[]{}
            );

            await Task.Delay(1000);

            Assert.Equal("param1", allEnvs[0].Rpcs.Param1Result);
            Assert.Equal(1, allEnvs[0].Rpcs.Param2Result);
            Assert.Equal(true, allEnvs[0].Rpcs.Param3Result);
            Assert.Equal("paramMember", allEnvs[0].Rpcs.Param4Result.TestMember);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task TestRpcImplicitOperator()
        {
            var testEnv = new SyncTestEnvironment(numClients: 2, creatorIndex: 0);
            await testEnv.StartViaName("testName");
            var allEnvs = testEnv.GetAllUserEnvs();
            allEnvs[0].Rpcs.Invoke
            (
                null,
                "TestRpcDelegateImplicit",
                new object[]{"param1", 1, true, new SyncTestRpcObjectImplicit{TestMember = "paramMember"}},
                new object[]{}
            );

            await Task.Delay(1000);
            Assert.Equal("param1", allEnvs[1].Rpcs.Param1Result);
            Assert.Equal(1, allEnvs[1].Rpcs.Param2Result);
            Assert.Equal(true, allEnvs[1].Rpcs.Param3Result);
            Assert.Equal("paramMember", allEnvs[1].Rpcs.Param4Result.TestMember);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task TestRpcOptionalParamsOmittedRemote()
        {
            var testEnv = new SyncTestEnvironment(numClients: 2, creatorIndex: 0);
            await testEnv.StartViaName("testName");
            var allEnvs = testEnv.GetAllUserEnvs();
            allEnvs[0].Rpcs.Invoke
            (
                new IUserPresence[]{allEnvs[1].Self},
                "TestRpcOptionalParamsOmittedRemote",
                new object[]{"param1", 1, true},
                new object[]{}
            );

            await Task.Delay(1000);

            Assert.Equal("param1", allEnvs[1].Rpcs.Param1Result);
            Assert.Equal(1, allEnvs[1].Rpcs.Param2Result);
            Assert.Equal(true, allEnvs[1].Rpcs.Param3Result);
            Assert.Equal(null, allEnvs[1].Rpcs.Param4Result);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        private async Task TestRpcOptionalParamsOmittedLocal()
        {
            var testEnv = new SyncTestEnvironment(numClients: 2, creatorIndex: 0);
            await testEnv.StartViaName("testName");
            var allEnvs = testEnv.GetAllUserEnvs();
            allEnvs[0].Rpcs.Invoke
            (
                new IUserPresence[]{allEnvs[1].Self},
                "TestRpcOptionalParamsOmittedLocal",
                new object[]{"param1", 1, true, new SyncTestRpcObjectImplicit()},
                new object[]{}
            );

            await Task.Delay(1000);
            Assert.Equal("param1", allEnvs[1].Rpcs.Param1Result);
            Assert.Equal(1, allEnvs[1].Rpcs.Param2Result);
            Assert.Equal(true, allEnvs[1].Rpcs.Param3Result);
            Assert.Equal(null, allEnvs[1].Rpcs.Param4Result);
        }
    }
}
