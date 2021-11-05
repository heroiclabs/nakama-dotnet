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

        // TODO put all incoming vars in the same collection in the shared var registry

        public void Register(SharedVar<bool> sharedBool)
        {
            Register<bool>(sharedBool, _sharedVarRegistry.SharedBoolsIncoming);
        }

        public void Register(SharedVar<int> sharedInt)
        {
            Register<int>(sharedInt, _sharedVarRegistry.SharedIntsIncoming);
        }

        public void Register(SharedVar<float> sharedFloat)
        {
            Register<float>(sharedFloat, _sharedVarRegistry.SharedFloatsIncoming);
        }

        public void Register(SharedVar<string> sharedString)
        {
            Register<string>(sharedString, _sharedVarRegistry.SharedStringsIncoming);
        }

        public void Register<T>(SharedVar<T> sharedObject) where T : class
        {
            Register<object>(sharedObject, _sharedVarRegistry.SharedObjectsIncoming);
        }

        public void Register<T>(SharedVar<IDictionary<string, T>> sharedObject)
        {
            Register<object>(sharedObject, _sharedVarRegistry.SharedObjectsIncoming);
        }

        // todo allow registration one-by-one and then validate each collection afterwards.
        // do this validation and batching after the match starts with an internal method in sync services or
        // somewhere.
        public void Register(SelfVar<bool> selfVar)
        {
            Register<bool>(selfVar, _otherVarRegistry.PresenceBools);
        }

        public void Register(SelfVar<float> selfVar)
        {
            Register<float>(selfVar, _otherVarRegistry.PresenceFloats);
        }

        public void Register(SelfVar<int> selfVar)
        {
            Register<int>(selfVar, _otherVarRegistry.PresenceInts);
        }

        public void Register(SelfVar<string> selfVar)
        {
            Register<string>(selfVar, _otherVarRegistry.PresenceStrings);
        }

        public void Register(OtherVar<bool> otherVar)
        {
            Register<bool>(otherVar, _otherVarRegistry.PresenceBools);
        }

        public void Register(OtherVar<float> otherVar)
        {
            Register<float>(otherVar, _otherVarRegistry.PresenceFloats);
        }

        public void Register(OtherVar<int> otherVar)
        {
            Register<int>(otherVar, _otherVarRegistry.PresenceInts);
        }

        public void Register(OtherVar<string> otherVar)
        {
            Register<string>(otherVar, _otherVarRegistry.PresenceStrings);
        }

        private void Register<T>(IIncomingVar<T> incomingVar, Dictionary<string, IIncomingVar<T>> incomingDict)
        {
            if (!_registeredKeys.Add(incomingVar.Key))
            {
                throw new InvalidOperationException($"Attempted to register duplicate key {incomingVar.Key}");
            }

            if (!_registeredVars.Add(incomingVar))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {incomingVar}");
            }

            incomingDict.Add(incomingVar.Key, incomingVar);
        }

        private void Register<T>(SelfVar<T> selfVar, Dictionary<string, OtherVarCollection<T>> otherVarCollections)
        {
            if (!_registeredVars.Add(selfVar))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {selfVar}");
            }

            OtherVarCollection<T> otherVarCollection =
                otherVarCollections.ContainsKey(selfVar.Key) ?
                otherVarCollections[selfVar.Key] :
                (otherVarCollections[selfVar.Key] = new OtherVarCollection<T>());

                if (otherVarCollection.SelfVar != null)
                {
                    throw new InvalidOperationException("Cannot reassign register multiple SelfVar<T> to the same key.");
                }

                otherVarCollection.SelfVar = selfVar;
        }

        private void Register<T>(OtherVar<T> otherVar, Dictionary<string, OtherVarCollection<T>> otherVarCollections)
        {
            if (!_registeredVars.Add(otherVar))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {otherVar}");
            }

            OtherVarCollection<T> OtherVarCollection =
                otherVarCollections.ContainsKey(otherVar.Key) ?
                otherVarCollections[otherVar.Key] :
                (otherVarCollections[otherVar.Key] = new OtherVarCollection<T>());

                OtherVarCollection.OtherVars.Add(otherVar);
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
