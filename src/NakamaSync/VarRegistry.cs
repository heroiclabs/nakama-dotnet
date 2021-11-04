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
        internal IEnumerable<IVar> RegisteredVars => new HashSet<IVar>(_registeredVars);

        internal SharedVarRegistry SharedVarRegistry => _sharedVarRegistry;
        internal OtherVarRegistry OtherVarRegistry => _otherVarRegistry;

        private readonly SharedVarRegistry _sharedVarRegistry;
        private readonly OtherVarRegistry _otherVarRegistry;
        private readonly HashSet<IVar> _registeredVars = new HashSet<IVar>();
        private readonly HashSet<string> _registeredKeys = new HashSet<string>();

        public VarRegistry()
        {
            _sharedVarRegistry = new SharedVarRegistry();
            _otherVarRegistry = new OtherVarRegistry();
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

        public void Register<T>(string key, SharedVar<T> sharedObject) where T : class
        {
            var asSharedObject = sharedObject as SharedVar<object>;
            _sharedVarRegistry.SharedObjects[key] = asSharedObject;
            Register<object>(key, asSharedObject, _sharedVarRegistry.SharedObjects);
        }

        // todo allow registration one-by-one and then validate each collection afterwards.
        // do this validation and batching after the match starts with an internal method in sync services or
        // somewhere.
        public void Register(string key, SelfVar<bool> selfVar)
        {
            Register<bool>(key, selfVar, _otherVarRegistry.PresenceBools);
        }

        public void Register(string key, SelfVar<float> selfVar)
        {
            Register<float>(key, selfVar, _otherVarRegistry.PresenceFloats);
        }

        public void Register(string key, SelfVar<int> selfVar)
        {
            Register<int>(key, selfVar, _otherVarRegistry.PresenceInts);
        }

        public void Register(string key, SelfVar<string> selfVar)
        {
            Register<string>(key, selfVar, _otherVarRegistry.PresenceStrings);
        }

        public void Register(string key, OtherVar<bool> OtherVar)
        {
            Register<bool>(key, OtherVar, _otherVarRegistry.PresenceBools);
        }

        public void Register(string key, OtherVar<float> OtherVar)
        {
            Register<float>(key, OtherVar, _otherVarRegistry.PresenceFloats);
        }

        public void Register(string key, OtherVar<int> OtherVar)
        {
            Register<int>(key, OtherVar, _otherVarRegistry.PresenceInts);
        }

        public void Register(string key, OtherVar<string> OtherVar)
        {
            Register<string>(key, OtherVar, _otherVarRegistry.PresenceStrings);
        }

        private void Register<T>(string key, ISharedVar<T> var, Dictionary<string, ISharedVar<T>> varDict)
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

        private void Register<T>(string key, SelfVar<T> selfVar, Dictionary<string, OtherVarCollection<T>> OtherVarCollections)
        {
            if (!_registeredVars.Add(selfVar))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {selfVar}");
            }

            OtherVarCollection<T> OtherVarCollection =
                OtherVarCollections.ContainsKey(key) ?
                OtherVarCollections[key] :
                (OtherVarCollections[key] = new OtherVarCollection<T>());

                if (OtherVarCollection.SelfVar != null)
                {
                    throw new InvalidOperationException("Cannot reassign register multiple SelfVar<T> to the same key.");
                }

                OtherVarCollection.SelfVar = selfVar;
        }

        private void Register<T>(string key, OtherVar<T> OtherVar, Dictionary<string, OtherVarCollection<T>> OtherVarCollections)
        {
            if (!_registeredVars.Add(OtherVar))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {OtherVar}");
            }

            OtherVarCollection<T> OtherVarCollection =
                OtherVarCollections.ContainsKey(key) ?
                OtherVarCollections[key] :
                (OtherVarCollections[key] = new OtherVarCollection<T>());

                OtherVarCollection.OtherVars.Add(OtherVar);
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
