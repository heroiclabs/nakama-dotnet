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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Nakama;

namespace NakamaSync
{
    internal class HostHandler
    {
        public event Action<IUserPresence, HandshakeResponse> OnHandshakeResponseSend;
        public event Action<IEnumerable<IUserPresence>, SyncVarValues> OnReplicatedDataSend;

        private PresenceTracker _presenceTracker;
        public IUserPresence Presence => _presenceTracker.GetHost();

        private readonly ConcurrentDictionary<string, IUserPresence> _guests = new ConcurrentDictionary<string, IUserPresence>();
        private readonly SyncVarValues _valuesToAll = new SyncVarValues();
        private readonly ConcurrentDictionary<string, SyncVarValues> _valuesToGuest = new ConcurrentDictionary<string, SyncVarValues>();
        private readonly VarKeys _varKeys;
        private readonly VarStore _store;

        public HostHandler(PresenceTracker presenceTracker, VarStore stores, VarKeys varKeys)
        {
            _presenceTracker = presenceTracker;
            _store = stores;
            _varKeys = varKeys;
        }

        public void ReceivedHandshakeRequest(IUserPresence requester, HandshakeRequest request)
        {
            HandshakeResponse response;

            HashSet<VarKey> localKeys = _varKeys.GetKeys();

            bool success = localKeys.All(request.AllKeys.Contains);

            SyncVarValues outgoingValues = null;

            if (success)
            {
                // user may have joined mid-match. send data for them to sync.
                // todo we don't send any pending values in the var store.
                // that is perhaps an optimization that can be made later.
                outgoingValues = new SyncVarValues();

                foreach (KeyValuePair<VarKey, UserVar<bool>> kvp in _store.UserBools)
                {
                    outgoingValues.AddBool(UserVarToValue(kvp));
                }

                foreach (KeyValuePair<VarKey, UserVar<float>> kvp in _store.UserFloats)
                {
                    outgoingValues.AddFloat(UserVarToValue(kvp));
                }

                foreach (KeyValuePair<VarKey, UserVar<int>> kvp in _store.UserInts)
                {
                    outgoingValues.AddInt(UserVarToValue(kvp));
                }

                foreach (KeyValuePair<VarKey, UserVar<string>> kvp in _store.UserStrings)
                {
                    outgoingValues.AddString(UserVarToValue(kvp));
                }
            }

            response = new HandshakeResponse(outgoingValues, success);

            if (OnHandshakeResponseSend != null)
            {
                OnHandshakeResponseSend(requester, response);
            }
        }

        public void HandleRemoteDataChanged(IUserPresence source, SyncVarValues remoteVals)
        {
            // prepare data to send back to user for data that requires host validation.
            if (!_valuesToGuest.ContainsKey(source.UserId))
            {
                _valuesToGuest[source.UserId] = new SyncVarValues();
            }

            Merge(source, _store.UserBools, remoteVals.Bools, remoteVals.AddBool);
            Merge(source, _store.UserFloats, remoteVals.Floats, remoteVals.AddFloat);
            Merge(source, _store.UserInts, remoteVals.Ints, remoteVals.AddInt);
            Merge(source, _store.UserStrings, remoteVals.Strings, remoteVals.AddString);

            OnReplicatedDataSend(new IUserPresence[]{source}, _valuesToGuest[source.UserId]);
            OnReplicatedDataSend(_guests.Select(kvp => kvp.Value), _valuesToAll);
        }

        private SyncVarValue<T> UserVarToValue<T>(KeyValuePair<VarKey, UserVar<T>> kvp)
        {
            return new SyncVarValue<T>(kvp.Key, kvp.Value.GetValue(Presence), _varKeys.GetLockVersion(kvp.Key), kvp.Value.KeyValidationStatus, Presence);
        }

        public void HandleLocalDataChanged<T>(VarKey key, T newValue, Func<SyncVarValues, Action<SyncVarValue<T>>> getAddToQueue)
        {
            var status = _varKeys.GetValidationStatus(key);

            if (status == KeyValidationStatus.Pending)
            {
                throw new InvalidOperationException("Host should not have local key pending validation: " + key);
            }

            var addToQueue = getAddToQueue(_valuesToAll);
            addToQueue(new SyncVarValue<T>(key, newValue, _varKeys.GetLockVersion(key), status, Presence));
        }

        private void Merge<T>(
            IUserPresence source,
            IReadOnlyDictionary<VarKey, UserVar<T>> userVars,
            IEnumerable<SyncVarValue<T>> incomingValues,
            Action<SyncVarValue<T>> addValueToSend)
        {
            foreach (SyncVarValue<T> incomingValue in incomingValues)
            {
                Merge(source, userVars, incomingValue, addValueToSend);
            }
        }

        private void Merge<T>(IUserPresence source, IReadOnlyDictionary<VarKey, UserVar<T>> userVars, SyncVarValue<T> incomingValue, Action<SyncVarValue<T>> addValueToSend)
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
                // stale data because this client updated the value
                // before receiving.
                return;
            }

            IUserPresence target = _presenceTracker.GetPresence(incomingValue.Key.UserId);

            switch (incomingValue.KeyValidationStatus)
            {
                case KeyValidationStatus.Validated:
                    throw new InvalidOperationException("Host received value that already claims to be validated.");
                case KeyValidationStatus.Pending:
                    if (localType.OnHostValidate(new UserVarEvent<T>(source, target, localType.GetValue(), remoteValue)))
                    {
                        localType.SetValue(remoteValue, source, target, KeyValidationStatus.Validated, localType.OnRemoteValueChanged);
                    }
                    else
                    {
                        // one guest has incorrect value. queue a rollback for that guest.
                        var outgoing = new SyncVarValue<T>(incomingValue.Key, localType.GetValue(source), _varKeys.GetLockVersion(incomingValue.Key), KeyValidationStatus.Validated, source);
                        addValueToSend(outgoing);
                    }
                break;
                case KeyValidationStatus.None:
                    localType.SetValue(remoteValue, target, source, KeyValidationStatus.None, localType.OnRemoteValueChanged);
                break;
            }
        }
    }
}
