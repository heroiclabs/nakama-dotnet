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
        internal VarKeys VarKeys => _keys;

        internal Dictionary<string, SharedVar<bool>> SharedBools { get; }
        internal Dictionary<string, SharedVar<float>> SharedFloats { get; }
        internal Dictionary<string, SharedVar<int>> SharedInts { get; }
        internal Dictionary<string, SharedVar<string>> SharedStrings { get; }

        internal Dictionary<string, PresenceVar<bool>> PresenceBools { get; }
        internal Dictionary<string, PresenceVar<float>> PresenceFloats { get; }
        internal Dictionary<string, PresenceVar<int>> PresenceInts { get; }
        internal Dictionary<string, PresenceVar<string>> PresenceStrings { get; }

        private readonly VarKeys _keys = new VarKeys();
        private readonly HashSet<IVar> _registeredVars = new HashSet<IVar>();

        public VarRegistry()
        {
            SharedBools = new Dictionary<string, SharedVar<bool>>();
            SharedFloats = new Dictionary<string, SharedVar<float>>();
            SharedInts = new Dictionary<string, SharedVar<int>>();
            SharedStrings = new Dictionary<string, SharedVar<string>>();

            PresenceBools = new Dictionary<string, PresenceVar<bool>>();
            PresenceFloats = new Dictionary<string, PresenceVar<float>>();
            PresenceInts = new Dictionary<string, PresenceVar<int>>();
            PresenceStrings = new Dictionary<string, PresenceVar<string>>();
        }

        internal void ReceiveMatch(IMatch match)
        {
            foreach (IVar var in _registeredVars)
            {
                var.Self = match.Self;
            }
        }

        public void Register(string id, SharedVar<bool> sharedBool)
        {
            Register<bool, SharedVar<bool>>(id, sharedBool, SharedBools);
        }

        public void Register(string id, SharedVar<int> sharedInt)
        {
            Register<int, SharedVar<int>>(id, sharedInt, SharedInts);
        }

        public void Register(string id, SharedVar<float> sharedFloat)
        {
            Register<float, SharedVar<float>>(id, sharedFloat, SharedFloats);
        }

        public void Register(string id, SharedVar<string> sharedString)
        {
            Register<string, SharedVar<string>>(id, sharedString, SharedStrings);
        }

        public void Register(string id, PresenceVar<bool> presenceBool)
        {
            Register<bool, PresenceVar<bool>>(id, presenceBool, PresenceBools);
        }

        public void Register(string id, PresenceVar<float> presenceFloat)
        {
            Register<float, PresenceVar<float>>(id, presenceFloat, PresenceFloats);
        }

        public void Register(string id, PresenceVar<int> presenceInt)
        {
            Register<int, PresenceVar<int>>(id, presenceInt, PresenceInts);
        }

        public void Register(string id, PresenceVar<string> presenceString)
        {
            Register<string, PresenceVar<string>>(id, presenceString, PresenceStrings);
        }

        private void Register<T, TVar>(string key, TVar var, Dictionary<string, TVar> dict) where TVar : Var<T>
        {
            if (!_registeredVars.Add(var))
            {
                throw new ArgumentException("Tried registering the same var with a different id: " + key);
            }

            _keys.RegisterKey(key, var.ValidationStatus);
            dict.Add(key,var);
        }
    }
}
