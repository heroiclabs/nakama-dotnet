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
using System.Linq;
using Nakama;

namespace NakamaSync
{
    public class SyncedVarRegistration
    {
        internal ISession Session => _session;
        internal PresenceTracker PresenceTracker => _presenceTracker;
        internal GuestHandler GuestHandler => _guestHandler;
        internal HostHandler HostHandler => _hostHandler;

        private readonly HashSet<object> _registeredVars = new HashSet<object>();
        private readonly ISession _session;
        private readonly VarKeys _varKeys = new VarKeys();
        private readonly VarStore _varStore = new VarStore();
        private readonly PresenceTracker _presenceTracker;
        private readonly GuestHandler _guestHandler;
        private readonly HostHandler _hostHandler;

        public SyncedVarRegistration(ISession session)
        {
            _session = session;
            _presenceTracker = new PresenceTracker(session.UserId);
            _guestHandler = new GuestHandler(_presenceTracker, _varStore, _varKeys);
            _hostHandler = new HostHandler(_presenceTracker, _varStore, _varKeys);
        }

        public void RegisterBool(string id, SharedVar<bool> userBool)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userBool, _varStore.SharedBools, store => store.AddBool);
        }

        public void RegisterFloat(string id, SharedVar<float> userFloat)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userFloat, _varStore.SharedFloats, store => store.AddFloat);
        }

        public void RegisterInt(string id, SharedVar<int> userInt)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userInt, _varStore.SharedInts, store => store.AddInt);
        }

        public void RegisterString(string id, SharedVar<string> userString)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userString, _varStore.SharedStrings, store => store.AddString);
        }

        public void RegisterBool(string id, UserVar<bool> userBool)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userBool, _varStore.UserBools, store => store.AddBool);
        }

        public void RegisterFloat(string id, UserVar<float> userFloat)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userFloat, _varStore.UserFloats, store => store.AddFloat);
        }

        public void RegisterInt(string id, UserVar<int> userInt)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userInt, _varStore.UserInts, store => store.AddInt);
        }

        public void RegisterString(string id, UserVar<string> userString)
        {
            var key = new VarKey(id, _session.UserId);
            Register(key, userString, _varStore.UserStrings, store => store.AddString);
        }

        internal List<VarKey> GetAllKeys()
        {
            return _varKeys.GetKeys().ToList();
        }

        private void Register<T>(VarKey key, SharedVar<T> var, IDictionary<VarKey, SharedVar<T>> varStore, Func<SyncVarValues, Action<SyncVarValue<T>>> getAddToQueue)
        {
            if (!_registeredVars.Add(var))
            {
                throw new ArgumentException("Tried registering the same shared var with a different id: " + key.SyncedId);
            }

            _varKeys.RegisterKey(key, var.KeyValidationStatus);
            var.OnLocalValueChanged += (evt) => HandleLocalDataChanged<T>(key, evt, getAddToQueue);

            varStore[key] = var;
        }

        private void Register<T>(VarKey key, UserVar<T> var, IDictionary<VarKey, UserVar<T>> varStore, Func<SyncVarValues, Action<SyncVarValue<T>>> getAddToQueue)
        {
            if (!_registeredVars.Add(var))
            {
                throw new ArgumentException("Tried registering the same user var with a different id: " + key.SyncedId);
            }

            _varKeys.RegisterKey(key, var.KeyValidationStatus);
            varStore[key] = var;
            var.OnLocalValueChanged += (evt) => HandleLocalDataChanged<T>(key, evt, getAddToQueue);
        }

        private void HandleLocalDataChanged<T>(VarKey key, IVarEvent<T> evt, Func<SyncVarValues, Action<SyncVarValue<T>>> getOutgoingQueue)
        {
            if (_varKeys.HasLockVersion(key))
            {
                _varKeys.IncrementLockVersion(key);
            }
            else
            {
                throw new KeyNotFoundException("Tried incrementing lock version for non-existent key: " + key);
            }

            if (_presenceTracker.IsSelfHost())
            {
                _hostHandler.HandleLocalDataChanged<T>(key, evt.NewValue, getOutgoingQueue);
            }
            else
            {
                _guestHandler.HandleLocalDataChanged<T>(key, evt.NewValue, getOutgoingQueue);
            }
        }
    }
}
