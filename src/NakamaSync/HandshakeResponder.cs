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

        private readonly object _lock = new object();

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
            lock (_lock)
            {
                var syncValues = new Envelope();

                bool success = request.AllKeys.SequenceEqual(_keys.GetKeys());

                if (success)
                {
                    CopySharedVarToGuest(_registry.SharedBools, source, syncValues.SharedBools);
                    CopySharedVarToGuest(_registry.SharedFloats, source, syncValues.SharedFloats);
                    CopySharedVarToGuest(_registry.SharedInts, source, syncValues.SharedInts);
                    CopySharedVarToGuest(_registry.SharedStrings, source, syncValues.SharedStrings);
                    CopyUserVarToGuest(_registry.UserBools, source, syncValues.UserBools);
                    CopyUserVarToGuest(_registry.UserFloats, source, syncValues.UserFloats);
                    CopyUserVarToGuest(_registry.UserInts, source, syncValues.UserInts);
                    CopyUserVarToGuest(_registry.UserStrings, source, syncValues.UserStrings);
                }

                var response = new HandshakeResponse(syncValues, success);
                socket.SendHandshakeResponse(source, response);
            }
        }

        private void CopySharedVarToGuest<T>(Dictionary<string, SharedVar<T>> vars, IUserPresence source, List<SharedValue<T>> values)
        {
            foreach (var kvp in vars)
            {
                SharedVar<T> var = kvp.Value;

                Logger?.DebugFormat("Shared variable value for initial payload: " + var.GetValue());
                var value = new SharedValue<T>(kvp.Key, var.GetValue(), _keys.GetLockVersion(kvp.Key), _keys.GetValidationStatus(kvp.Key));
                values.Add(value);
            }
        }

        private void CopyUserVarToGuest<T>(Dictionary<string, UserVar<T>> vars, IUserPresence source, List<UserValue<T>> values)
        {
            foreach (var kvp in vars)
            {
                UserVar<T> var = kvp.Value;

                Logger?.DebugFormat($"Handshake responder scanning through user values to copy for key: {kvp.Key}");

                foreach (KeyValuePair<string, T> innerKvp in kvp.Value.Values)
                {
                    // TODO handle data for a stale user
                    Logger?.DebugFormat($"Handshake responder found user value for user: {innerKvp.Key}");

                    IUserPresence targetPresence = _presenceTracker.GetPresence(innerKvp.Key);
                    System.Console.WriteLine(targetPresence);
                    T rawValue = kvp.Value.GetValue(targetPresence);
                    Logger?.DebugFormat($"User variable value for initial payload: User: {targetPresence}, Raw Value: ${rawValue}");

                    var value = new UserValue<T>(kvp.Key, rawValue, _keys.GetLockVersion(kvp.Key), _keys.GetValidationStatus(kvp.Key), targetPresence);
                    values.Add(value);
                }
            }
        }
    }
}