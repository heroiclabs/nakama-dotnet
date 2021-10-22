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
using Nakama;

namespace NakamaSync
{
    internal class SelfVarEgress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private PresenceTracker _presenceTracker;
        private HostTracker _hostTracker;
        private SelfVarGuestEgress _selfVarGuestEgress;
        private SelfVarHostEgress _selfHostEgress;

        public SelfVarEgress(SelfVarGuestEgress selfVarGuestEgress, SelfVarHostEgress selfHostEgress, PresenceTracker presenceTracker, HostTracker hostTracker)
        {
            _selfVarGuestEgress = selfVarGuestEgress;
            _selfHostEgress = selfHostEgress;
            _presenceTracker = presenceTracker;
            _hostTracker = hostTracker;
        }

        public void Subscribe(PresenceVarRegistry registry)
        {
            Subscribe(registry.PresenceBools, values => values.PresenceBools);
            Subscribe(registry.PresenceFloats, values => values.PresenceFloats);
            Subscribe(registry.PresenceInts,  values => values.PresenceInts);
            Subscribe(registry.PresenceStrings, values => values.PresenceStrings);
        }

        private void Subscribe<T>(Dictionary<string, PresenceVarCollection<T>> vars, PresenceVarAccessor<T> accessor)
        {
            foreach (var selfVarKvp in vars)
            {
                var selfVarKey = new PresenceVarKey(selfVarKvp.Key, _presenceTracker.UserId);
                var selfVar = selfVarKvp.Value.SelfVar;
                Logger?.DebugFormat($"Subscribing to self variable.");
                selfVarKvp.Value.SelfVar.OnValueChanged += (evt) => HandleLocalSelfVarChanged(selfVarKey, evt, accessor);
            }
        }

        private void HandleLocalSelfVarChanged<T>(PresenceVarKey key, ISelfVarEvent<T> evt, PresenceVarAccessor<T> accessor)
        {
            bool isHost = _hostTracker.IsSelfHost();

            Logger?.DebugFormat($"Local self variable changed. Key: {key}, OldValue: {evt.ValueChange.OldValue}, Value: {evt.ValueChange.NewValue}");

            if (isHost)
            {
                _selfHostEgress.HandleLocalSelfVarChanged(key, evt.Var, evt.ValueChange.NewValue, accessor);
            }
            else
            {
                _selfVarGuestEgress.HandleLocalSelfVarChanged(key, evt.Var, evt.ValueChange.NewValue, accessor);
            }
        }
    }
}
