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

namespace NakamaSync
{
    public class VarRegistry
    {
        private readonly Dictionary<Type, IVarRegistry> _registries = new Dictionary<Type, IVarRegistry>();

        public void Register<T>(SharedVar<T> var)
        {
            if (!_registries.ContainsKey(typeof(T)))
            {
                _registries[typeof(T)] = new VarRegistry<T>();
            }

            VarRegistry<T> registry = (VarRegistry<T>) _registries[typeof(T)];

            registry.SharedVars.Register(var);
        }

        public void Register<T>(PresenceVar<T> var)
        {
            if (!_registries.ContainsKey(typeof(T)))
            {
                _registries[typeof(T)] = new VarRegistry<T>();
            }

            VarRegistry<T> registry = (VarRegistry<T>) _registries[typeof(T)];

            registry.PresenceVars.Register(var);
        }

        public void Register<T>(SelfVar<T> var)
        {
            if (!_registries.ContainsKey(typeof(T)))
            {
                _registries[typeof(T)] = new VarRegistry<T>();
            }

            VarRegistry<T> registry = (VarRegistry<T>) _registries[typeof(T)];
            registry.SelfVars.Register(var);
        }

        internal VarRegistry<T> GetRegistry<T>()
        {
            return (VarRegistry<T>) _registries[typeof(T)];
        }

        internal IEnumerable<IVar> GetAllVars()
        {
            return _registries.Values.SelectMany(registry => registry.GetAllVars());
        }

        internal IEnumerable<IPresenceRotatable> GetAllRotatables()
        {
            return _registries.Values.SelectMany(registry => registry.GetAllRotatables());
        }

        internal void ReceiveMatch(SyncMatch match)
        {
            foreach (IVarRegistry registry in _registries.Values)
            {
                registry.ReceiveMatch(match);
            }
        }

        internal Task GetPendingHandshake()
        {
            return Task.WhenAll(_registries.Values.Select(registry => registry.GetPendingHandshake()));
        }
    }

    internal class VarRegistry<T> : IVarRegistry
    {
        public VarRegistry<SharedVar<T>, T> SharedVars => _sharedVars;
        public VarRegistry<SelfVar<T>, T> SelfVars => _selfVars;
        public VarRegistry<PresenceVar<T>, T> PresenceVars => _presenceVars;

        private readonly VarRegistry<SharedVar<T>, T> _sharedVars = new VarRegistry<SharedVar<T>, T>(allowDuplicateKeys: false);
        private readonly VarRegistry<SelfVar<T>, T> _selfVars = new VarRegistry<SelfVar<T>, T>(allowDuplicateKeys: false);
        // multiple presence vars per key, each corresponding to a different user (or unoccupied)
        private readonly VarRegistry<PresenceVar<T>, T> _presenceVars = new VarRegistry<PresenceVar<T>, T>(allowDuplicateKeys: true);

        public IEnumerable<string> GetAllKeys()
        {
            var keys = new List<string>();
            keys.AddRange(_sharedVars.GetAllKeys());
            keys.AddRange(_selfVars.GetAllKeys());
            keys.AddRange(_presenceVars.GetAllKeys());
            return keys;
        }

        public IEnumerable<IVar> GetAllVars()
        {
            var vars = new List<IVar>();
            vars.AddRange(_sharedVars.GetAllVars());
            vars.AddRange(_selfVars.GetAllVars());
            vars.AddRange(_presenceVars.GetAllVars());
            return vars;
        }

        public bool ContainsKey(string key)
        {
            return _sharedVars.ContainsKey(key) || _selfVars.ContainsKey(key) || _presenceVars.ContainsKey(key);
        }

        public IEnumerable<IPresenceRotatable> GetAllRotatables()
        {
            return _presenceVars.Vars;
        }

        public Task GetPendingHandshake()
        {
            return Task.WhenAll(_presenceVars.GetPendingHandshake(), _selfVars.GetPendingHandshake(), _sharedVars.GetPendingHandshake());
        }

        public void ReceiveMatch(SyncMatch match)
        {
            _presenceVars.ReceiveMatch(match);
            _selfVars.ReceiveMatch(match);
            _sharedVars.ReceiveMatch(match);
        }
    }

    internal class VarRegistry<T, K> : IVarRegistry where T : Var<K>
    {
        public IEnumerable<T> Vars => _vars.Values;

        internal IEnumerable<IVar> RegisteredVars => new HashSet<IVar>(_registeredVars);

        private readonly bool _allowDuplicateKeys;
        private readonly HashSet<string> _registeredKeys = new HashSet<string>();
        private readonly HashSet<T> _registeredVars = new HashSet<T>();
        private readonly Dictionary<string, T> _vars = new Dictionary<string, T>();

        public VarRegistry(bool allowDuplicateKeys)
        {
            _allowDuplicateKeys = allowDuplicateKeys;
        }

        public bool ContainsKey(string key)
        {
            return _vars.ContainsKey(key);
        }

        public void Register(T var)
        {
            if (!_registeredKeys.Add(var.Key) && !_allowDuplicateKeys)
            {
                throw new InvalidOperationException($"Attempted to register duplicate key {var.Key}");
            }

            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            _vars.Add(var.Key, var);
        }

        public IEnumerable<string> GetAllKeys()
        {
            return _registeredKeys;
        }

        public IEnumerable<IVar> GetAllVars()
        {
            return _registeredVars;
        }

        public IEnumerable<IPresenceRotatable> GetAllRotatables()
        {
            return _registeredVars.OfType<IPresenceRotatable>();
        }

        public Task GetPendingHandshake()
        {
            return Task.WhenAll(_vars.Values.Select(var => (var as IVar).GetPendingHandshake()));
        }

        public void ReceiveMatch(SyncMatch match)
        {
            foreach (T var in _vars.Values)
            {
                var connection = new VarConnection<K>(match.Socket, match.Opcodes, match.PresenceTracker, match.HostTracker);
                var.ReceiveConnection(connection);
            }
        }
    }

    internal interface IVarRegistry
    {
        IEnumerable<string> GetAllKeys();
        IEnumerable<IVar> GetAllVars();
        IEnumerable<IPresenceRotatable> GetAllRotatables();
        Task GetPendingHandshake();
        void ReceiveMatch(SyncMatch match);
        bool ContainsKey(string key);
    }
}
