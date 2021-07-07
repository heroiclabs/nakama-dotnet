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
    internal class UserVarRegistry
    {
        public UserVars UserVars => _userVars;

        private ISession _session;
        private readonly SyncVarKeys _keys;
        private readonly UserVars _userVars;

        private readonly HashSet<object> _registeredVars = new HashSet<object>();

        public UserVarRegistry(ISession session, UserVars userVars, SyncVarKeys keys)
        {
            _session = session;
            _keys = keys;
            _userVars = userVars;
        }

        public void Register(SyncVarRegistry registry)
        {
            RegisterUserVars(registry.UserBools, _userVars.Bools);
            RegisterUserVars(registry.UserFloats, _userVars.Floats);
            RegisterUserVars(registry.UserInts, _userVars.Ints);
            RegisterUserVars(registry.UserStrings, _userVars.Strings);
        }

        private void RegisterUserVars<T>(SyncVarDictionary<string, UserVar<T>> varsById,
            SyncVarDictionary<SyncVarKey, UserVar<T>> varsByKey)
        {
            foreach (string varId in varsById.GetKeys())
            {
                var var = varsById.GetSyncVar(varId);
                var key = new SyncVarKey(varId, _session.UserId);

                if (!_registeredVars.Add(var))
                {
                    throw new ArgumentException("Tried registering the same user var with a different id: " + key.SyncedId);
                }

                _keys.RegisterKey(key, var.KeyValidationStatus);
                varsByKey.Register(key, var);
            }
        }
    }
}
