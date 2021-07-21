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
    // todo block registration after match has started.
    public class VarRegistry
    {
        internal SharedVarRegistry SharedVarRegistry => _sharedVarRegistry;
        internal PresenceVarRegistry PresenceVarRegistry => _presenceVarRegistry;

        private readonly SharedVarRegistry _sharedVarRegistry;
        private readonly PresenceVarRegistry _presenceVarRegistry;
        private readonly HashSet<IVar> _registeredVars = new HashSet<IVar>();
        private readonly HashSet<string> _registeredKeys = new HashSet<string>();

        public VarRegistry()
        {
            _sharedVarRegistry = new SharedVarRegistry();
            _presenceVarRegistry = new PresenceVarRegistry();
        }

        public void Register(string key, SharedVar<bool> sharedBool)
        {
            Register<bool>(key, sharedBool, _sharedVarRegistry.SharedBools);
        }

        public void Register(string key, SharedVar<int> sharedInt)
        {
            Register<int>(key, sharedInt, _sharedVarRegistry.SharedInts);
        }

        public void Register(string key, SharedVar<float> sharedFloat)
        {
            Register<float>(key, sharedFloat, _sharedVarRegistry.SharedFloats);
        }

        public void Register(string key, SharedVar<string> sharedString)
        {
            Register<string>(key, sharedString, _sharedVarRegistry.SharedStrings);
        }

        // todo allow registration one-by-one and then validate each collection afterwards.
        // do this validation and batching after the match starts with an internal method in sync services or
        // somewhere.
        public void Register(string key, SelfVar<bool> selfVar)
        {
            Register<bool>(key, selfVar, _presenceVarRegistry.PresenceBools);
        }

        public void Register(string key, SelfVar<float> selfVar)
        {
            Register<float>(key, selfVar, _presenceVarRegistry.PresenceFloats);
        }

        public void Register(string key, SelfVar<int> selfVar)
        {
            Register<int>(key, selfVar, _presenceVarRegistry.PresenceInts);
        }

        public void Register(string key, SelfVar<string> selfVar)
        {
            Register<string>(key, selfVar, _presenceVarRegistry.PresenceStrings);
        }

        public void Register(string key, PresenceVar<bool> presenceVar)
        {
            Register<bool>(key, presenceVar, _presenceVarRegistry.PresenceBools);
        }

        public void Register(string key, PresenceVar<float> presenceVar)
        {
            Register<float>(key, presenceVar, _presenceVarRegistry.PresenceFloats);
        }

        public void Register(string key, PresenceVar<int> presenceVar)
        {
            Register<int>(key, presenceVar, _presenceVarRegistry.PresenceInts);
        }

        public void Register(string key, PresenceVar<string> presenceVar)
        {
            Register<string>(key, presenceVar, _presenceVarRegistry.PresenceStrings);
        }

        private void Register<T>(string key, SharedVar<T> var, Dictionary<string, SharedVar<T>> varDict)
        {
            if (!_registeredKeys.Add(key))
            {
                throw new InvalidOperationException($"Attempted to register duplicate key {key}");
            }

            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            varDict.Add(key, var);
        }

        private void Register<T>(string key, SelfVar<T> selfVar, Dictionary<string, PresenceVarCollection<T>> presenceVarCollections)
        {
            if (!_registeredVars.Add(selfVar))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {selfVar}");
            }

            PresenceVarCollection<T> presenceVarCollection =
                presenceVarCollections.ContainsKey(key) ?
                presenceVarCollections[key] :
                (presenceVarCollections[key] = new PresenceVarCollection<T>());

                if (presenceVarCollection.SelfVar != null)
                {
                    throw new InvalidOperationException("Cannot reassign register multiple SelfVar<T> to the same key.");
                }

                presenceVarCollection.SelfVar = selfVar;
        }

        private void Register<T>(string key, PresenceVar<T> presenceVar, Dictionary<string, PresenceVarCollection<T>> presenceVarCollections)
        {
            if (!_registeredVars.Add(presenceVar))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {presenceVar}");
            }

            PresenceVarCollection<T> presenceVarCollection =
                presenceVarCollections.ContainsKey(key) ?
                presenceVarCollections[key] :
                (presenceVarCollections[key] = new PresenceVarCollection<T>());

                presenceVarCollection.PresenceVars.Add(presenceVar);
        }

        internal HashSet<string> GetAllKeys()
        {
            return _registeredKeys;
        }

        internal void ReceiveMatch(IMatch match)
        {
            foreach (IVar var in _registeredVars)
            {
                var.Self = match.Self;
            }
        }
    }
}
