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
internal class ValueMergerGuest
{
        private ReplicatedVarStore _localVars;
        private ReplicatedValueStore _remoteVals;
        private IUserPresence _sender;
        private IUserPresence _host;


        internal ValueMergerGuest(IUserPresence sender, IUserPresence host, ReplicatedVarStore localVars, ReplicatedValueStore remoteVals)
        {
            _sender = sender;
            _host = host;
            _localVars = localVars;
            _remoteVals = remoteVals;
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
            foreach (ReplicatedValue<T> incomingValue in remoteValues)
            {
                T remoteValue = incomingValue.Value;

                if (!_localVars.HasLockVersion(incomingValue.Key))
                {
                    throw new ArgumentException($"Received unrecognized remote key: {incomingValue.Key}");
                }

                // todo one client updated locally while another value was in flight
                // how to handle? think about 2x2 host guest combos
                // also if values are equal it doesn't matter.
                if (incomingValue.LockVersion == _localVars.GetLockVersion(incomingValue.Key))
                {
                    throw new ArgumentException($"Received conflicting remote key: {incomingValue.Key}");
                }

                ReplicatedVar<T> localType = localVars[incomingValue.Key];

                if (incomingValue.LockVersion < _localVars.GetLockVersion(incomingValue.Key))
                {
                    // host can roll back the guest's value and lock version
                    if (_sender.UserId != _host.UserId ||
                        incomingValue.KeyValidationStatus != KeyValidationStatus.Validated)
                    {
                        // stale data because this client updated the value
                        // before receiving.
                        continue;
                    }
                }

                switch (incomingValue.KeyValidationStatus)
                {
                    case KeyValidationStatus.Pending:
                        throw new InvalidOperationException("Guest received value pending validation.");
                    case KeyValidationStatus.Validated:
                        localType.SetValue(_sender, remoteValue, ReplicatedClientType.Remote, KeyValidationStatus.Validated);
                    break;
                    case KeyValidationStatus.None:
                        localType.SetValue(_sender, remoteValue, ReplicatedClientType.Remote, KeyValidationStatus.None);
                    break;
                }
            }
        }
    }
}
