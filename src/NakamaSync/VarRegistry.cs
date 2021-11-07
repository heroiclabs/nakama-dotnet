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

        internal Dictionary<string, IVar<bool>> Bools { get; }
        internal Dictionary<string, IVar<float>> Floats { get; }
        internal Dictionary<string, IVar<int>> Ints { get; }
        internal Dictionary<string, IVar<string>> Strings { get; }
        internal Dictionary<string, IVar<object>> Objects { get; }

        internal Dictionary<string, List<PresenceVar<bool>>> PresenceBools { get; }
        internal Dictionary<string, List<PresenceVar<float>>> PresenceFloats { get; }
        internal Dictionary<string, List<PresenceVar<int>>> PresenceInts { get; }
        internal Dictionary<string, List<PresenceVar<string>>> PresenceStrings { get; }
        internal Dictionary<string, List<PresenceVar<object>>> PresenceObjects { get; }

        internal Dictionary<string, SelfVar<bool>> SelfBools { get; }
        internal Dictionary<string, SelfVar<float>> SelfFloats { get; }
        internal Dictionary<string, SelfVar<int>> SelfInts { get; }
        internal Dictionary<string, SelfVar<string>> SelfStrings { get; }
        internal Dictionary<string, SelfVar<object>> SelfObjects { get; }

        private readonly HashSet<IVar> _registeredVars = new HashSet<IVar>();
        private readonly HashSet<string> _registeredKeys = new HashSet<string>();

        public VarRegistry()
        {
            Bools = new Dictionary<string, IVar<bool>>();
            Floats = new Dictionary<string, IVar<float>>();
            Ints = new Dictionary<string, IVar<int>>();
            Strings = new Dictionary<string, IVar<string>>();
            Objects = new Dictionary<string, IVar<object>>();

            PresenceBools = new Dictionary<string, List<PresenceVar<bool>>>();
            PresenceFloats = new Dictionary<string, List<PresenceVar<float>>>();
            PresenceInts = new Dictionary<string, List<PresenceVar<int>>>();
            PresenceStrings = new Dictionary<string, List<PresenceVar<string>>>();
            PresenceObjects = new Dictionary<string, List<PresenceVar<object>>>();

            SelfBools = new Dictionary<string, SelfVar<bool>>();
            SelfFloats = new Dictionary<string,SelfVar<float>>();
            SelfInts = new Dictionary<string, SelfVar<int>>();
            SelfStrings = new Dictionary<string, SelfVar<string>>();
            SelfObjects = new Dictionary<string, SelfVar<object>>();
        }

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
        // do this validation and batching after the match starts with an internal method in sync services or somewhere. there should be
        // no set of presence var without a corresponding self var
        public void Register(SelfVar<bool> selfVar)
        {
            RegisterSelfVar<bool>(selfVar, SelfBools);
        }

        public void Register(SelfVar<float> selfVar)
        {
            RegisterSelfVar<float>(selfVar, SelfFloats);
        }

        public void Register(SelfVar<int> selfVar)
        {
            RegisterSelfVar<int>(selfVar, SelfInts);
        }

        public void Register(SelfVar<string> selfVar)
        {
            RegisterSelfVar<string>(selfVar, SelfStrings);
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

        private void RegisterSelfVar<T>(SelfVar<T> var, Dictionary<string, SelfVar<T>> incomingDict)
        {
            // multiple vars with same key is expected
            _registeredKeys.Add(var.Key);

            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            incomingDict.Add(var.Key, var);
        }

        private void RegisterPresenceVar<T>(PresenceVar<T> var, Dictionary<string, List<PresenceVar<T>>> incomingDict)
        {
            // multiple vars with same key is expected
            _registeredKeys.Add(var.Key);

            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            if (!incomingDict.ContainsKey(var.Key))
            {
                incomingDict[var.Key] = new List<PresenceVar<T>>();
            }

            incomingDict[var.Key].Add(var);
        }

        // we use IVar<T> rather than SharedVar<T> to indicate to the compiler that we are supporting covariance.
        // for more background, see the `out` keyword in generic interfaces.
        private void RegisterSharedVar<T>(IVar<T> var, Dictionary<string, IVar<T>> incomingDict)
        {
            if (!_registeredKeys.Add(var.Key))
            {
                throw new InvalidOperationException($"Attempted to register duplicate key {var.Key}");
            }

            if (!_registeredVars.Add(var))
            {
                throw new InvalidOperationException($"Attempted to register duplicate var {var}");
            }

            incomingDict.Add(var.Key, var);
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
