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
using Nakama;

namespace NakamaSync
{
    public class SyncMatch : IMatch
    {
        public bool Authoritative => _match.Authoritative;
        public string Id => _match.Id;
        public string Label => _match.Label;
        public IEnumerable<IUserPresence> Presences => _match.Presences;
        public int Size => _match.Size;
        public IUserPresence Self => _match.Self;

        public event Action<HostChangedEvent> OnHostChanged
        {
            add
            {
                _hostTracker.OnHostChanged += value;
            }
            remove
            {
                _hostTracker.OnHostChanged -= value;
            }
        }

        internal HostTracker HostTracker => _hostTracker;
        internal PresenceTracker PresenceTracker => _presenceTracker;
        internal ISession Session => _session;
        internal ISocket Socket => _socket;
        internal SyncEncoding Encoding => _encoding;

        private readonly IMatch _match;
        private readonly ISession _session;
        private readonly PresenceTracker _presenceTracker;
        private readonly HostTracker _hostTracker;
        private ISocket _socket;
        private readonly SyncEncoding _encoding = new SyncEncoding();

        internal SyncMatch(ISocket socket, ISession session, IMatch match)
        {
            _socket = socket;
            _session = session;
            _match = match;
            _presenceTracker = new PresenceTracker(session.UserId);
            _hostTracker = new HostTracker(_presenceTracker);
        }

        public IUserPresence GetHostPresence()
        {
            return _hostTracker.GetHost();
        }

        public List<IUserPresence> GetOtherPresences()
        {
            return  _presenceTracker.GetSortedOthers();
        }

        public List<IUserPresence> GetAllPresences()
        {
            return _presenceTracker.GetSortedPresences();
        }

        public bool IsSelfHost()
        {
            return _hostTracker.IsSelfHost();
        }


/*
        public void SendRpc(IEnumerable<IUserPresence> targetPresences, string rpcId, string targetId, params object[] parameters)
        {
            var envelope = new RpcEnvelope();
            envelope.Parameters = parameters;
            envelope.RpcKey = new RpcKey(rpcId, targetId);
            _socket.SendRpc(envelope, targetPresences);
        }

        public void SendRpc(string rpcId, string targetId, params object[] parameters)
        {
            var envelope = new RpcEnvelope();
            envelope.Parameters = parameters;
            envelope.RpcKey = new RpcKey(rpcId, targetId);
            _socket.SendRpc(envelope);
        }
        */
    }
}
