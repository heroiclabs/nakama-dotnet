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
    internal class HostIngress : IVarIngress
    {
        private SyncSocket _socket;
        private readonly SyncVarKeys _keys;

        public HostIngress(SyncSocket socket, SyncVarKeys keys)
        {
            _socket = socket;
            _keys = keys;
        }

        public void HandleIncomingSharedValue<T>(SharedValue<T> value, SharedVarAccessor<T> accessor, SyncVarDictionary<SyncVarKey, SharedVar<T>> vars, IUserPresence source)
        {
            SharedVar<T> var = vars.GetSyncVar(value.Key);


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
                        var values = new SyncEnvelope();

                        // one guest has incorrect value. queue a rollback for all guests.
                        var outgoing = new SharedValue<T>(value.Key, var.GetValue(), _keys.GetLockVersion(value.Key), KeyValidationStatus.Validated);
                        accessor(values).Add(value);
                        _socket.SendSyncData(source, values);
                    }
                break;
                case KeyValidationStatus.None:
                    var.SetValue(source, value.Value, KeyValidationStatus.None, var.OnRemoteValueChanged);
                break;
            }
        }

        public void HandleIncomingUserValue<T>(UserValue<T> value, UserVarAccessor<T> accessor, SyncVarDictionary<SyncVarKey, UserVar<T>> vars, IUserPresence source)
        {
            IUserPresence target = value.Target;
            UserVar<T> var = vars.GetSyncVar(value.Key);

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
                        var values = new SyncEnvelope();
                        // one guest has incorrect value. queue a rollback for that guest.
                        var outgoing = new UserValue<T>(value.Key, var.GetValue(target), _keys.GetLockVersion(value.Key), KeyValidationStatus.Validated, target);
                        accessor(values).Add(outgoing);
                        _socket.SendSyncData(source, values);
                    }
                break;
                case KeyValidationStatus.None:
                    var.SetValue(value.Value, target, source, KeyValidationStatus.None, var.OnRemoteValueChanged);
                break;
            }
        }
    }
}
