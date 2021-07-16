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
    internal class HandshakeResponder : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly VarKeys _keys;
        private readonly VarRegistry _registry;
        private readonly PresenceTracker _presenceTracker;

        public HandshakeResponder(VarKeys keys, VarRegistry registry, PresenceTracker presenceTracker)
        {
            _keys = keys;
            _registry = registry;
            _presenceTracker = presenceTracker;
        }

        public void Subscribe(SyncSocket socket)
        {
            socket.OnHandshakeRequest += (source, request) => HandleHandshakeRequest(source, request, socket);
        }

        private void HandleHandshakeRequest(IUserPresence source, HandshakeRequest request, SyncSocket socket)
        {
            var syncValues = new Envelope();

            bool success = request.AllKeys.SequenceEqual(_keys.GetKeys());


            if (success)
            {
                Logger?.InfoFormat($"Remote keys from {source.UserId} match the local keys from {_presenceTracker.GetSelf().UserId}");
                CopySharedVarToGuest(_registry.SharedBools, syncValues.SharedBools);
                CopySharedVarToGuest(_registry.SharedFloats, syncValues.SharedFloats);
                CopySharedVarToGuest(_registry.SharedInts, syncValues.SharedInts);
                CopySharedVarToGuest(_registry.SharedStrings, syncValues.SharedStrings);
                CopyUserVarToGuest(_registry.UserBools, syncValues.UserBools);
                CopyUserVarToGuest(_registry.UserFloats, syncValues.UserFloats);
                CopyUserVarToGuest(_registry.UserInts, syncValues.UserInts);
                CopyUserVarToGuest(_registry.UserStrings, syncValues.UserStrings);
            }
            else
            {
                Logger?.WarnFormat($"Remote keys from {source.UserId} do not match the local keys from {_presenceTracker.GetSelf().UserId}");
            }

            var response = new HandshakeResponse(syncValues, success);
            socket.SendHandshakeResponse(source, response);
        }

        private void CopySharedVarToGuest<T>(Dictionary<string, SharedVar<T>> vars, List<SharedValue<T>> values)
        {
            foreach (var kvp in vars)
            {
                SharedVar<T> var = kvp.Value;
                T rawValue = var.GetValue();
                Logger?.DebugFormat("Shared variable value for initial payload: " + rawValue);
                var sharedValue = new SharedValue<T>(kvp.Key, rawValue, _keys.GetLockVersion(kvp.Key), _keys.GetValidationStatus(kvp.Key));
                values.Add(sharedValue);
            }
        }

        private void CopyUserVarToGuest<T>(Dictionary<string, UserVar<T>> vars, List<UserValue<T>> values)
        {
            foreach (var varKvp in vars)
            {
                UserVar<T> var = varKvp.Value;

                Logger?.DebugFormat($"Handshake responder scanning through user values to copy for key: {varKvp.Key}");

                foreach (KeyValuePair<string, T> userKvp in var.Values)
                {
                    // TODO handle data for a stale user
                    Logger?.DebugFormat($"Handshake responder found user value for user: {userKvp.Key}");

                    // use the var presences, not the presence tracker, in case
                    // user has stored a value left before this handshake.

                    T rawValue = userKvp.Value;

                    Logger?.DebugFormat($"User variable value for initial payload: User: {userKvp.Key}, Raw Value: ${rawValue}");

                    var value = new UserValue<T>(varKvp.Key, rawValue, _keys.GetLockVersion(varKvp.Key), _keys.GetValidationStatus(varKvp.Key), userKvp.Key);
                    values.Add(value);
                }
            }
        }
    }
}