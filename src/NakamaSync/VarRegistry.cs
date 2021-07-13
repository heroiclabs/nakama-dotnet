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

        public Dictionary<string, UserVar<bool>> UserBools { get; }
        public Dictionary<string, UserVar<float>> UserFloats { get; }
        public Dictionary<string, UserVar<int>> UserInts { get; }
        public Dictionary<string, UserVar<string>> UserStrings { get; }

        private readonly HashSet<object> _registeredVars = new HashSet<object>();

        public VarRegistry()
        {
            SharedBools = new Dictionary<string, SharedVar<bool>>();
            SharedFloats = new Dictionary<string, SharedVar<float>>();
            SharedInts = new Dictionary<string, SharedVar<int>>();
            SharedStrings = new Dictionary<string, SharedVar<string>>();

            UserBools = new Dictionary<string, UserVar<bool>>();
            UserFloats = new Dictionary<string, UserVar<float>>();
            UserInts = new Dictionary<string, UserVar<int>>();
            UserStrings = new Dictionary<string, UserVar<string>>();
        }

        internal void Register(VarKeys keys, IUserPresence self)
        {
            Register(keys, SharedBools, self);
            Register(keys, SharedFloats, self);
            Register(keys, SharedInts, self);
            Register(keys, SharedStrings, self);

            Register(keys, UserBools, self);
            Register(keys, UserFloats, self);
            Register(keys, UserInts, self);
            Register(keys, UserStrings, self);
        }

        private void Register<TVar>(VarKeys keys, Dictionary<string, TVar> vars, IUserPresence self) where TVar : IVar
        {
            foreach (KeyValuePair<string, TVar> kvp in vars)
            {
                if (!_registeredVars.Add(kvp.Key))
                {
                    throw new ArgumentException("Tried registering the same shared var with a different id: " + kvp.Key);
                }

                keys.RegisterKey(kvp.Key, kvp.Value.GetValidationStatus());
                kvp.Value.Self = self;
            }
        }
    }
}