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

namespace NakamaSync
{
    public class SyncVarDictionary<TKey, TVar> where TVar : ISyncVar
    {
        private readonly Dictionary<TKey, TVar> _vars = new Dictionary<TKey, TVar>();

        public TVar GetSyncVar(TKey key)
        {
            if (!_vars.ContainsKey(key))
            {
                throw new ArgumentException("Could not find var with key " + key);
            }

            return _vars[key];
        }

        public void Register(TKey key, TVar var)
        {
            if (_vars.ContainsKey(key))
            {
                throw new ArgumentException("Tried registering a duplicate sync var " + key);
            }
            else
            {
                _vars.Add(key, var);
            }
        }

        public void ResetVars()
        {
            foreach (TVar var in _vars.Values)
            {
                var.Reset();
            }
        }

        public IEnumerable<TKey> GetKeys()
        {
            return _vars.Keys;
        }
    }
}
