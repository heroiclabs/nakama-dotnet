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

namespace Nakama.Replicated
{
    internal class ValueMergerHost
    {
        private ReplicatedValueStore _outgoingVals;
        private Store _ownedVars;
        private readonly PresenceTracker _presenceTracker;
        private ReplicatedValueStore _remoteVals;
        private IUserPresence _source;

        internal ValueMergerHost(
            PresenceTracker presenceTracker,
            IUserPresence source,
            Store ownedVars,
            ReplicatedValueStore remoteVals,
            ReplicatedValueStore responseVals)
        {
            _source = source;
            _ownedVars = ownedVars;
            _remoteVals = remoteVals;
            _outgoingVals = responseVals;
        }

        public void Merge()
        {
            Merge(_ownedVars.Bools, _remoteVals.Bools, _remoteVals.AddBool);
            Merge(_ownedVars.Floats, _remoteVals.Floats, _remoteVals.AddFloat);
            Merge(_ownedVars.Ints, _remoteVals.Ints, _remoteVals.AddInt);
            Merge(_ownedVars.Strings, _remoteVals.Strings, _remoteVals.AddString);
        }

        private void Merge<T>(
            IReadOnlyDictionary<ReplicatedKey, Owned<T>> ownedVars,
            IEnumerable<ReplicatedValue<T>> incomingValues,
            Action<ReplicatedValue<T>> addValueToSend)
        {
            foreach (ReplicatedValue<T> incomingValue in incomingValues)
            {
                T remoteValue = incomingValue.Value;

                if (!_ownedVars.HasLockVersion(incomingValue.Key))
                {
                    throw new ArgumentException($"Received unrecognized remote key: {incomingValue.Key}");
                }

                // todo one client updated locally while another value was in flight
                // how to handle? think about 2x2 host guest combos
                // also if values are equal it doesn't matter.
                if (incomingValue.LockVersion == _ownedVars.GetLockVersion(incomingValue.Key))
                {
                    throw new ArgumentException($"Received conflicting remote key: {incomingValue.Key}");
                }

                Owned<T> localType = ownedVars[incomingValue.Key];

                if (incomingValue.LockVersion < _ownedVars.GetLockVersion(incomingValue.Key))
                {
                    // stale data because this client updated the value
                    // before receiving.
                    continue;
                }

                IUserPresence target;

                switch (incomingValue.KeyValidationStatus)
                {
                    case KeyValidationStatus.Validated:
                        throw new InvalidOperationException("Host received value that already claims to be validated.");
                    case KeyValidationStatus.Pending:
                        target = _presenceTracker.GetPresence(incomingValue.Key.UserId);
                        if (localType.OnHostValidate(localType.GetValue(), remoteValue, _source, target))
                        {
                            localType.SetValue(remoteValue, _source, target, KeyValidationStatus.Validated);
                        }
                        else
                        {
                            // one guest has incorrect value. queue a rollback for that guest.
                            var outgoing = new ReplicatedValue<T>(incomingValue.Key, localType.GetValue(_source), _ownedVars.GetLockVersion(incomingValue.Key), KeyValidationStatus.Validated, _source);
                            addValueToSend(outgoing);
                        }
                    break;
                    case KeyValidationStatus.None:
                         target = _presenceTracker.GetPresence(incomingValue.Key.UserId);

                        localType.SetValue(remoteValue, target, _source, KeyValidationStatus.None);
                    break;
                }
            }
        }
    }
}
