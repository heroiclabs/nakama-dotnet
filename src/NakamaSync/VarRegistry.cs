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
    // todo block registration after match has started.
    public class VarRegistry
    {
        internal VarRegistry<bool> Bools => _bools;
        internal VarRegistry<float> Floats => _floats;
        internal VarRegistry<int> Ints => _ints;
        internal VarRegistry<string> Strings => _strings;

        private readonly VarRegistry<bool> _bools = new VarRegistry<bool>();
        private readonly VarRegistry<float> _floats = new VarRegistry<float>();
        private readonly VarRegistry<int> _ints = new VarRegistry<int>();
        private readonly VarRegistry<string> _strings = new VarRegistry<string>();

        private readonly Dictionary<Type, IVarRegistry> _userRegistries = new Dictionary<Type, IVarRegistry>();
        private readonly List<IVarRegistry> _allRegistries = new List<IVarRegistry>();

        public VarRegistry()
        {
            _allRegistries.Add(_bools);
            _allRegistries.Add(_floats);
            _allRegistries.Add(_ints);
            _allRegistries.Add(_strings);
        }

        public IEnumerable<string> GetAllKeys()
        {
            return _allRegistries.SelectMany(registry => registry.GetAllKeys());
        }

        public void Register(SharedVar<bool> sharedBool)
        {
            _bools.RegisterSharedVar(sharedBool);
        }

        public void Register(SharedVar<int> sharedInt)
        {
            _ints.RegisterSharedVar(sharedInt);
        }

        public void Register(SharedVar<float> sharedFloat)
        {
            _floats.RegisterSharedVar(sharedFloat);
        }

        public void Register(SharedVar<string> sharedString)
        {
            _strings.RegisterSharedVar(sharedString);
        }

        // todo allow registration of self and presence vars one-by-one and then validate each group of self and presence vars afterwards.
        // do this validation and batching after the match starts with an internal method in sync services or somewhere. there should be
        // no set of presence var without a corresponding self var
        public void Register(SelfVar<bool> selfVar)
        {
            _bools.RegisterSelfVar(selfVar);
        }

        public void Register(SelfVar<float> selfVar)
        {
            _floats.RegisterSelfVar(selfVar);
        }

        public void Register(SelfVar<int> selfVar)
        {
            _ints.RegisterSelfVar(selfVar);
        }

        public void Register(SelfVar<string> selfVar)
        {
            _strings.RegisterSelfVar(selfVar);
        }

        public void Register(PresenceVar<bool> presenceVar)
        {
            _bools.RegisterPresenceVar(presenceVar);
        }

        public void Register(PresenceVar<float> presenceVar)
        {
            _floats.RegisterPresenceVar(presenceVar);
        }

        public void Register(PresenceVar<int> presenceVar)
        {
            _ints.RegisterPresenceVar(presenceVar);
        }

        public void Register(PresenceVar<string> presenceVar)
        {
            _strings.RegisterPresenceVar(presenceVar);
        }

        internal void Reset()
        {
            var allVars = GetAllVars();

            foreach (var var in allVars)
            {
                var.Reset();
            }
        }

        internal void ReceiveMatch(IMatch match)
        {
            foreach (VarRegistry var in _allRegistries)
            {
                var.ReceiveMatch(match);
            }
        }

        internal List<IVar> GetAllVars()
        {
            var allVars = new List<IVar>();

            foreach (var registry in _allRegistries)
            {
                foreach (var var in allVars)
                {
                    allVars.AddRange(registry.GetAllVars());
                }
            }

            return allVars;
        }
    }

    internal class VarRegistry<T> : IVarRegistry
    {
        public Dictionary<string, SharedVar<T>> SharedVars { get; }
        public Dictionary<string, SelfVar<T>> SelfVars { get; }
        public Dictionary<string, List<PresenceVar<T>>> PresenceVars { get; }

        private readonly HashSet<IVar<T>> _registeredVars = new HashSet<IVar<T>>();
        private readonly HashSet<string> _registeredKeys = new HashSet<string>();

        internal IEnumerable<IVar> RegisteredVars => new HashSet<IVar>(_registeredVars);

        public VarRegistry()
        {
            SharedVars = new Dictionary<string, SharedVar<T>>();
            SelfVars = new Dictionary<string, SelfVar<T>>();
            PresenceVars = new Dictionary<string, List<PresenceVar<T>>>();
        }

        public void ReceiveMatch(IMatch match)
        {
            foreach (IVar var in _registeredVars)
            {
                var.Self = match.Self;
            }
        }

        public void RegisterSharedVar(SharedVar<T> var)
        {
            if (!_registeredKeys.Add(var.Key))
            {
                throw new InvalidOperationException($"Attempted to register duplicate key {var.Key}");
            }

            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            SharedVars.Add(var.Key, var);
        }

        public void RegisterSelfVar(SelfVar<T> var)
        {
            // multiple vars with same key is expected
            _registeredKeys.Add(var.Key);

            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            SelfVars.Add(var.Key, var);
        }

        public void RegisterPresenceVar(PresenceVar<T> var)
        {
            // multiple vars with same key is expected
            _registeredKeys.Add(var.Key);

            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            if (!PresenceVars.ContainsKey(var.Key))
            {
                PresenceVars[var.Key] = new List<PresenceVar<T>>();
            }

            PresenceVars[var.Key].Add(var);
        }

        public HashSet<string> GetAllKeys()
        {
            return _registeredKeys;
        }

        public IEnumerable<IVar<T>> GetAllVars()
        {
            return _registeredVars;
        }

        IEnumerable<IVar> IVarRegistry.GetAllVars()
        {
            return _registeredVars;
        }
    }

    internal interface IVarRegistry
    {
        HashSet<string> GetAllKeys();
        void ReceiveMatch(IMatch match);
        IEnumerable<IVar> GetAllVars();

    }
}
