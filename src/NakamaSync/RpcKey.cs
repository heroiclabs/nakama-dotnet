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
using System.Runtime.Serialization;

namespace NakamaSync
{
    [Serializable]
    internal struct RpcKey
    {
        [DataMember(Name="method_name"), Preserve]
        public string MethodName { get; set; }

        [DataMember(Name="target_id"), Preserve]
        public string TargetId { get; set; }

        public RpcKey(string rpcId, string targetId)
        {
            if (string.IsNullOrEmpty(rpcId))
            {
                throw new ArgumentException("Null or empty rpc id passed to rpc key.");
            }

            // target id can be null (static rpc) but not empty
            if (targetId == string.Empty)
            {
                throw new ArgumentException("Empty target id passed to rpc key.");
            }

            MethodName = rpcId;
            TargetId = targetId;
        }
    }
}