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

            registry.RegisterSharedVar(var);
        }

        public void Register<T>(PresenceVar<T> var)
        {
            if (!_registries.ContainsKey(typeof(T)))
            {
                _registries[typeof(T)] = new VarRegistry<T>();
            }

            VarRegistry<T> registry = (VarRegistry<T>) _registries[typeof(T)];

            registry.RegisterPresenceVar(var);
        }

        public void Register<T>(SelfVar<T> var)
        {
            if (!_registries.ContainsKey(typeof(T)))
            {
                _registries[typeof(T)] = new VarRegistry<T>();
            }

            VarRegistry<T> registry = (VarRegistry<T>) _registries[typeof(T)];

            registry.RegisterSelfVar(var);
        }

        internal VarRegistry<T> GetRegistry<T>()
        {
            return (VarRegistry<T>) _registries[typeof(T)];
        }

        internal IEnumerable<IVar> GetAllVars()
        {
            return _registries.Values.SelectMany(registry => registry.GetAllVars());
        }
    }

    internal class VarRegistry<T> : IVarRegistry
    {
        public IEnumerable<SharedVar<T>> SharedVars => _sharedVars.Values;
        public IEnumerable<SelfVar<T>> SelfVars => _selfVars.Values;
        public IEnumerable<IEnumerable<PresenceVar<T>>> PresenceVars => _presenceVars.Values;

        private Dictionary<string, SharedVar<T>> _sharedVars;
        private Dictionary<string, SelfVar<T>> _selfVars;
        private Dictionary<string, List<PresenceVar<T>>> _presenceVars;

        private readonly HashSet<Var<T>> _registeredVars = new HashSet<Var<T>>();
        private readonly HashSet<string> _registeredKeys = new HashSet<string>();

        internal IEnumerable<IVar> RegisteredVars => new HashSet<IVar>(_registeredVars);

        public VarRegistry()
        {
            _sharedVars = new Dictionary<string, SharedVar<T>>();
            _selfVars = new Dictionary<string, SelfVar<T>>();
            _presenceVars = new Dictionary<string, List<PresenceVar<T>>>();
        }

        public SharedVar<T> GetSharedVar(string key)
        {
            return _sharedVars[key];
        }

        public IEnumerable<PresenceVar<T>> GetPresenceVars(string key)
        {
            return _presenceVars[key];
        }

        public bool ContainsSharedKey(string key)
        {
            return _sharedVars.ContainsKey(key);
        }

        public bool ContainsPresenceKey(string key)
        {
            return _selfVars.ContainsKey(key) || _presenceVars.ContainsKey(key);
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

            _sharedVars.Add(var.Key, var);
        }

        public void RegisterSelfVar(SelfVar<T> var)
        {
            // multiple vars with same key is expected
            _registeredKeys.Add(var.Key);

            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            _selfVars.Add(var.Key, var);
        }

        public void RegisterPresenceVar(PresenceVar<T> var)
        {
            // multiple vars with same key is expected
            _registeredKeys.Add(var.Key);

            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            if (!_presenceVars.ContainsKey(var.Key))
            {
                _presenceVars[var.Key] = new List<PresenceVar<T>>();
            }

            _presenceVars[var.Key].Add(var);
        }

        public HashSet<string> GetAllKeys()
        {
            return _registeredKeys;
        }

        public IEnumerable<Var<T>> GetAllVars()
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
        IEnumerable<IVar> GetAllVars();
        bool ContainsSharedKey(string key);
        bool ContainsPresenceKey(string key);
    }
}
