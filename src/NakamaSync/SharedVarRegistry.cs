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
    internal class SharedVarRegistry
    {
        public SharedVars SharedVars => _sharedVars;

        private ISession _session;
        private readonly SharedVars _sharedVars;
        private readonly SyncVarKeys _keys;

        private readonly HashSet<object> _registeredVars = new HashSet<object>();

        public SharedVarRegistry(ISession session, SharedVars sharedVars, SyncVarKeys keys)
        {
            _session = session;
            _sharedVars = sharedVars;
            _keys = keys;
        }

        public void Register(SyncVarRegistry registry)
        {
            // here, we just need somewhere to track the deltas. we don't know who the deltas are going to yet.
            RegisterSharedVars(registry.SharedBools, _sharedVars.Bools);
            RegisterSharedVars(registry.SharedFloats, _sharedVars.Floats);
            RegisterSharedVars(registry.SharedInts, _sharedVars.Ints);
            RegisterSharedVars(registry.SharedStrings, _sharedVars.Strings);
        }

        private void RegisterSharedVars<T>(SyncVarDictionary<string, SharedVar<T>> varsById, SyncVarDictionary<SyncVarKey, SharedVar<T>> varsByKey)
        {
            foreach (string varId in varsById.GetKeys())
            {
                var var = varsById.GetSyncVar(varId);
                var key = new SyncVarKey(varId, _session.UserId);
                if (!_registeredVars.Add(var))
                {
                    throw new ArgumentException("Tried registering the same shared var with a different id: " + key.SyncedId);
                }

                _keys.RegisterKey(key, var.KeyValidationStatus);
                varsByKey.Register(key, var);
            }
        }
    }
}
