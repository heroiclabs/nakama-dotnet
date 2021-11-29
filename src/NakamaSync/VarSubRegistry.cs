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
using System.Threading.Tasks;
using Nakama;

namespace NakamaSync
{
    internal class VarSubRegistry<T> : IVarSubRegistry
    {
        public bool ReceivedSyncMatch => _syncMatch != null;
        //todo this is just shared and self vars at the moment and not presence vars which is weird.
        private readonly Dictionary<long, List<Var<T>>> _vars = new Dictionary<long, List<Var<T>>>();
        private SyncMatch _syncMatch;
        private readonly long _opcodeStart;
        private PresenceVarFactory<T> _factory;
        private int _handshakeTimeoutSec;

        public VarSubRegistry(int opcodeStart, int handshakeTimeoutSec)
        {
            _opcodeStart = opcodeStart;
            _handshakeTimeoutSec = handshakeTimeoutSec;
            _factory = new PresenceVarFactory<T>(handshakeTimeoutSec);
        }

        public Task ReceiveMatch(SyncMatch syncMatch)
        {
            _syncMatch = syncMatch;

            var handshakeTasks = new List<Task>();
            handshakeTasks.Add(_factory.ReceiveSyncMatch(syncMatch));

            var allVars = _vars.Values.SelectMany(l => l);
            foreach (var var in allVars)
            {
                handshakeTasks.Add(var.ReceiveSyncMatch(syncMatch, _handshakeTimeoutSec));
            }

            return Task.WhenAll(handshakeTasks);
        }

        public void ReceiveMatchState(IMatchState state)
        {
            if (_syncMatch == null)
            {
                throw new NullReferenceException("Received match state with null sync match.");
            }

            if (_vars.ContainsKey(_opcodeStart + state.OpCode))
            {
                SerializableVar<T> serialized = _syncMatch.Encoding.Decode<SerializableVar<T>>(state.State);
                var vars = _vars[_opcodeStart + state.OpCode];

                foreach (var var in vars)
                {
                    // TODO hack, this needs a refactor
                    if (var is SelfVar<T>)
                    {
                        continue;
                    }

                    var.ReceiveSerialized(state.UserPresence, serialized);
                }

                _factory.HandleSerialized(state.UserPresence, _opcodeStart + state.OpCode, serialized);
            }
        }

        public void Register(Var<T> var)
        {
            if (!_vars.ContainsKey(_opcodeStart + var.Opcode))
            {
                _vars[_opcodeStart + var.Opcode] = new List<Var<T>>();
            }

            _vars[_opcodeStart + var.Opcode].Add(var);

            if (_syncMatch != null && !var.HasSyncMatch)
            {
                // deferred registration of variable
                var.ReceiveSyncMatch(_syncMatch, _handshakeTimeoutSec);
            }
        }

        public void Reset()
        {
            foreach (Var<T> var in _vars.Values.SelectMany(l => l))
            {
                var.Reset();
            }
        }

        public void AddGroupVar(long opcode, GroupVar<T> groupVar)
        {
            _factory.AddGroupVar(opcode, groupVar);
        }

        public void ReceivePresenceEvent(IMatchPresenceEvent evt)
        {
            _factory.ReceivePresenceEvent(evt);
        }
    }

    internal interface IVarSubRegistry
    {
        void ReceivePresenceEvent(IMatchPresenceEvent evt);
        Task ReceiveMatch(SyncMatch match);
        void ReceiveMatchState(IMatchState match);
        void Reset();
    }
}
