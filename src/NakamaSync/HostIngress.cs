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
using Nakama;

namespace NakamaSync
{
    internal class HostIngress : IVarIngress
    {
        private readonly SyncVarKeys _keys;

        public HostIngress(SyncVarKeys keys)
        {
            _keys = keys;
        }

        public void HandleIncomingSharedVar<T>(IUserPresence source, SyncSharedValue<T> value, SharedVarCollections<T> collections)
        {
            SharedVar<T> var = collections.SharedVars.GetSyncVar(value.Key);

            // TODO keys lock version checks

            switch (value.KeyValidationStatus)
            {
                case KeyValidationStatus.Validated:
                    throw new InvalidOperationException("Host received value that already claims to be validated.");
                case KeyValidationStatus.Pending:
                    if (var.OnHostValidate(new SharedVarEvent<T>(source, var.GetValue(), value.Value)))
                    {
                        var.SetValue(source, value.Value, KeyValidationStatus.Validated, var.OnRemoteValueChanged);
                    }
                    else
                    {
                        // one guest has incorrect value. queue a rollback for that guest.
                        var outgoing = new SyncSharedValue<T>(value.Key, var.GetValue(), _keys.GetLockVersion(value.Key), KeyValidationStatus.Validated);
                        collections.SharedValuesToAll.Add(outgoing);
                    }
                break;
                case KeyValidationStatus.None:
                    var.SetValue(source, value.Value, KeyValidationStatus.None, var.OnRemoteValueChanged);
                break;
            }
        }

        public void HandleIncomingUserVar<T>(IUserPresence source, SyncUserValue<T> value, UserVarCollections<T> collections)
        {
            IUserPresence target = value.Target;
            UserVar<T> var = collections.UserVars.GetSyncVar(value.Key);

            switch (value.KeyValidationStatus)
            {
                case KeyValidationStatus.Validated:
                    throw new InvalidOperationException("Host received value that already claims to be validated.");
                case KeyValidationStatus.Pending:
                    if (var.OnHostValidate(new UserVarEvent<T>(source, target, var.GetValue(), value.Value)))
                    {
                        var.SetValue(value.Value, source, target, KeyValidationStatus.Validated, var.OnRemoteValueChanged);
                    }
                    else
                    {
                        // one guest has incorrect value. queue a rollback for that guest.
                        var outgoing = new SyncUserValue<T>(value.Key, var.GetValue(target), _keys.GetLockVersion(value.Key), KeyValidationStatus.Validated, target);
                        collections.UserValuesToGuest.Add(source.UserId, outgoing);
                    }
                break;
                case KeyValidationStatus.None:
                    var.SetValue(value.Value, target, source, KeyValidationStatus.None, var.OnRemoteValueChanged);
                break;
            }
        }
    }
}
