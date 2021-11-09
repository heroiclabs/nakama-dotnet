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
using Nakama;

namespace NakamaSync
{
    //public delegate void RpcEnvelopeHandler(IUserPresence source, RpcEnvelope response);

    internal interface IRpcSocket
    {
       // event RpcEnvelopeHandler OnRpcEnvelope;
    }

    internal class RpcIngress
    {
        private readonly RpcTargetRegistry _registry;

        public RpcIngress(RpcTargetRegistry registry)
        {
            _registry = registry;
        }

        public void Subscribe(IRpcSocket socket)
        {
            //socket.OnRpcEnvelope += HandleRpcEnvelope;
        }

        private void HandleRpcEnvelope(IUserPresence source, RpcEnvelope envelope)
        {
            if (!_registry.HasTarget(envelope.RpcKey.TargetId))
            {
                throw new InvalidOperationException("Received rpc for non-existent target: " + envelope.RpcKey.TargetId);
            }

            var rpc = RpcInvocation.Create(_registry.GetTarget(envelope.RpcKey.TargetId), envelope.RpcKey.MethodName, envelope.Parameters);
            rpc.Invoke();
        }
    }
}
