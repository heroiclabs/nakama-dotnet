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
using System.Linq;
using Nakama;

namespace NakamaSync
{
    internal class VarEgress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private PresenceTracker _presenceTracker;
        private HostTracker _hostTracker;
        private VarGuestEgress _guestEgress;
        private VarHostEgress _hostEgress;

        public VarEgress(VarGuestEgress guestEgress, VarHostEgress hostEgress, PresenceTracker presenceTracker, HostTracker hostTracker)
        {
            _guestEgress = guestEgress;
            _hostEgress = hostEgress;
            _presenceTracker = presenceTracker;
            _hostTracker = hostTracker;
        }

        public void Subscribe(VarRegistry registry)
        {
            Subscribe(registry.Bools, values => values.Bools);
            Subscribe(registry.Floats, values => values.Floats);
            Subscribe(registry.Ints,  values => values.Ints);
            Subscribe(registry.Strings, values => values.Strings);
            Subscribe(registry.Objects, values => values.Objects);
        }

        private void Subscribe<T>(Dictionary<string, List<IVar<T>>> vars, VarValueAccessor<T> accessor)
        {
            var flattenedVars = vars.Values.SelectMany(l => l);
            foreach (var var in flattenedVars)
            {
                Logger?.DebugFormat($"Subscribing to shared variable with key {var.Key}");
                var.OnValueChanged += (evt) => HandleLocalVarChanged(var.Key, var, evt, accessor);
            }
        }

        private void HandleLocalVarChanged<T>(string key, IVar<T> var, IVarEvent<T> evt, VarValueAccessor<T> accessor)
        {
            if (evt.Source.UserId != _presenceTracker.GetSelf().UserId)
            {
                // ingress should only send out changes initated by self.
                Logger?.DebugFormat($"Egress for {_presenceTracker.UserId} is ignoring local shared var change.");
                return;
            }

            bool isHost = _hostTracker.IsSelfHost();

            Logger?.DebugFormat($"Local shared variable changed. Key: {key}, OldValue: {evt.ValueChange.OldValue}, Value: {evt.ValueChange.NewValue}");

            if (isHost)
            {
                _hostEgress.HandleLocalVarChanged(key, var, evt.ValueChange.NewValue, accessor);
            }
            else
            {
                _guestEgress.HandleLocalVarChanged(key, var, evt.ValueChange.NewValue, accessor);
            }
        }
    }
}
