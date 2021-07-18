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

        private readonly LockVersionGuard _lockVersionGuard;
        private readonly VarRegistry _registry;
        private readonly PresenceTracker _presenceTracker;

        public HandshakeResponder(LockVersionGuard lockVersionGuard, VarRegistry registry, PresenceTracker presenceTracker)
        {
            _lockVersionGuard = lockVersionGuard;
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

            bool success = request.AllKeys.SequenceEqual(_registry.GetAllKeys());

            if (success)
            {
                Logger?.InfoFormat($"Remote keys from {source.UserId} match the local keys from {_presenceTracker.GetSelf().UserId}");
                CopyToGuestResponse(_registry.SharedVarRegistry.SharedBools, syncValues.SharedBools);
                CopyToGuestResponse(_registry.SharedVarRegistry.SharedFloats, syncValues.SharedFloats);
                CopyToGuestResponse(_registry.SharedVarRegistry.SharedInts, syncValues.SharedInts);
                CopyToGuestResponse(_registry.SharedVarRegistry.SharedStrings, syncValues.SharedStrings);
                CopyToGuestResponse(_registry.PresenceVarRegistry.PresenceBools, syncValues.PresenceBools);
                CopyToGuestResponse(_registry.PresenceVarRegistry.PresenceFloats, syncValues.PresenceFloats);
                CopyToGuestResponse(_registry.PresenceVarRegistry.PresenceInts, syncValues.PresenceInts);
                CopyToGuestResponse(_registry.PresenceVarRegistry.PresenceStrings, syncValues.PresenceStrings);
            }
            else
            {
                Logger?.WarnFormat($"Remote keys from {source.UserId} do not match the local keys from {_presenceTracker.GetSelf().UserId}");
            }

            var response = new HandshakeResponse(syncValues, success);
            socket.SendHandshakeResponse(source, response);
        }

        private void CopyToGuestResponse<T>(Dictionary<string, SharedVar<T>> vars, List<SharedValue<T>> values)
        {
            foreach (var kvp in vars)
            {
                SharedVar<T> var = kvp.Value;
                T rawValue = var.GetValue();
                Logger?.DebugFormat("Shared variable value for initial payload: " + rawValue);
                var sharedValue = new SharedValue<T>(kvp.Key, rawValue, _lockVersionGuard.GetLockVersion(kvp.Key), kvp.Value.ValidationStatus);
                values.Add(sharedValue);
            }
        }

        private void CopyToGuestResponse<T>(Dictionary<string, PresenceVarCollection<T>> varsByKey, List<PresenceValue<T>> values)
        {
            foreach (var varKvp in varsByKey)
            {
                string varKey = varKvp.Key;
                List<PresenceVar<T>> vars = varKvp.Value.PresenceVars;

                Logger?.DebugFormat($"Handshake responder scanning through user values to copy for key: {varKvp.Key}");

                foreach (PresenceVar<T> var in vars)
                {
                    // TODO handle data for a stale user?
                    if (var.Presence == null)
                    {
                        // this is a valid case
                        continue;
                    }

                    T rawValue = var.GetValue();

                    Logger?.DebugFormat($"User variable value for initial payload: User: {varKvp.Key}, Raw Value: ${rawValue}");

                    var value = new PresenceValue<T>(new PresenceVarKey(varKey, var.Presence.UserId), rawValue, _lockVersionGuard.GetLockVersion(varKvp.Key), var.ValidationStatus);
                    values.Add(value);
                }

                T rawSelfValue = varKvp.Value.SelfVar.GetValue();

                Logger?.DebugFormat($"User variable value for initial payload: User: {varKvp.Key}, Raw Value: ${rawSelfValue}");

                var selfValue = new PresenceValue<T>(new PresenceVarKey(varKey, varKvp.Value.SelfVar.Self.UserId), rawSelfValue, _lockVersionGuard.GetLockVersion(varKvp.Key), varKvp.Value.SelfVar.ValidationStatus);
                values.Add(selfValue);
            }
        }
    }
}