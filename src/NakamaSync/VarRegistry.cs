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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nakama;

namespace NakamaSync
{
    public class VarRegistry
    {
        private readonly Dictionary<Type, IVarSubRegistry> _subregistriesByType = new Dictionary<Type, IVarSubRegistry>();
        private readonly Dictionary<long, IVarSubRegistry> _subregistriesByOpcode = new Dictionary<long, IVarSubRegistry>();

        private readonly int _opcodeStart;
        private readonly SyncMatch _syncMatch;
        private bool _attachedReset = false;

        public VarRegistry(int opcodeStart = 0)
        {
            _opcodeStart = opcodeStart;
        }

        public void Register<T>(SharedVar<T> var)
        {
            VarSubRegistry<T> subegistry = GetOrAddSubregistry<T>(_opcodeStart + var.Opcode);
            subegistry.Register(var);
        }

        public void Register<T>(GroupVar<T> var)
        {
            VarSubRegistry<T> subegistry = GetOrAddSubregistry<T>(_opcodeStart + var.Opcode);
            subegistry.AddRotatorOpcode(_opcodeStart + var.Opcode);
            subegistry.Register(var);
        }

        internal void ReceiveMatch(SyncMatch match)
        {
            foreach (IVarSubRegistry subRegistry in _subregistriesByType.Values)
            {
                subRegistry.ReceiveMatch(match);
            }

            if (!_attachedReset)
            {
                // todo think about race between this event and inside the presence var rotator
                match.Socket.ReceivedMatchPresence += (evt) =>
                {
                    if (evt.Leaves.Any(leave => leave.UserId == match.Session.UserId))
                    {
                        foreach (IVarSubRegistry subRegistry in _subregistriesByType.Values)
                        {
                            subRegistry.Reset();
                        }
                    }
                };
            }

            _attachedReset = true;
        }

        private VarSubRegistry<T> GetOrAddSubregistry<T>(long combinedOpcode)
        {
            if (!_subregistriesByType.ContainsKey(typeof(T)))
            {
                var newSubRegistry = new VarSubRegistry<T>(_opcodeStart);
                _subregistriesByType[typeof(T)] = newSubRegistry;
                _subregistriesByOpcode[combinedOpcode] = newSubRegistry;
            }

            VarSubRegistry<T> registry = (VarSubRegistry<T>) _subregistriesByType[typeof(T)];
            return registry;
        }

        internal void HandleReceivedMatchState(IMatchState matchState)
        {
            // could just be a regular piece of match data, i.e., not related to sync vars
            if (_subregistriesByOpcode.ContainsKey(_opcodeStart + matchState.OpCode))
            {
                IVarSubRegistry subRegistry = _subregistriesByOpcode[_opcodeStart + matchState.OpCode];
                subRegistry.ReceiveMatchState(matchState);
            }
        }

        internal void HandleMatchClosed()
        {
            foreach (IVarSubRegistry subRegistry in _subregistriesByType.Values)
            {
                subRegistry.Reset();
            }
        }
    }
}
