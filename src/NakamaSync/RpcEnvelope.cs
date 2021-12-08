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
    internal class RpcEnvelope
    {
        [DataMember(Name="rpc_key"), Preserve]
        public RpcKey RpcKey { get; set; }

        [DataMember(Name="required_parameters"), Preserve]
        public object[] RequiredParameters { get; set; }

        [DataMember(Name="optional_parameters"), Preserve]
        public object[] OptionalParameters { get; set; }
    }
}
