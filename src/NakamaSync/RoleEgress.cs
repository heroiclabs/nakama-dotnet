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

        public void Subscribe(SharedVars sharedVars, UserVars userVars)
        {
            Subscribe(sharedVars.Bools, values => values.SharedBools);
            Subscribe(sharedVars.Floats, values => values.SharedFloats);
            Subscribe(sharedVars.Ints,  values => values.SharedInts);
            Subscribe(sharedVars.Bools, values => values.SharedBools);

            Subscribe(userVars.Bools, values => values.UserBools);
            Subscribe(userVars.Floats, values => values.UserFloats);
            Subscribe(userVars.Ints,  values => values.UserInts);
            Subscribe(userVars.Strings, values => values.UserStrings);
        }

        private void Subscribe<T>(SyncVarDictionary<SyncVarKey, SharedVar<T>> vars, SharedVarAccessor<T> accessor)
        {
            foreach (var key in vars.GetKeys())
            {
                vars.GetSyncVar(key).OnLocalValueChanged += evt => GetEgress().HandleLocalSharedVarChanged(key, evt.NewValue, accessor);
            }
        }

        private void Subscribe<T>(SyncVarDictionary<SyncVarKey, UserVar<T>> vars, UserVarAccessor<T> accessor)
        {
            foreach (var key in vars.GetKeys())
            {
                vars.GetSyncVar(key).OnLocalValueChanged += evt => GetEgress().HandleLocalUserVarChanged(key, evt.NewValue, accessor, evt.Target);
            }
        }

        private IVarEgress GetEgress()
        {
            if (_presenceTracker.IsSelfHost())
            {
                return _hostEgress;
            }

            return _guestEgress;
        }
    }
}
