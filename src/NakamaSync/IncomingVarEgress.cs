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
    internal class IncomingVarEgress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private PresenceTracker _presenceTracker;
        private HostTracker _hostTracker;
        private IncomingVarGuestEgress _guestEgress;
        private IncomingVarHostEgress _hostEgress;

        public IncomingVarEgress(IncomingVarGuestEgress guestEgress, IncomingVarHostEgress hostEgress, PresenceTracker presenceTracker, HostTracker hostTracker)
        {
            _guestEgress = guestEgress;
            _hostEgress = hostEgress;
            _presenceTracker = presenceTracker;
            _hostTracker = hostTracker;
        }

        public void Subscribe(IncomingVarRegistry registry)
        {
            Subscribe(registry.Bools.Values, values => values.Bools);
            Subscribe(registry.Floats.Values, values => values.Floats);
            Subscribe(registry.Ints.Values,  values => values.Ints);
            Subscribe(registry.Strings.Values, values => values.Strings);
            Subscribe(registry.Objects.Values, values => values.Objects);
        }

        private void Subscribe<T>(IEnumerable<IIncomingVar<T>> vars, VarValueAccessor<T> accessor)
        {
            foreach (var var in vars)
            {
                Logger?.DebugFormat($"Subscribing to shared variable with key {var.Key}");
                var.OnValueChanged += (evt) => HandleLocalVarChanged(var.Key, var, evt, accessor);
            }
        }

        private void HandleLocalVarChanged<T>(string key, IIncomingVar<T> var, IVarEvent<T> evt, VarValueAccessor<T> accessor)
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
