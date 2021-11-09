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

namespace NakamaSync
{
    internal class VarEgress<T>
    {
        private PresenceTracker _presenceTracker;
        private HostTracker _hostTracker;
        private LockVersionGuard _lockVersionGuard;
        private SyncSocket<T> _syncSocket;

        public VarEgress(LockVersionGuard lockVersionGuard, PresenceTracker presenceTracker, HostTracker hostTracker, SyncSocket<T> socket)
        {
            _lockVersionGuard = lockVersionGuard;
            _presenceTracker = presenceTracker;
            _hostTracker = hostTracker;
            _syncSocket = socket;
        }

        public void Subscribe(VarRegistry<T> registry)
        {
            foreach (var var in registry.SharedVars)
            {
                var.OnValueChanged += (evt) => HandleValueChange(var.Key, var, evt);
            }

            foreach (var var in registry.SelfVars)
            {
                var.OnValueChanged += (evt) => HandleValueChange(var.Key, var, evt);
            }
        }

        private void HandleValueChange(string key, SharedVar<T> var, IVarEvent<T> evt)
        {
            if (evt.Source.UserId != _presenceTracker.GetSelf().UserId)
            {
                // not set by this user.
                return;
            }

            var envelope = new Envelope<T>();
            var newStatus = SetNewStatus(var);
            var newValue = new SharedVarValue<T>(key, evt.ValueChange.NewValue, _lockVersionGuard.GetLockVersion(key), newStatus, isAck: false);
            _lockVersionGuard.IncrementLockVersion(key);
            envelope.SharedValues.Add(newValue);
            _syncSocket.SendSyncDataToAll(envelope);
        }

        private void HandleValueChange(string key, SelfVar<T> var, IVarEvent<T> evt)
        {
            var envelope = new Envelope<T>();
            var newStatus = SetNewStatus(var);
            var newValue = new PresenceVarValue<T>(key, evt.ValueChange.NewValue, _lockVersionGuard.GetLockVersion(key), newStatus, isAck: false, _presenceTracker.GetSelf().UserId);
            _lockVersionGuard.IncrementLockVersion(key);
            envelope.PresenceValues.Add(newValue);
            _syncSocket.SendSyncDataToAll(envelope);
        }

        private ValidationStatus SetNewStatus(Var<T> var)
        {
           bool isHost = _hostTracker.IsSelfHost();

            ValidationStatus status = var.Status;

            if (!isHost)
            {
                if (status == ValidationStatus.Valid)
                {
                    status = ValidationStatus.Pending;
                    var.ValidationStatus = status;
                }
            }

            return status;
        }
    }
}
