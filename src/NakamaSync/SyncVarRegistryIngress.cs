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
    internal class SyncVarRegistryIngress
    {
        private ISession _session;
        private readonly SyncVarKeys _keys;
        private readonly SyncCollections _collectionss;
        private SyncVarRouter _router;
        private readonly HashSet<object> _registeredVars = new HashSet<object>();

        public SyncVarRegistryIngress(ISession session, SyncVarKeys keys, SyncCollections collectionss, SyncVarRouter router)
        {
            _session = session;
            _keys = keys;
            _collectionss = collectionss;
            _router = router;
        }

        public void Register(SyncVarRegistry registry)
        {
            RegisterSharedVars(registry.SharedBools, _collectionss.SharedBoolCollections);
            RegisterSharedVars(registry.SharedFloats, _collectionss.SharedFloatCollections);
            RegisterSharedVars(registry.SharedInts, _collectionss.SharedIntCollections);
            RegisterSharedVars(registry.SharedStrings, _collectionss.SharedStringCollections);

            RegisterUserVars(registry.UserBools, _collectionss.UserBoolCollections);
            RegisterUserVars(registry.UserFloats, _collectionss.UserFloatCollections);
            RegisterUserVars(registry.UserInts, _collectionss.UserIntCollections);
            RegisterUserVars(registry.UserStrings, _collectionss.UserStringCollections);
        }

        private void RegisterSharedVars<T>(SyncVarDictionary<string, SharedVar<T>> dict, SharedVarCollections<T> collections)
        {
            foreach (string key in dict.GetKeys())
            {
                RegisterSharedVar(new SyncVarKey(key, _session.UserId), dict.GetSyncVar(key), collections);
            }
        }

        private void RegisterUserVars<T>(SyncVarDictionary<string, UserVar<T>> dict, UserVarCollections<T> collections)
        {
            foreach (string key in dict.GetKeys())
            {
                RegisterUserVar(new SyncVarKey(key, _session.UserId), dict.GetSyncVar(key), collections);
            }
        }

        private void RegisterSharedVar<T>(SyncVarKey key, SharedVar<T> var, SharedVarCollections<T> collections)
        {
            if (!_registeredVars.Add(var))
            {
                throw new ArgumentException("Tried registering the same shared var with a different id: " + key.SyncedId);
            }

            _keys.RegisterKey(key, var.KeyValidationStatus);
            collections.SharedVars.Register(key, var);
            var.OnLocalValueChanged += (evt) => _router.HandleLocalSharedVarChanged<T>(key, evt, collections);
        }

        private void RegisterUserVar<T>(SyncVarKey key, UserVar<T> var, UserVarCollections<T> collections)
        {
            if (!_registeredVars.Add(var))
            {
                throw new ArgumentException("Tried registering the same user var with a different id: " + key.SyncedId);
            }

            _keys.RegisterKey(key, var.KeyValidationStatus);
            collections.UserVars.Register(key, var);
            var.OnLocalValueChanged += (evt) => _router.HandleLocalUserVarChanged<T>(key, evt, collections);
        }
    }
}
