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

        internal Dictionary<string, List<IVar<bool>>> Bools { get; }
        internal Dictionary<string, List<IVar<float>>> Floats { get; }
        internal Dictionary<string, List<IVar<int>>> Ints { get; }
        internal Dictionary<string, List<IVar<string>>> Strings { get; }
        internal Dictionary<string, List<IVar<object>>> Objects { get; }

        internal Dictionary<string, List<PresenceVar<bool>>> PresenceBools { get; }
        internal Dictionary<string, List<PresenceVar<float>>> PresenceFloats { get; }
        internal Dictionary<string, List<PresenceVar<int>>> PresenceInts { get; }
        internal Dictionary<string, List<PresenceVar<string>>> PresenceStrings { get; }
        internal Dictionary<string, List<PresenceVar<object>>> PresenceObjects { get; }

        private readonly HashSet<IVar> _registeredVars = new HashSet<IVar>();
        private readonly HashSet<string> _registeredKeys = new HashSet<string>();

        public VarRegistry()
        {
            Bools = new Dictionary<string, List<IVar<bool>>>();
            Floats = new Dictionary<string, List<IVar<float>>>();
            Ints = new Dictionary<string, List<IVar<int>>>();
            Strings = new Dictionary<string, List<IVar<string>>>();
            Objects = new Dictionary<string, List<IVar<object>>>();

            PresenceBools = new Dictionary<string, List<PresenceVar<bool>>>();
            PresenceFloats = new Dictionary<string, List<PresenceVar<float>>>();
            PresenceInts = new Dictionary<string, List<PresenceVar<int>>>();
            PresenceStrings = new Dictionary<string, List<PresenceVar<string>>>();
            PresenceObjects = new Dictionary<string, List<PresenceVar<object>>>();
        }

        // TODO put all incoming vars in the same collection in the shared var registry

        public void Register(SharedVar<bool> sharedBool)
        {
            RegisterSharedVar<bool>(sharedBool, Bools);
        }

        public void Register(SharedVar<int> sharedInt)
        {
            RegisterSharedVar<int>(sharedInt, Ints);
        }

        public void Register(SharedVar<float> sharedFloat)
        {
            RegisterSharedVar<float>(sharedFloat, Floats);
        }

        public void Register(SharedVar<string> sharedString)
        {
            RegisterSharedVar<string>(sharedString, Strings);
        }

        public void Register<T>(SharedVar<T> sharedObject) where T : class
        {
            RegisterSharedVar<object>(sharedObject, Objects);
        }

        public void Register<T>(SharedVar<IDictionary<string, T>> sharedObject)
        {
            RegisterSharedVar<object>(sharedObject, Objects);
        }

        // todo allow registration of self and presence vars one-by-one and then validate each group of self and presence vars afterwards.
        // do this validation and batching after the match starts with an internal method in sync services or
        // somewhere.

        public void Register(SelfVar<bool> selfVar)
        {
            RegisterSelfVar<bool>(selfVar, Bools);
        }

        public void Register(SelfVar<float> selfVar)
        {
            RegisterSelfVar<float>(selfVar, Floats);
        }

        public void Register(SelfVar<int> selfVar)
        {
            RegisterSelfVar<int>(selfVar, Ints);
        }

        public void Register(SelfVar<string> selfVar)
        {
            RegisterSelfVar<string>(selfVar, Strings);
        }

        public void Register(PresenceVar<bool> presenceVar)
        {
            RegisterPresenceVar<bool>(presenceVar, PresenceBools);
        }

        public void Register(PresenceVar<float> presenceVar)
        {
            RegisterPresenceVar<float>(presenceVar, PresenceFloats);
        }

        public void Register(PresenceVar<int> presenceVar)
        {
            RegisterPresenceVar<int>(presenceVar, PresenceInts);
        }

        public void Register(PresenceVar<string> presenceVar)
        {
            RegisterPresenceVar<string>(presenceVar, PresenceStrings);
        }

        private void RegisterSelfVar<T>(SelfVar<T> var, Dictionary<string, List<IVar<T>>> incomingDict)
        {
            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            List<IVar<T>> vars = incomingDict.ContainsKey(var.Key) ?
                                         incomingDict[var.Key] :
                                         new List<IVar<T>>();

            vars.Add(var);
        }

        private void RegisterPresenceVar<T>(PresenceVar<T> var, Dictionary<string, List<PresenceVar<T>>> incomingDict)
        {
            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            List<PresenceVar<T>> vars = incomingDict.ContainsKey(var.Key) ?
                                         incomingDict[var.Key] :
                                         new List<PresenceVar<T>>();

            vars.Add(var);
        }

        private void RegisterSharedVar<T>(IVar<T> var, Dictionary<string, List<IVar<T>>> incomingDict)
        {
            if (!_registeredKeys.Add(var.Key))
            {
                throw new InvalidOperationException($"Attempted to register duplicate key {var.Key}");
            }

            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            var vars = new List<IVar<T>>();
            vars.Add(var);

            incomingDict.Add(var.Key, vars);
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
