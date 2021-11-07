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
using System.Linq;
using Nakama;

namespace NakamaSync
{

    internal delegate List<SharedVarValue<T>> ValuesAccessor<T>(Envelope env);
    internal delegate List<PresenceVarValue<T>> SelfValuesAccessor<T>(Envelope env);

    internal class VarEgress : ISyncService
    {

        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private PresenceTracker _presenceTracker;
        private HostTracker _hostTracker;
        private LockVersionGuard _lockVersionGuard;
        private SyncSocket _syncSocket;

        public VarEgress(LockVersionGuard lockVersionGuard, PresenceTracker presenceTracker, HostTracker hostTracker, SyncSocket socket)
        {
            _lockVersionGuard = lockVersionGuard;
            _presenceTracker = presenceTracker;
            _hostTracker = hostTracker;
            _syncSocket = socket;
        }

        public void Subscribe(VarRegistry registry)
        {
            Subscribe(registry.Bools, env => env.Bools);
            Subscribe(registry.Floats, env => env.Floats);
            Subscribe(registry.Ints,  env => env.Ints);
            Subscribe(registry.Strings, env => env.Strings);
            Subscribe(registry.Objects, env => env.Objects);
            Subscribe(registry.SelfBools, env => env.PresenceBools);
            Subscribe(registry.SelfFloats, env => env.PresenceFloats);
            Subscribe(registry.SelfInts,  env => env.PresenceInts);
            Subscribe(registry.SelfStrings, env => env.PresenceStrings);
            Subscribe(registry.SelfObjects, env => env.PresenceObjects);
        }

        private void Subscribe<T>(Dictionary<string, IVar<T>> vars, ValuesAccessor<T> accessor)
        {
            var flattenedVars = vars.Values;
            foreach (var var in flattenedVars)
            {
                var.OnValueChanged += (evt) => HandleValueChange(var.Key, var, evt, accessor);
            }
        }

        private void Subscribe<T>(Dictionary<string, SelfVar<T>> vars, SelfValuesAccessor<T> accessor)
        {
            var flattenedVars = vars.Values;
            foreach (var var in flattenedVars)
            {
                var.OnValueChanged += (evt) => HandleValueChange(var.Key, var, evt, accessor);
            }
        }

        private void HandleValueChange<T>(string key, IVar<T> var, IVarEvent<T> evt, ValuesAccessor<T> accessor)
        {
            if (evt.Source.UserId != _presenceTracker.GetSelf().UserId)
            {
                // not set by this user.
                return;
            }

            var envelope = new Envelope();
            var newStatus = SetNewStatus(var);
            var newValue = new SharedVarValue<T>(key, evt.ValueChange.NewValue, _lockVersionGuard.GetLockVersion(key), newStatus, isAck: false);
            _lockVersionGuard.IncrementLockVersion(key);
            accessor(envelope).Add(newValue);
            _syncSocket.SendSyncDataToAll(envelope);
        }

        private void HandleValueChange<T>(string key, SelfVar<T> var, IVarEvent<T> evt, SelfValuesAccessor<T> accessor)
        {
            var envelope = new Envelope();
            var newStatus = SetNewStatus(var);
            var newValue = new PresenceVarValue<T>(key, evt.ValueChange.NewValue, _lockVersionGuard.GetLockVersion(key), newStatus, isAck: false, _presenceTracker.GetSelf().UserId);
            _lockVersionGuard.IncrementLockVersion(key);
            accessor(envelope).Add(newValue);
        }

        private ValidationStatus SetNewStatus<T>(IVar<T> var)
        {
           bool isHost = _hostTracker.IsSelfHost();

            ValidationStatus status = var.ValidationStatus;

            if (!isHost)
            {
                if (status == ValidationStatus.Valid)
                {
                    status = ValidationStatus.Pending;
                    (var as IVar).SetValidationStatus(status);
                }
            }

            return status;
        }
    }
}
