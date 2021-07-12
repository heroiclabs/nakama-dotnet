

using System.Collections.Generic;
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
    internal class RoleEgress
    {
        private RolePresenceTracker _presenceTracker;
        private HostEgress _hostEgress;
        private GuestEgress _guestEgress;

        public RoleEgress(GuestEgress guestEgress, HostEgress hostEgress, RolePresenceTracker presenceTracker)
        {
            _guestEgress = guestEgress;
            _hostEgress = hostEgress;
            _presenceTracker = presenceTracker;
        }

        public void Subscribe(SyncVarRegistry registry)
        {
            Subscribe(registry.SharedBools, values => values.SharedBools);
            Subscribe(registry.SharedFloats, values => values.SharedFloats);
            Subscribe(registry.SharedInts,  values => values.SharedInts);
            Subscribe(registry.SharedBools, values => values.SharedBools);

            Subscribe(registry.UserBools, values => values.UserBools);
            Subscribe(registry.UserFloats, values => values.UserFloats);
            Subscribe(registry.UserInts,  values => values.UserInts);
            Subscribe(registry.UserStrings, values => values.UserStrings);
        }

        private void Subscribe<T>(Dictionary<string, SharedVar<T>> vars, SharedVarAccessor<T> accessor)
        {
            bool isHost = _presenceTracker.IsSelfHost();

            foreach (var kvp in vars)
            {
                if (isHost)
                {
                    vars[kvp.Key].OnLocalValueChanged += evt => _hostEgress.HandleLocalSharedVarChanged(kvp.Key, evt.NewValue, accessor);
                }
                else
                {
                    vars[kvp.Key].OnLocalValueChanged += evt => _guestEgress.HandleLocalSharedVarChanged(kvp.Key, evt.NewValue, accessor);
                }
            }
        }

        private void Subscribe<T>(Dictionary<string, UserVar<T>> vars, UserVarAccessor<T> accessor)
        {
            bool isHost = _presenceTracker.IsSelfHost();

            foreach (var kvp in vars)
            {
                if (isHost)
                {
                    vars[kvp.Key].OnLocalValueChanged += evt => _hostEgress.HandleLocalUserVarChanged(kvp.Key, evt.NewValue, evt.Target, accessor);
                }
                else
                {
                    vars[kvp.Key].OnLocalValueChanged += evt => _guestEgress.HandleLocalUserVarChanged(kvp.Key, evt.NewValue, evt.Target, accessor);
                }
            }
        }
    }
}
