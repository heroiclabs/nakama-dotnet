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
        public Dictionary<string, SharedVar<bool>> SharedBools { get; }
        public Dictionary<string, SharedVar<float>> SharedFloats { get; }
        public Dictionary<string, SharedVar<int>> SharedInts { get; }
        public Dictionary<string, SharedVar<string>> SharedStrings { get; }

        public Dictionary<string, PresenceVar<bool>> UserBools { get; }
        public Dictionary<string, PresenceVar<float>> UserFloats { get; }
        public Dictionary<string, PresenceVar<int>> UserInts { get; }
        public Dictionary<string, PresenceVar<string>> UserStrings { get; }

        private readonly HashSet<object> _registeredVars = new HashSet<object>();

        public VarRegistry()
        {
            SharedBools = new Dictionary<string, SharedVar<bool>>();
            SharedFloats = new Dictionary<string, SharedVar<float>>();
            SharedInts = new Dictionary<string, SharedVar<int>>();
            SharedStrings = new Dictionary<string, SharedVar<string>>();

            UserBools = new Dictionary<string, PresenceVar<bool>>();
            UserFloats = new Dictionary<string, PresenceVar<float>>();
            UserInts = new Dictionary<string, PresenceVar<int>>();
            UserStrings = new Dictionary<string, PresenceVar<string>>();
        }

        internal void ReceiveMatch(VarKeys keys, IMatch match)
        {
            Register<bool, SharedVar<bool>>(keys, SharedBools, match.Self);
            Register<float, SharedVar<float>>(keys, SharedFloats, match.Self);
            Register<int, SharedVar<int>>(keys, SharedInts, match.Self);
            Register<string, SharedVar<string>>(keys, SharedStrings, match.Self);

            Register<bool, PresenceVar<bool>>(keys, UserBools, match.Self);
            Register<float, PresenceVar<float>>(keys, UserFloats, match.Self);
            Register<int, PresenceVar<int>>(keys, UserInts, match.Self);
            Register<string, PresenceVar<string>>(keys, UserStrings, match.Self);
        }

        private void Register<T, TVar>(VarKeys keys, Dictionary<string, TVar> vars, IUserPresence self) where TVar : Var<T>
        {
            foreach (KeyValuePair<string, TVar> kvp in vars)
            {
                if (!_registeredVars.Add(kvp.Value))
                {
                    throw new ArgumentException("Tried registering the same var with a different id: " + kvp.Key);
                }

                keys.RegisterKey(kvp.Key, kvp.Value.ValidationStatus);
                kvp.Value.Self = self;
            }
        }
    }
}