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
using System.Threading.Tasks;
using Nakama;

namespace NakamaSync
{
    internal class VarConnection<T>
    {
        public delegate void SyncEnvelopeHandler(IUserPresence source, ISerializableVar<T> envelope);
        public delegate void HandshakeRequestHandler(IUserPresence source, HandshakeRequest request);
        public delegate void HandshakeSuccessHandler(IUserPresence source, ISerializableVar<T> response);
        public delegate void HandshakeFailureHandler(IUserPresence source);

        public event SyncEnvelopeHandler OnSyncEnvelope;
        public event HandshakeRequestHandler OnHandshakeRequest;
        public event HandshakeSuccessHandler OnHandshakeSuccess;
        public event HandshakeFailureHandler OnHandshakeFailure;

        public ILogger Logger { get; set; }
        public IMatch Match => _match;
        public HostTracker HostTracker => _hostTracker;

        private readonly SyncEncoding _encoding = new SyncEncoding();
        private readonly ISocket _socket;
        private readonly SyncOpcodes _opcodes;
        private readonly PresenceTracker _presenceTracker;
        private readonly HostTracker _hostTracker;
        private IMatch _match;
        private readonly TaskCompletionSource<object> _handshakeTcs = new TaskCompletionSource<object>();

        public VarConnection(ISocket socket, SyncOpcodes opcodes, PresenceTracker presenceTracker, HostTracker hostTracker)
        {
            if (socket == null)
            {
                throw new ArgumentException("Null socket provided to sync socket.");
            }

            if (opcodes == null)
            {
                throw new ArgumentException("Null opcodes provided to sync socket.");
            }

            if (presenceTracker == null)
            {
                throw new ArgumentException("Null presence tracker provided to sync socket.");
            }

            if (hostTracker == null)
            {
                throw new ArgumentException("Null host tracker provided to sync socket.");
            }

            _socket = socket;
            _opcodes = opcodes;
            _presenceTracker = presenceTracker;
            _hostTracker = hostTracker;
            _socket.ReceivedMatchState += HandleReceivedMatchState;
        }

        public void ReceiveMatch(IMatch match)
        {
            _match = match;
        }

        public void SendHandshakeRequest(HandshakeRequest request, IUserPresence target)
        {
            Logger?.InfoFormat($"User id {_match.Self.UserId} sending handshake request.");
            _socket.SendMatchStateAsync(_match.Id, _opcodes.HandshakeRequest, _encoding.Encode(request), new IUserPresence[]{target});
        }

        public void SendHandshakeResponse(HandshakeResponse<T> response, IUserPresence target)
        {
            Logger?.InfoFormat($"User id {_match.Self.UserId} sending handshake response.");
            _socket.SendMatchStateAsync(_match.Id, _opcodes.HandshakeResponse, _encoding.Encode(response), new IUserPresence[]{target});
        }

        public void SendSyncDataToAll(ISerializableVar<T> serializable)
        {
            if (_match == null)
            {
                throw new NullReferenceException("Tried sending data before match was received");
            }

            Logger?.DebugFormat($"User id {_match.Self.UserId} sending data.");
            _socket.SendMatchStateAsync(_match.Id, _opcodes.Data, _encoding.Encode(serializable));
        }

        public void HandleReceivedMatchState(IMatchState state)
        {
            if (state.OpCode == _opcodes.Data)
            {
                Logger?.InfoFormat($"Socket for {_match.Self.UserId} received sync envelope.");

                ISerializableVar<T> serialized = _encoding.Decode<ISerializableVar<T>>(state.State);

                Logger?.DebugFormat($"Envelope decoded.");

                OnSyncEnvelope?.Invoke(state.UserPresence, serialized);
            }
            else if (state.OpCode == _opcodes.HandshakeRequest)
            {
                Logger?.InfoFormat($"Socket for {_match.Self.UserId} received handshake request.");

                HandshakeRequest request = null;

                request = _encoding.Decode<HandshakeRequest>(state.State);
                OnHandshakeRequest?.Invoke(state.UserPresence, request);
            }
            else if (state.OpCode == _opcodes.HandshakeResponse)
            {
                Logger?.InfoFormat($"Socket for {_match.Self.UserId} received handshake response.");

                HandshakeResponse<T> response = null;

                response = _encoding.Decode<HandshakeResponse<T>>(state.State);

                if (response.Success)
                {
                    _handshakeTcs.SetResult(true);
                    OnHandshakeSuccess?.Invoke(state.UserPresence, response.Serializable);
                }
                else
                {
                    _handshakeTcs.SetException(new HandshakeFailedException("Handshake requester received handshake failure", state.UserPresence));
                    OnHandshakeFailure?.Invoke(state.UserPresence);
                }
            }
        }

        public Task GetPendingHandshake()
        {
            return _handshakeTcs.Task;
        }
    }
}
