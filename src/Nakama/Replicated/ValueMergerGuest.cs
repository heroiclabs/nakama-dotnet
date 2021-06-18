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
        private OwnedStore _ownedVars;
        private ReplicatedValueStore _remoteVals;
        private IUserPresence _sender;
        private IUserPresence _host;


        internal ValueMergerGuest(IUserPresence sender, IUserPresence host, OwnedStore ownedVars, ReplicatedValueStore remoteVals)
        {
            _sender = sender;
            _host = host;
            _ownedVars = ownedVars;
            _remoteVals = remoteVals;
        }

        public void Merge()
        {
            Merge(_remoteVals.Bools, _ownedVars.Bools, _remoteVals.AddBool);
            Merge(_remoteVals.Floats, _ownedVars.Floats, _remoteVals.AddFloat);
            Merge(_remoteVals.Ints, _ownedVars.Ints, _remoteVals.AddInt);
            Merge(_remoteVals.Strings, _ownedVars.Strings, _remoteVals.AddString);
        }

        private void Merge<T>(
            IEnumerable<ReplicatedValue<T>> remoteValues,
            IReadOnlyDictionary<ReplicatedKey, Owned<T>> ownedVars,
            Action<ReplicatedValue<T>> addValueToSend)
        {
            foreach (ReplicatedValue<T> incomingValue in remoteValues)
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
