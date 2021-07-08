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
    internal class HostHandshaker
    {
        private readonly SyncVarKeys _keys;
        private readonly SharedVars _sharedVars;
        private readonly UserVars _userVars;
        private readonly PresenceTracker _presenceTracker;

        public HostHandshaker(SyncVarKeys keys, SharedVars sharedVars, UserVars userVars, PresenceTracker presenceTracker)
        {
            _keys = keys;
            _sharedVars = sharedVars;
            _userVars = userVars;
            _presenceTracker = presenceTracker;
        }

        public void ListenForHandshakes(SyncSocket socket)
        {
            socket.OnHandshakeRequest += (source, request) => HandleHandshakeRequest(source, request, socket);
        }

        private void HandleHandshakeRequest(IUserPresence source, HandshakeRequest request, SyncSocket socket)
        {
            var syncValues = new SyncEnvelope();

            bool success = request.AllKeys.SequenceEqual(_keys.GetKeys());

            if (success)
            {
                CopySharedVarToGuest(_sharedVars.Bools, source, syncValues.SharedBools);
                CopySharedVarToGuest(_sharedVars.Floats, source, syncValues.SharedFloats);
                CopySharedVarToGuest(_sharedVars.Ints, source, syncValues.SharedInts);
                CopySharedVarToGuest(_sharedVars.Strings, source, syncValues.SharedStrings);
                CopyUserVarToGuest(_userVars.Bools, source, syncValues.UserBools);
                CopyUserVarToGuest(_userVars.Floats, source, syncValues.UserFloats);
                CopyUserVarToGuest(_userVars.Ints, source, syncValues.UserInts);
                CopyUserVarToGuest(_userVars.Strings, source, syncValues.UserStrings);
            }

            var response = new HandshakeResponse(syncValues, success);
            socket.SendHandshakeResponse(source, response);
        }

        private void CopySharedVarToGuest<T>(SyncVarDictionary<SyncVarKey, SharedVar<T>> vars, IUserPresence target, List<SharedValue<T>> values)
        {
            foreach (var key in vars.GetKeys())
            {
                SharedVar<T> var = vars.GetSyncVar(key);
                var value = new SharedValue<T>(key, var.GetValue(), _keys.GetLockVersion(key), _keys.GetValidationStatus(key));
                values.Add(value);
            }
        }

        private void CopyUserVarToGuest<T>(SyncVarDictionary<SyncVarKey, UserVar<T>> vars, IUserPresence target, List<UserValue<T>> values)
        {
            foreach (var key in vars.GetKeys())
            {
                UserVar<T> var = vars.GetSyncVar(key);

                foreach (KeyValuePair<string, T> kvp in var.Values)
                {
                    // TODO handle data for a stale user
                    var value = new UserValue<T>(key, var.GetValue(), _keys.GetLockVersion(key), _keys.GetValidationStatus(key), _presenceTracker.GetPresence(kvp.Key));
                    values.Add(value);
                }
            }
        }
    }
}