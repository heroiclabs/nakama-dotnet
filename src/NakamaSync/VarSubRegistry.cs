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
    internal class VarSubRegistry<T> : IVarSubRegistry
    {
        private readonly Dictionary<long, List<Var<T>>> _vars = new Dictionary<long, List<Var<T>>>();
        private SyncMatch _syncMatch;
        private readonly long _opcodeStart;
        private PresenceVarRotator<T> _rotator = new PresenceVarRotator<T>();

        public VarSubRegistry(int opcodeStart)
        {
            _opcodeStart = opcodeStart;
        }

        public void ReceiveMatch(SyncMatch syncMatch)
        {
            _syncMatch = syncMatch;
            var allVars = _vars.Values.SelectMany(l => l);
            foreach (var var in allVars)
            {
                var.ReceiveSyncMatch(syncMatch);
            }
        }

        public void ReceiveMatchState(IMatchState state)
        {
            if (_vars.ContainsKey(_opcodeStart + state.OpCode))
            {
                SerializableVar<T> serialized = null;
                try
                {
                    serialized = _syncMatch.Encoding.Decode<SerializableVar<T>>(state.State);

                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }

                var vars = _vars[_opcodeStart + state.OpCode];

                foreach (var var in vars)
                {
                    var.HandleSerialized(state.UserPresence, serialized);
                }
            }
        }

        public void Register(Var<T> var)
        {
            if (!_vars.ContainsKey(_opcodeStart + var.Opcode))
            {
                _vars[_opcodeStart + var.Opcode] = new List<Var<T>>();
            }

            _vars[_opcodeStart + var.Opcode].Add(var);
        }

        public void Reset()
        {
            foreach (Var<T> var in _vars.Values.SelectMany(l => l))
            {
                var.Reset();
            }
        }

        public void AddRotatorOpcode(long opcode)
        {
            _rotator.AddOpcode(opcode);
        }
    }

    internal interface IVarSubRegistry
    {
        void ReceiveMatch(SyncMatch match);
        void ReceiveMatchState(IMatchState match);
        void Reset();
    }
}
