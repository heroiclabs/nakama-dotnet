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

using System.Collections.Generic;
using Nakama;

namespace NakamaSync
{
    internal class VarIngress<T>
    {
        private readonly VarRegistry<T> _registry;
        private readonly LockVersionGuard _lockVersionGuard;
        private readonly HostTracker _hostTracker;

        public VarIngress(VarRegistry<T> registry, LockVersionGuard lockVersionGuard, HostTracker hostTracker)
        {
            _registry = registry;
            _lockVersionGuard = lockVersionGuard;
            _hostTracker = hostTracker;
        }

        public void Subscribe(SyncSocket<T> socket)
        {
            socket.OnSyncEnvelope += (source, envelope) =>
            {
                ReceiveSyncEnvelope(source, envelope, socket);
            };
        }

        public void ReceiveSyncEnvelope(IUserPresence source, Envelope<T> incomingEnvelope, SyncSocket<T> socket)
        {
            var outgoingEnvelope = new Envelope<T>();

            HandleSyncEnvelope(source, _registry, incomingEnvelope, outgoingEnvelope);

            if (outgoingEnvelope.GetAllValues().Count > 0)
            {
                socket.SendSyncDataToAll(outgoingEnvelope);
            }
        }

        private void HandleSyncEnvelope(IUserPresence source, VarRegistry<T> registry, Envelope<T> incomingValues, Envelope<T> outgoingValues)
        {
            foreach (SharedVarValue<T> value in incomingValues.SharedValues)
            {
                if (!_lockVersionGuard.IsValidLockVersion(value.Key, value.LockVersion))
                {
                    // expected race to occur
                    continue;
                }

                if (!registry.ContainsSharedKey(value.Key))
                {
                    // not expected
                    throw new KeyNotFoundException($"Could not find var with key: {value.Key}");
                }

                var var = registry.GetSharedVar(value.Key);

                HandleSyncValue(source, var, value, outgoingValues.SharedValues);
            }

            foreach (PresenceVarValue<T> value in incomingValues.PresenceValues)
            {
                if (!registry.ContainsPresenceKey(value.Key))
                {
                    // not expected
                    throw new KeyNotFoundException($"Could not find var with key: {value.Key}");
                }

                foreach (var var in registry.GetPresenceVars(value.Key))
                {
                    HandleSyncValue(source, var, value, outgoingValues.PresenceValues);
                }
            }
        }

        private void HandleSyncValue(IUserPresence source, Var<T> var, SharedVarValue<T> value, List<SharedVarValue<T>> responses)
        {
            // response from host, treat as authoritative
            if (value.IsAck)
            {
                var.SetValidationStatus(value.ValidationStatus);
                return;
            }

            var.SetValue(source, value.Value);

            if (_hostTracker.IsSelfHost())
            {
                responses.Add(new SharedVarValue<T>(value.Key, value.Value, value.LockVersion, var.ValidationStatus, isAck: true));
            }
        }

        private void HandleSyncValue(IUserPresence source, Var<T> var, PresenceVarValue<T> value, List<PresenceVarValue<T>> responses)
        {
            if (value.IsAck)
            {
                var.ValidationStatus = value.ValidationStatus;
                return;
            }

            var.SetValue(source, value.Value);

            if (_hostTracker.IsSelfHost())
            {
                responses.Add(new PresenceVarValue<T>(value.Key, value.Value, value.LockVersion, var.ValidationStatus, isAck: true, value.UserId));
            }
        }
    }
}
