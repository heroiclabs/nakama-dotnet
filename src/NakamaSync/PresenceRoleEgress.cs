

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

using Nakama;
using System.Collections.Generic;

namespace NakamaSync
{
    // todo split this into shared and user
    internal class PresenceRoleEgress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private RoleTracker _roleTracker;
        private PresenceGuestEgress _presenceGuestEgress;
        private PresenceHostEgress _presenceHostEgress;

        public PresenceRoleEgress(PresenceGuestEgress presenceGuestEgress, PresenceHostEgress presenceHostEgress, RoleTracker roleTracker)
        {
            _presenceGuestEgress = presenceGuestEgress;
            _presenceHostEgress = presenceHostEgress;
            _roleTracker = roleTracker;
        }

        public void Subscribe(VarRegistry registry, HandshakeRequester requester)
        {
            requester.OnInitialStoreLoaded += () =>
            {
                // now that we have initial store loaded,
                // listen for user modifications to sync vars.
                Subscribe(registry);
            };
        }

        public void Subscribe(VarRegistry registry)
        {
            Subscribe(registry.UserBools, values => values.PresenceBools);
            Subscribe(registry.UserFloats, values => values.PresenceFloats);
            Subscribe(registry.UserInts,  values => values.PresenceInts);
            Subscribe(registry.UserStrings, values => values.PResenceStrings);
        }

        private void Subscribe<T>(Dictionary<string, PresenceVar<T>> vars, PresenceVarAccessor<T> accessor)
        {
            foreach (var kvp in vars)
            {
                vars[kvp.Key].OnLocalValueChanged += (evt) => HandleLocalPresenceVarChanged(kvp.Key, evt, accessor);
            }
        }

        private void HandleLocalPresenceVarChanged<T>(string key, IPresenceVarEvent<T> evt, PresenceVarAccessor<T> accessor)
        {
            bool isHost = _roleTracker.IsSelfHost();

            Logger?.DebugFormat($"Local user variable changed. Key: {key}, OldValue: {evt.OldValue}, Value: {evt.NewValue}, Target: {evt.TargetId}");

            if (isHost)
            {
                _presenceHostEgress.HandleLocalPresenceVarChanged(key, evt.NewValue, evt.TargetId, accessor);
            }
            else
            {
                _presenceGuestEgress.HandleLocalPresenceVarChanged(key, evt.NewValue, evt.TargetId, accessor);
            }
        }
    }
}
