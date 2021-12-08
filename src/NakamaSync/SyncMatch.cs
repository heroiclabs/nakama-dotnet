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
    internal class SyncMatch : ISyncMatch
    {
        public bool Authoritative => _match.Authoritative;
        public string Id => _match.Id;
        public string Label => _match.Label;
        public IEnumerable<IUserPresence> Presences => _match.Presences;
        public int Size => _match.Size;
        public IUserPresence Self => _match.Self;

        public event Action<IHostChangedEvent> OnHostChanged
        {
            add
            {
                HostTracker.OnHostChanged += value;
            }
            remove
            {
                HostTracker.OnHostChanged -= value;
            }
        }

        internal HostTracker HostTracker => _syncTrackers.HostTracker;
        internal PresenceTracker PresenceTracker => _syncTrackers.PresenceTracker;
        internal ISession Session => _session;
        internal ISocket Socket => _socket;
        internal SyncEncoding Encoding => _encoding;

        private readonly IMatch _match;
        private readonly ISession _session;
        private readonly SyncTrackers _syncTrackers;
        private ISocket _socket;
        private readonly SyncEncoding _encoding = new SyncEncoding();
        private readonly RpcRegistry _rpcRegistry;

        internal SyncMatch(ISocket socket, ISession session, IMatch match, VarRegistry varRegistry, RpcRegistry rpcRegistry, SyncTrackers syncTrackers)
        {
            _socket = socket;
            _session = session;
            _match = match;
            _rpcRegistry = rpcRegistry;
            _syncTrackers = syncTrackers;

            socket.ReceivedMatchState += HandleRpcMatchState;
        }

        private void HandleRpcMatchState(IMatchState obj)
        {
            if (obj.OpCode == _rpcRegistry.Opcode)
            {
                RpcEnvelope envelope = _encoding.Decode<RpcEnvelope>(obj.State);

                if (!_rpcRegistry.HasTarget(envelope.RpcKey.TargetId))
                {
                    throw new InvalidOperationException("Received rpc for non-existent target: " + envelope.RpcKey.TargetId);
                }

                var rpc = new RpcInvocation(_rpcRegistry.GetTarget(envelope.RpcKey.TargetId), envelope.RpcKey.MethodName, envelope.RequiredParameters, envelope.OptionalParameters);
                rpc.Invoke();
            }
        }

        public IUserPresence GetHostPresence()
        {
            return HostTracker.GetHost();
        }

        public List<IUserPresence> GetOtherPresences()
        {
            return  PresenceTracker.GetSortedOthers();
        }

        public List<IUserPresence> GetAllPresences()
        {
            return PresenceTracker.GetSortedPresences();
        }

        public bool IsSelfHost()
        {
            return HostTracker.IsSelfHost();
        }

        public void SendRpc(string rpcId, string targetId, IEnumerable<IUserPresence> targetPresences, object[] requiredParameters, object[] optionalParameters = null)
        {
            // todo name sure match exists
            var envelope = new RpcEnvelope();
            envelope.RequiredParameters = requiredParameters;
            envelope.OptionalParameters = optionalParameters;
            envelope.RpcKey = new RpcKey(rpcId, targetId);

            if (!_rpcRegistry.HasTarget(targetId))
            {
                throw new ArgumentException("Unrecognized rpc target id: " + targetId);
            }

            _socket.SendMatchStateAsync(_match.Id, _rpcRegistry.Opcode, _encoding.Encode(envelope), targetPresences);

            if (targetPresences != null && targetPresences.Any(presence => presence.UserId == Self.UserId))
            {
                if (!_rpcRegistry.HasTarget(targetId))
                {
                    throw new InvalidOperationException("Received rpc for non-existent target: " + targetId);
                }

                var rpc = new RpcInvocation(_rpcRegistry.GetTarget(targetId), rpcId, requiredParameters, optionalParameters);
                rpc.Invoke();
            }
        }

        public void SendRpc(string rpcId, string targetId, object[] parameters)
        {
            var envelope = new RpcEnvelope();
            envelope.RequiredParameters = parameters;
            envelope.RpcKey = new RpcKey(rpcId, targetId);

            if (!_rpcRegistry.HasTarget(targetId))
            {
                throw new ArgumentException("Unrecognized rpc target id: " + targetId);
            }

            _socket.SendMatchStateAsync(_match.Id, _rpcRegistry.Opcode, _encoding.Encode(envelope));
        }

        public void SetHost(IUserPresence newHost)
        {
            _syncTrackers.HostTracker.SetHost(newHost.UserId);
        }
    }
}
