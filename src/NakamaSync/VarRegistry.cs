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

        public void Register(string key, PresenceVarCollection<bool> presenceVarCollection)
        {
            Register<bool>(key, presenceVarCollection, _presenceVarRegistry.PresenceBools);
        }

        public void Register(string key, PresenceVarCollection<float> presenceVarCollection)
        {
            Register<float>(key, presenceVarCollection, _presenceVarRegistry.PresenceFloats);
        }

        public void Register(string key, PresenceVarCollection<int> presenceVarCollection)
        {
            Register<int>(key, presenceVarCollection, _presenceVarRegistry.PresenceInts);
        }

        public void Register(string key, PresenceVarCollection<string> presenceVarCollection)
        {
            Register<string>(key, presenceVarCollection, _presenceVarRegistry.PresenceStrings);
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

        private void Register<T>(string key, PresenceVarCollection<T> presenceVarCollection, Dictionary<string, PresenceVarCollection<T>> varDict)
        {
            if (!_registeredKeys.Add(key))
            {
                throw new InvalidOperationException($"Attempted to register duplicate key {key}");
            }

            if (!_registeredVars.Add(presenceVarCollection.SelfVar))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {presenceVarCollection.SelfVar}");
            }

            foreach (var presenceVar in presenceVarCollection.PresenceVars)
            {
                if (!_registeredVars.Add(presenceVar))
                {
                    throw new InvalidOperationException($"Attempted to register duplicate var {presenceVar}");
                }
            }

            varDict[key] = presenceVarCollection;
        }

        public HashSet<string> GetAllKeys()
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
