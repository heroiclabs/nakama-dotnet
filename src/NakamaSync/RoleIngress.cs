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
    internal class RoleIngress
    {
        private readonly RolePresenceTracker _presenceTracker;
        private readonly GuestIngress _guestIngress;
        private readonly HostIngress _hostIngress;
        private readonly SharedVars _sharedVars;
        private readonly UserVars _userVars;

        public RoleIngress(GuestIngress guestIngress, HostIngress hostIngress, RolePresenceTracker presenceTracker, SharedVars sharedVars, UserVars userVars)
        {
            _presenceTracker = presenceTracker;
            _guestIngress = guestIngress;
            _hostIngress = hostIngress;
            _sharedVars = sharedVars;
            _userVars = userVars;
        }

        public void Subscribe(SyncSocket socket)
        {
            socket.OnSyncData += HandleSyncData;
        }

        public void HandleSyncData(IUserPresence source, SyncValues incomingValues)
        {
            // todo clean up the redundancy here
            HandleIncomingSharedSyncValues(source, incomingValues.SharedBools, values => values.SharedBools, _sharedVars.Bools);
            HandleIncomingSharedSyncValues(source, incomingValues.SharedFloats, values => values.SharedFloats, _sharedVars.Floats);
            HandleIncomingSharedSyncValues(source, incomingValues.SharedInts, values => values.SharedInts, _sharedVars.Ints);
            HandleIncomingSharedSyncValues(source, incomingValues.SharedStrings, values => values.SharedStrings, _sharedVars.Strings);

            HandleIncomingUserSyncValues(source, incomingValues.UserBools, values => values.UserBools, _userVars.Bools);
            HandleIncomingUserSyncValues(source, incomingValues.UserFloats, values => values.UserFloats, _userVars.Floats);
            HandleIncomingUserSyncValues(source, incomingValues.UserInts, values => values.UserInts, _userVars.Ints);
            HandleIncomingUserSyncValues(source, incomingValues.UserStrings, values => values.UserStrings, _userVars.Strings);
        }

        private IVarIngress GetIngress()
        {
            if (_presenceTracker.IsSelfHost())
            {
                return _hostIngress;
            }

            return _guestIngress;
        }

        private void HandleIncomingSharedSyncValues<T>(IUserPresence source, List<SharedValue<T>> values, SharedVarAccessor<T> accessor, SyncVarDictionary<SyncVarKey, SharedVar<T>> vars)
        {
            IVarIngress ingress = GetIngress();

            foreach (SharedValue<T> sharedValue in values)
            {
                ingress.HandleIncomingSharedValue(sharedValue, accessor, vars, source);
            }
        }

        private void HandleIncomingUserSyncValues<T>(IUserPresence source, List<UserValue<T>> values, UserVarAccessor<T> accessor, SyncVarDictionary<SyncVarKey, UserVar<T>> vars)
        {
            IVarIngress ingress = GetIngress();

            foreach (UserValue<T> userValue in values)
            {
                ingress.HandleIncomingUserValue(userValue, accessor, vars, source);
            }
        }
    }
}
