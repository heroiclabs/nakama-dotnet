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
using Nakama;

namespace NakamaSync
{
    internal class GuestIngress : IVarIngress
    {
        private SyncVarKeys _keys;
        private PresenceTracker _presenceTracker;

        public GuestIngress(SyncVarKeys keys, PresenceTracker presenceTracker)
        {
            _presenceTracker = presenceTracker;
            _keys = keys;
        }

        public void HandleIncomingUserVar<T>(IUserPresence source, SyncUserValue<T> incomingValue, UserVarCollections<T> collections)
        {
            T remoteValue = incomingValue.Value;

            if (!_keys.HasLockVersion(incomingValue.Key))
            {
                throw new ArgumentException($"Received unrecognized remote key: {incomingValue.Key}");
            }

            // todo one client updated locally while another value was in flight
            // how to handle? think about 2x2 host guest combos
            // also if values are equal it doesn't matter.
            if (incomingValue.LockVersion == _keys.GetLockVersion(incomingValue.Key))
            {
                throw new ArgumentException($"Received conflicting remote key: {incomingValue.Key}");
            }

            UserVar<T> localType = collections.UserVars.GetSyncVar(incomingValue.Key);

            if (incomingValue.LockVersion < _keys.GetLockVersion(incomingValue.Key))
            {
                // host can roll back the guest's value and lock version
                if (source.UserId != _presenceTracker.GetHost().UserId ||
                    incomingValue.KeyValidationStatus != KeyValidationStatus.Validated)
                {
                    // stale data because this client updated the value
                    // before receiving.
                    return;
                }
            }

            if (incomingValue.KeyValidationStatus == KeyValidationStatus.Pending)
            {
                // TODO
                // throw new InvalidOperationException("Guest received value pending validation.");
                return;
            }

            IUserPresence target = _presenceTracker.GetPresence(incomingValue.Key.UserId);
            localType.SetValue(remoteValue, source, target, KeyValidationStatus.None, localType.OnRemoteValueChanged);
        }

        public void HandleIncomingSharedVar<T>(IUserPresence source, SyncSharedValue<T> incomingValue, SharedVarCollections<T> collections)
        {
            T remoteValue = incomingValue.Value;

            if (!_keys.HasLockVersion(incomingValue.Key))
            {
                throw new ArgumentException($"Received unrecognized remote key: {incomingValue.Key}");
            }

            // todo one client updated locally while another value was in flight
            // how to handle? think about 2x2 host guest combos
            // also if values are equal it doesn't matter.
            if (incomingValue.LockVersion == _keys.GetLockVersion(incomingValue.Key))
            {
                throw new ArgumentException($"Received conflicting remote key: {incomingValue.Key}");
            }

            SharedVar<T> localType = collections.SharedVars.GetSyncVar(incomingValue.Key);

            if (incomingValue.LockVersion < _keys.GetLockVersion(incomingValue.Key))
            {
                // host can roll back the guest's value and lock version
                if (source.UserId != _presenceTracker.GetHost().UserId ||
                    incomingValue.KeyValidationStatus != KeyValidationStatus.Validated)
                {
                    // stale data because this client updated the value
                    // before receiving.
                    return;
                }
            }

            if (incomingValue.KeyValidationStatus == KeyValidationStatus.Pending)
            {
                // TODO
                // throw new InvalidOperationException("Guest received value pending validation.");
                return;
            }

            IUserPresence target = _presenceTracker.GetPresence(incomingValue.Key.UserId);
            localType.SetValue(source, remoteValue, KeyValidationStatus.None, localType.OnRemoteValueChanged);
        }
    }
}