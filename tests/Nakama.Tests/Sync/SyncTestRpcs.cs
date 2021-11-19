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
using System.Runtime.Serialization;
using NakamaSync;

namespace Nakama.Tests.Sync
{
    public class SyncTestRpcs
    {
        public string Param1Result { get; private set; }
        public int Param2Result { get; private set; }
        public bool Param3Result { get; private set; }
        public SyncTestRpcObject Param4Result { get; private set; }

        private SyncMatch _syncMatch;

        private const string ObjectId = "SyncTestRpcs";

        public SyncTestRpcs(RpcRegistry registry)
        {
            registry.AddTarget(ObjectId, this);
        }

        public void ReceiveMatch(SyncMatch syncMatch)
        {
            _syncMatch = syncMatch;
        }

        public void Invoke(IEnumerable<IUserPresence> presences = null, string testDel = "TestRpcDelegate")
        {
            var testObj = new SyncTestRpcObject();
            testObj.TestMember = "testMember";
            _syncMatch.SendRpc(presences, testDel, ObjectId, new object[]{"param1", 1, true, testObj});
        }

        private void TestRpcDelegate(string param1, int param2, bool param3, SyncTestRpcObject param4)
        {
            Param1Result = param1;
            Param2Result = param2;
            Param3Result = param3;
            Param4Result = param4;
        }

        private void TestRpcDelegate2(string param1, int param2, bool param3, SyncTestRpcObject2 param4)
        {
            Param1Result = param1;
            Param2Result = param2;
            Param3Result = param3;
            Param4Result = param4;
        }
    }

    public class SyncTestRpcObject
    {
        [DataMember(Name="TestMember")]
        public string TestMember { get; set; }

        public static implicit operator SyncTestRpcObject2(SyncTestRpcObject v) => new SyncTestRpcObject2{OtherTestMember = v.TestMember };
        public static implicit operator SyncTestRpcObject(SyncTestRpcObject2 v) => new SyncTestRpcObject{TestMember = v.OtherTestMember };
    }

    public class SyncTestRpcObject2
    {
        [DataMember(Name = "test_value")]
        public string OtherTestMember { get; set; }
    }
}
