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
    internal class GuestHandler
    {
        public event Action<IEnumerable<IUserPresence>, SyncVarValues> OnReplicatedDataSend;

        private readonly VarKeys _varKeys;
        private readonly PresenceTracker _presenceTracker;
        private readonly VarStore _stores;
        private readonly SyncVarValues _valuesToHost = new SyncVarValues();
        private readonly SyncVarValues _valuesToAll = new SyncVarValues();

        public GuestHandler(PresenceTracker presenceTracker, VarStore stores, VarKeys varKeys)
        {
            _presenceTracker = presenceTracker;
            _stores = stores;
            _varKeys = varKeys;
        }

        public void ReceivedHandshakeResponse(HandshakeResponse response)
        {
            if (response.Success)
            {
                Merge(_presenceTracker.GetHost(), _stores.UserBools, response.Store.Bools);
                Merge(_presenceTracker.GetHost(), _stores.UserFloats, response.Store.Floats);
                Merge(_presenceTracker.GetHost(), _stores.UserInts, response.Store.Ints);
                Merge(_presenceTracker.GetHost(), _stores.UserStrings, response.Store.Strings);
            }
            else
            {
                throw new Exception("Host rejected client due to mismatched app binaries.");
            }
        }

        public void HandleLocalDataChanged<T>(VarKey key, T newValue, Func<SyncVarValues, Action<SyncVarValue<T>>> getAddToQueue)
        {
            var status = _varKeys.GetValidationStatus(key);

            SyncVarValues outgoingValues = status == KeyValidationStatus.None ? _valuesToAll : _valuesToHost;

            if (status == KeyValidationStatus.Validated)
            {
                status = KeyValidationStatus.Pending;
                _varKeys.SetValidationStatus(key, status);
            }

            var newSyncedValue = new SyncVarValue<T>(key, newValue, _varKeys.GetLockVersion(key), status, _presenceTracker.GetSelf());

            if (status == KeyValidationStatus.Pending)
            {
                getAddToQueue(_valuesToHost)(newSyncedValue);
            }
            else
            {
                getAddToQueue(_valuesToAll)(newSyncedValue);
            }
        }

        public void HandleRemoteDataChanged(IUserPresence sender, SyncVarValues remoteVals)
        {
            Merge(sender, _stores.UserBools, remoteVals.Bools);
            Merge(sender, _stores.UserFloats, remoteVals.Floats);
            Merge(sender, _stores.UserInts, remoteVals.Ints);
            Merge(sender, _stores.UserStrings, remoteVals.Strings);
        }

        private void Merge<T>(
            IUserPresence source,
            IReadOnlyDictionary<VarKey, UserVar<T>> userVars,
            IEnumerable<SyncVarValue<T>> remoteValues)
        {
            foreach (SyncVarValue<T> incomingValue in remoteValues)
            {
                Merge(source, userVars, remoteValues);
            }
        }

        private void Merge<T>(IUserPresence source, IReadOnlyDictionary<VarKey, UserVar<T>> userVars, SyncVarValue<T> incomingValue)
        {
            T remoteValue = incomingValue.Value;

            if (!_varKeys.HasLockVersion(incomingValue.Key))
            {
                throw new ArgumentException($"Received unrecognized remote key: {incomingValue.Key}");
            }

            // todo one client updated locally while another value was in flight
            // how to handle? think about 2x2 host guest combos
            // also if values are equal it doesn't matter.
            if (incomingValue.LockVersion == _varKeys.GetLockVersion(incomingValue.Key))
            {
                throw new ArgumentException($"Received conflicting remote key: {incomingValue.Key}");
            }

            UserVar<T> localType = userVars[incomingValue.Key];

            if (incomingValue.LockVersion < _varKeys.GetLockVersion(incomingValue.Key))
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
    }
}
