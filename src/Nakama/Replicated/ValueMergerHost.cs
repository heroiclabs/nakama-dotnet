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
        private IUserPresence _sender;
        private ReplicatedVarStore _localVars;
        private ReplicatedValueStore _remoteVals;
        private ReplicatedValueStore _outgoingVals;

        internal ValueMergerHost(
            IUserPresence sender, ReplicatedVarStore localVars, ReplicatedValueStore remoteVals, ReplicatedValueStore responseVals)
        {
            _sender = sender;
            _localVars = localVars;
            _remoteVals = remoteVals;
            _outgoingVals = responseVals;
        }

        public void Merge()
        {
            Merge(_remoteVals.Bools, _localVars.Bools, _remoteVals.AddBool);
            Merge(_remoteVals.Floats, _localVars.Floats, _remoteVals.AddFloat);
            Merge(_remoteVals.Ints, _localVars.Ints, _remoteVals.AddInt);
            Merge(_remoteVals.Strings, _localVars.Strings, _remoteVals.AddString);
        }

        private void Merge<T>(
            IEnumerable<ReplicatedValue<T>> remoteValues,
            IReadOnlyDictionary<ReplicatedKey, ReplicatedVar<T>> localVars,
            Action<ReplicatedValue<T>> addValueToSend)
        {
            foreach (ReplicatedValue<T> remoteReplicatedValue in remoteValues)
            {
                T remoteValue = remoteReplicatedValue.Value;

                if (!_localVars.HasLockVersion(remoteReplicatedValue.Key))
                {
                    throw new ArgumentException($"Received unrecognized remote key: {remoteReplicatedValue.Key}");
                }

                // todo one client updated locally while another value was in flight
                // how to handle? think about 2x2 host guest combos
                // also if values are equal it doesn't matter.
                if (remoteReplicatedValue.LockVersion == _localVars.GetLockVersion(remoteReplicatedValue.Key))
                {
                    throw new ArgumentException($"Received conflicting remote key: {remoteReplicatedValue.Key}");
                }

                ReplicatedVar<T> localType = localVars[remoteReplicatedValue.Key];

                if (remoteReplicatedValue.LockVersion < _localVars.GetLockVersion(remoteReplicatedValue.Key))
                {
                    // stale data because this client updated the value
                    // before receiving.
                    continue;
                }

                switch (remoteReplicatedValue.KeyValidationStatus)
                {
                    case KeyValidationStatus.Validated:
                        throw new InvalidOperationException("Host received value that already claims to be validated.");
                    case KeyValidationStatus.Pending:
                        if (localType.OnHostValidate(localType.GetValue(_sender), remoteValue))
                        {
                            localType.SetValue(_sender, remoteValue, ReplicatedClientType.Remote, KeyValidationStatus.Validated);
                        }
                        else
                        {
                            // one guest has incorrect value. queue a rollback for that guest.
                            var outgoing = new ReplicatedValue<T>(remoteReplicatedValue.Key, localType.GetValue(_sender), _localVars.GetLockVersion(remoteReplicatedValue.Key), KeyValidationStatus.Validated, _sender);
                            addValueToSend(outgoing);
                        }
                    break;
                    case KeyValidationStatus.None:
                        localType.SetValue(_sender, remoteValue, ReplicatedClientType.Remote, KeyValidationStatus.None);
                    break;
                }
            }
        }
    }
}
