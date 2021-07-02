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
using System.Linq;
using System.Collections.Generic;
using Nakama;

namespace NakamaSync
{
    internal class HostEgress : IVarEgress
    {
        public event Action<IUserPresence, HandshakeResponse> OnHandshakeResponseReady;
        public event Action OnSyncedDataReady;

        private readonly SyncVarKeys _keys;
        private readonly PresenceTracker _presenceTracker;

        public HostEgress(SyncVarKeys keys, PresenceTracker presenceTracker)
        {
            _keys = keys;
            _presenceTracker = presenceTracker;
        }

        public void HandleValidHandshakeRequest(IUserPresence source, HandshakeRequest request, SyncCollections collections)
        {
            CopySharedVarToGuest(collections.SharedBoolCollections, source);
            CopySharedVarToGuest(collections.SharedFloatCollections, source);
            CopySharedVarToGuest(collections.SharedIntCollections, source);
            CopySharedVarToGuest(collections.SharedStringCollections, source);
            CopyUserVarToGuest(collections.UserBoolCollections, source);
            CopyUserVarToGuest(collections.UserFloatCollections, source);
            CopyUserVarToGuest(collections.UserIntCollections, source);
            CopyUserVarToGuest(collections.UserStringCollections, source);
        }

        public void HandleLocalSharedVarChanged<T>(SyncVarKey key, T newValue, SharedVarCollections<T> collections)
        {
            var status = _keys.GetValidationStatus(key);

            if (status == KeyValidationStatus.Pending)
            {
                throw new InvalidOperationException("Host should not have local key pending validation: " + key);
            }

            collections.SharedValuesToAll.Add(new SyncSharedValue<T>(key, newValue, _keys.GetLockVersion(key), status));
        }

        public void HandleLocalUserVarChanged<T>(SyncVarKey key, T newValue, UserVarCollections<T> collections, IUserPresence target)
        {
            var status = _keys.GetValidationStatus(key);

            if (status == KeyValidationStatus.Pending)
            {
                throw new InvalidOperationException("Host should not have local key pending validation: " + key);
            }

            collections.UserValuesToGuest.Add(target.UserId, new SyncUserValue<T>(key, newValue, _keys.GetLockVersion(key), status, target));
        }

        public bool IsValidHandshakeRequest(HandshakeRequest request)
        {
            return request.AllKeys.SequenceEqual(_keys.GetKeys());
        }

        private void CopySharedVarToGuest<T>(SharedVarCollections<T> collections, IUserPresence target)
        {
            foreach (var key in collections.SharedVars.GetKeys())
            {
                SharedVar<T> var = collections.SharedVars.GetSyncVar(key);
                var value = new SyncSharedValue<T>(key, var.GetValue(), _keys.GetLockVersion(key), _keys.GetValidationStatus(key));
                collections.SharedValuesToGuest.Add(target.UserId, value);
            }
        }

        private void CopyUserVarToGuest<T>(UserVarCollections<T> collections, IUserPresence target)
        {
            foreach (var key in collections.UserVars.GetKeys())
            {
                UserVar<T> var = collections.UserVars.GetSyncVar(key);

                foreach (KeyValuePair<string, T> kvp in var.Values)
                {
                    // TODO handle data for a stale user

                    var value = new SyncUserValue<T>(key, var.GetValue(), _keys.GetLockVersion(key), _keys.GetValidationStatus(key), _presenceTracker.GetPresence(kvp.Key));
                    collections.UserValuesToGuest.Add(target.UserId, value);
                }
            }
        }
    }
}
