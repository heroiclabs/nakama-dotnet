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
using System.Runtime.Serialization;
using NakamaSync;

namespace Nakama.Tests.Sync
{
    public class SyncTestRpcs
    {
        public string Param1Result { get; private set; }
        public int Param2Result { get; private set; }
        public bool Param3Result { get; private set; }
        public SyncTestRpcObjectImplicit Param4Result { get; private set; }

        private ISyncMatch _syncMatch;

        private const string ObjectId = "SyncTestRpcs";

        public SyncTestRpcs(RpcRegistry registry)
        {
            registry.AddTarget(ObjectId, this);
        }

        public void ReceiveMatch(ISyncMatch syncMatch)
        {
            _syncMatch = syncMatch;
        }

        public void Invoke(IEnumerable<IUserPresence> presences, string testDel, object[] requiredParameters, object[] optionalParameters)
        {
            _syncMatch.SendRpc(testDel, ObjectId, presences, requiredParameters, optionalParameters);
        }

        private void TestRpcDelegateImplicit(string param1, int param2, bool param3, SyncTestRpcObjectImplicit param4)
        {
            Param1Result = param1;
            Param2Result = param2;
            Param3Result = param3;
            Param4Result = param4;
        }

        private void TestRpcDelegateNoImplicit(string param1, int param2, bool param3, SyncTestRpcObjectNoImplicit param4)
        {
            Param1Result = param1;
            Param2Result = param2;
            Param3Result = param3;
            Param4Result = param4;
        }

        private void TestRpcOptionalParamsOmittedLocal(string param1, int param2, bool param3)
        {
            Param1Result = param1;
            Param2Result = param2;
            Param3Result = param3;
        }

        private void TestRpcOptionalParamsOmittedRemote(string param1, int param2, bool param3, SyncTestRpcObjectNoImplicit optionalParam4)
        {
            Param1Result = param1;
            Param2Result = param2;
            Param3Result = param3;
            Param4Result = optionalParam4;
        }
    }

    [Serializable]
    public class SyncTestRpcObjectImplicit
    {
        [DataMember(Name="TestMember")]
        public string TestMember { get; set; }

        public static implicit operator SyncTestRpcObjectNoImplicit(SyncTestRpcObjectImplicit v) => new SyncTestRpcObjectNoImplicit{TestMember = v.TestMember};
        public static implicit operator SyncTestRpcObjectImplicit(SyncTestRpcObjectNoImplicit v) => new SyncTestRpcObjectImplicit{TestMember = v.TestMember};
    }

    [Serializable]
    public class SyncTestRpcObjectNoImplicit
    {
        [DataMember(Name = "TestMember")]
        public string TestMember { get; set; }
    }
}
