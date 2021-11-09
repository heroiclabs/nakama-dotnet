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
using Nakama;

namespace NakamaSync
{
    internal class HandshakeResponseHandler<T>
    {
        public ILogger Logger { get; set; }

        private readonly TaskCompletionSource<object> _handshakeTcs = new TaskCompletionSource<object>();
        private VarIngress<T> _varIngress;

        public HandshakeResponseHandler(VarIngress<T> incoingVarIngress)
        {
            _varIngress = incoingVarIngress;
        }

        public void Subscribe(HandshakeRequester<T> handshakeRequester, SyncSocket<T> syncSocket, HostTracker hostTracker)
        {
            handshakeRequester.OnHandshakeSuccess += () =>
            {
                _varIngress.Subscribe(syncSocket);
                _handshakeTcs.SetResult(null);
            };

            handshakeRequester.OnHandshakeFailure += (source) =>
            {
                _handshakeTcs.SetException(new HandshakeFailedException("Handshake requester received handshake failure", source));
            };
        }

        public Task GetHandshakeTask()
        {
            return _handshakeTcs.Task;
        }
    }
}
