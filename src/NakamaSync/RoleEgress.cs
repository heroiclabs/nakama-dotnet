

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
        private RoleTracker _presenceTracker;
        private HostEgress _hostEgress;
        private GuestEgress _guestEgress;

        public RoleEgress(GuestEgress guestEgress, HostEgress hostEgress, RoleTracker presenceTracker)
        {
            _guestEgress = guestEgress;
            _hostEgress = hostEgress;
            _presenceTracker = presenceTracker;
        }

        public void Subscribe(VarRegistry registry)
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
            foreach (var kvp in vars)
            {
                vars[kvp.Key].OnLocalValueChanged += (evt) => HandleLocalSharedVarChanged(kvp.Key, evt, accessor);
            }
        }

        private void HandleLocalSharedVarChanged<T>(string key, ISharedVarEvent<T> evt, SharedVarAccessor<T> accessor)
        {
            bool isHost = _presenceTracker.IsSelfHost();

            if (isHost)
            {
                _hostEgress.HandleLocalSharedVarChanged(key, evt.NewValue, accessor);
            }
            else
            {
                _guestEgress.HandleLocalSharedVarChanged(key, evt.NewValue, accessor);
            }
        }

        private void Subscribe<T>(Dictionary<string, UserVar<T>> vars, UserVarAccessor<T> accessor)
        {
            foreach (var kvp in vars)
            {
                vars[kvp.Key].OnLocalValueChanged += (evt) => HandleLocalUserVarChanged(kvp.Key, evt, accessor);
            }
        }

        private void HandleLocalUserVarChanged<T>(string key, IUserVarEvent<T> evt, UserVarAccessor<T> accessor)
        {
            bool isHost = _presenceTracker.IsSelfHost();

            if (isHost)
            {
                _hostEgress.HandleLocalUserVarChanged(key, evt.NewValue, evt.Target, accessor);
            }
            else
            {
                _guestEgress.HandleLocalUserVarChanged(key, evt.NewValue, evt.Target, accessor);
            }
        }
    }
}
