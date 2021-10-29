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
using System.Linq;
using Nakama;

namespace NakamaSync
{
    internal class SyncSocket : ISyncService
    {
        public delegate void SyncEnvelopeHandler(IUserPresence source, Envelope envelope);
        public delegate void HandshakeRequestHandler(IUserPresence source, HandshakeRequest request);
        public delegate void HandshakeResponseHandler(IUserPresence source, HandshakeResponse response);
        public delegate void RpcEnvelopeHandler(IUserPresence source, RpcEnvelope response);

        public event SyncEnvelopeHandler OnSyncEnvelope;
        public event HandshakeRequestHandler OnHandshakeRequest;
        public event HandshakeResponseHandler OnHandshakeResponse;
        public event RpcEnvelopeHandler OnRpcEnvelope;

        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly SyncEncoding _encoding = new SyncEncoding();
        private readonly ISocket _socket;
        private readonly SyncOpcodes _opcodes;
        private readonly PresenceTracker _presenceTracker;
        private IMatch _match;

        public SyncSocket(ISocket socket, SyncOpcodes opcodes, PresenceTracker presenceTracker)
        {
            _socket = socket;
            _opcodes = opcodes;
            _presenceTracker = presenceTracker;
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

        public void SendHandshakeResponse(IUserPresence target, HandshakeResponse response)
        {
            Logger?.InfoFormat($"User id {_match.Self.UserId} sending handshake response.");
            _socket.SendMatchStateAsync(_match.Id, _opcodes.HandshakeResponse, _encoding.Encode(response), new IUserPresence[]{target});
        }

        public void SendSyncDataToAll(Envelope envelope)
        {
            Logger?.DebugFormat($"User id {_match.Self.UserId} sending data to all");
            _socket.SendMatchStateAsync(_match.Id, _opcodes.Data, _encoding.Encode(envelope));
        }

        public void SendRpc(RpcEnvelope envelope, IEnumerable<IUserPresence> targets)
        {
            Logger?.DebugFormat($"User id {_match.Self.UserId} sending data to all");
            _socket.SendMatchStateAsync(_match.Id, _opcodes.Rpc, _encoding.Encode(envelope), targets);

            if (targets.Any(presence => presence.UserId == _match.Self.UserId))
            {
                this.OnRpcEnvelope(_match.Self, envelope);
            }
        }

        public void SendRpc(RpcEnvelope envelope)
        {
            Logger?.DebugFormat($"User id {_match.Self.UserId} sending data to all");
            _socket.SendMatchStateAsync(_match.Id, _opcodes.Rpc, _encoding.Encode(envelope));
        }

        private void HandleReceivedMatchState(IMatchState state)
        {
            if (state.OpCode == _opcodes.Data)
            {
                Logger?.InfoFormat($"Socket for {_match.Self.UserId} received sync envelope.");

                Envelope envelope = null;

                try
                {
                    envelope = _encoding.Decode<Envelope>(state.State);
                }
                catch (Exception e)
                {
                    ErrorHandler?.Invoke(e);
                }

                Logger?.DebugFormat($"Envelope decoded.");

                OnSyncEnvelope?.Invoke(state.UserPresence, envelope);
            }
            else if (state.OpCode == _opcodes.HandshakeRequest)
            {
                Logger?.InfoFormat($"Socket for {_match.Self.UserId} received handshake request.");

                HandshakeRequest request = null;

                try
                {
                    request = _encoding.Decode<HandshakeRequest>(state.State);
                    OnHandshakeRequest?.Invoke(state.UserPresence, request);
                }
                catch (Exception e)
                {
                    ErrorHandler?.Invoke(e);
                }
            }
            else if (state.OpCode == _opcodes.HandshakeResponse)
            {
                Logger?.InfoFormat($"Socket for {_match.Self.UserId} received handshake response.");

                HandshakeResponse response = null;

                try
                {
                    response = _encoding.Decode<HandshakeResponse>(state.State);
                    OnHandshakeResponse?.Invoke(state.UserPresence, response);
                }
                catch (Exception e)
                {
                    ErrorHandler?.Invoke(e);
                }
            }
            else if (state.OpCode == _opcodes.Rpc)
            {
                Logger?.InfoFormat($"Socket for {_match.Self.UserId} received rpc.");

                RpcEnvelope response = null;

                try
                {
                    response = _encoding.Decode<RpcEnvelope>(state.State);
                    OnRpcEnvelope?.Invoke(state.UserPresence, response);
                }
                catch (Exception e)
                {
                    ErrorHandler?.Invoke(e);
                }
            }
        }
    }
}
