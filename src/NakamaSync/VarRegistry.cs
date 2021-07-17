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
        internal Dictionary<string, SharedVar<bool>> SharedBools { get; }
        internal Dictionary<string, SharedVar<float>> SharedFloats { get; }
        internal Dictionary<string, SharedVar<int>> SharedInts { get; }
        internal Dictionary<string, SharedVar<string>> SharedStrings { get; }

        internal Dictionary<string, PresenceVar<bool>> UserBools { get; }
        internal Dictionary<string, PresenceVar<float>> UserFloats { get; }
        internal Dictionary<string, PresenceVar<int>> UserInts { get; }
        public Dictionary<string, PresenceVar<string>> UserStrings { get; }

        private readonly HashSet<IVar> _registeredVars = new HashSet<IVar>();
        private VarKeys _keys;

        internal VarRegistry(VarKeys keys)
        {
            _keys = keys;

            SharedBools = new Dictionary<string, SharedVar<bool>>();
            SharedFloats = new Dictionary<string, SharedVar<float>>();
            SharedInts = new Dictionary<string, SharedVar<int>>();
            SharedStrings = new Dictionary<string, SharedVar<string>>();

            UserBools = new Dictionary<string, PresenceVar<bool>>();
            UserFloats = new Dictionary<string, PresenceVar<float>>();
            UserInts = new Dictionary<string, PresenceVar<int>>();
            UserStrings = new Dictionary<string, PresenceVar<string>>();
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
            Register<bool, SharedVar<bool>>(id, sharedBool);
        }

        public void Register(string id, SharedVar<int> sharedInt)
        {
            Register<int, SharedVar<int>>(id, sharedInt);
        }

        public void Register(string id, SharedVar<float> sharedFloat)
        {
            Register<float, SharedVar<float>>(id, sharedFloat);
        }

        public void Register(string id, SharedVar<string> sharedString)
        {
            Register<string, SharedVar<string>>(id, sharedString);
        }

        public void Register(string id, PresenceVar<bool> presenceString)
        {
            Register<bool, PresenceVar<bool>>(id, presenceString);
        }

        public void Register(string id, PresenceVar<float> presenceFloat)
        {
            Register<float, PresenceVar<float>>(id, presenceFloat);
        }

        public void Register(string id, PresenceVar<int> presenceInt)
        {
            Register<int, PresenceVar<int>>(id, presenceInt);
        }

        public void Register(string id, PresenceVar<string> presenceString)
        {
            Register<string, PresenceVar<string>>(id, presenceString);
        }

        private void Register<T, TVar>(string key, TVar var) where TVar : Var<T>
        {
            if (!_registeredVars.Add(var))
            {
                throw new ArgumentException("Tried registering the same var with a different id: " + key);
            }

            _keys.RegisterKey(key, var.ValidationStatus);
        }
    }
}
