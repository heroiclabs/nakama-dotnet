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
        private readonly Dictionary<long, PresenceVarRotator> _rotatorsByOpcode = new Dictionary<long, PresenceVarRotator>();

        private Hashtable _registeredVars = new Hashtable();
        private Hashtable _registeredOpcodes = new Hashtable();

        private readonly int _opcodeStart;

        public VarRegistry(int opcodeStart = 0)
        {
            _opcodeStart = opcodeStart;
        }

        public void Register<T>(SharedVar<T> var)
        {
            if (_registeredVars.ContainsKey(_opcodeStart + var.Opcode))
            {
                throw new ArgumentException("Cannot register duplicate variable.");
            }

            if (_registeredOpcodes.ContainsKey(_opcodeStart + var.Opcode))
            {
                throw new ArgumentException("Cannot register duplicate opcode.");
            }

            VarSubRegistry<T> subegistry = GetOrAddSubregistry<T>(_opcodeStart + var.Opcode);
            subegistry.Register(var);
        }

        public void Register<T>(PresenceVar<T> var)
        {
            if (_registeredVars.ContainsKey(_opcodeStart + var.Opcode))
            {
                throw new ArgumentException("Cannot register duplicate variable.");
            }

            if (!_rotatorsByOpcode.ContainsKey(var.Opcode))
            {
                _rotatorsByOpcode.Add(var.Opcode, new PresenceVarRotator());
            }

            _rotatorsByOpcode[var.Opcode].AddPresenceVar(var);

            // multiple opcodes are okay for presence vars
            GetOrAddSubregistry<T>(_opcodeStart + var.Opcode).Register(var);
        }

        public void Register<T>(SelfVar<T> var)
        {
            if (_registeredVars.ContainsKey(_opcodeStart + var.Opcode))
            {
                throw new ArgumentException("Cannot register duplicate variable.");
            }

            if (_registeredOpcodes.ContainsKey(_opcodeStart + var.Opcode))
            {
                throw new ArgumentException("Cannot register duplicate opcode.");
            }

            VarSubRegistry<T> subegistry = GetOrAddSubregistry<T>(_opcodeStart + var.Opcode);
            subegistry.Register(var);
        }

        internal IEnumerable<IPresenceRotatable> GetPresenceRotatables()
        {
            return _subregistriesByType.Values.SelectMany(registry => registry.GetPresenceRotatables());
        }

        internal void ReceiveMatch(SyncMatch match)
        {
            foreach (IVarSubRegistry subRegistry in _subregistriesByType.Values)
            {
                subRegistry.ReceiveMatch(match);
            }

            foreach (PresenceVarRotator rotator in _rotatorsByOpcode.Values)
            {
                rotator.Subscribe(match.PresenceTracker);
                rotator.HandlePresencesAdded(match.Presences);
            }
        }

        private VarSubRegistry<T> GetOrAddSubregistry<T>(long opcode)
        {
            if (!_subregistriesByType.ContainsKey(typeof(T)))
            {
                var newSubRegistry = new VarSubRegistry<T>();
                _subregistriesByType[typeof(T)] = newSubRegistry;
                _subregistriesByOpcode[opcode] = newSubRegistry;
            }

            VarSubRegistry<T> registry = (VarSubRegistry<T>) _subregistriesByType[typeof(T)];
            return registry;
        }

        private void HandleReceivedMatchState(IMatchState matchState)
        {
            // could just be a regular piece of match data, i.e., not related to sync vars
            if (_subregistriesByOpcode.ContainsKey(matchState.OpCode))
            {
                IVarSubRegistry subRegistry = _subregistriesByOpcode[matchState.OpCode];
                subRegistry.ReceiveMatchState(matchState);
            }
        }
    }

    internal class VarSubRegistry<T> : IVarSubRegistry
    {
        private readonly Dictionary<long, List<Var<T>>> _vars = new Dictionary<long, List<Var<T>>>();
        private SyncMatch _syncMatch;

        public void ReceiveMatch(SyncMatch syncMatch)
        {
            _syncMatch = syncMatch;
            var allVars = _vars.Values.SelectMany(l => l);
            foreach (var var in allVars)
            {
                var.ReceiveSyncMatch(syncMatch);
            }
        }

        public void ReceiveMatchState(MatchState matchState)
        {
            if (!_vars.ContainsKey(matchState.OpCode))
            {
                throw new ArgumentException("Registry did not recognize opcode.");
            }

            var vars = _vars[matchState.OpCode];
            foreach (var var in vars)
            {
                var.HandleSerialized(matchState.UserPresence, _syncMatch.Encoding.Decode<SerializableVar<T>>(matchState.State));
            }
        }

        public void ReceiveMatchState(IMatchState state)
        {
            if (_vars.ContainsKey(state.OpCode))
            {
                ISerializableVar<T> serialized = _syncMatch.Encoding.Decode<ISerializableVar<T>>(state.State);
                var vars = _vars[state.OpCode];
                foreach (var var in vars)
                {
                    var.ReceiveSerializable(serialized);

                }
            }
        }

        public void Register(Var<T> var)
        {
            if (!_vars.ContainsKey(var.Opcode))
            {
                _vars[var.Opcode] = new List<Var<T>>();
            }

            _vars[var.Opcode].Add(var);
        }

        IEnumerable<IPresenceRotatable> IVarSubRegistry.GetPresenceRotatables()
        {
            return _vars.Values.SelectMany(l => l).OfType<IPresenceRotatable>();
        }
    }

    internal interface IVarSubRegistry
    {
        void ReceiveMatch(SyncMatch match);
        void ReceiveMatchState(IMatchState match);
        IEnumerable<IPresenceRotatable> GetPresenceRotatables();
    }
}
