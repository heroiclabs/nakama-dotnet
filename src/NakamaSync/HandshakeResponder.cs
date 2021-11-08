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
                CopyToGuestResponse(_registry.Bools, syncValues.Bools);
                CopyToGuestResponse(_registry.Floats, syncValues.Floats);
                CopyToGuestResponse(_registry.Ints, syncValues.Ints);
                CopyToGuestResponse(_registry.Strings, syncValues.Strings);
            }
            else
            {
                Logger?.WarnFormat($"Remote keys from {source.UserId} do not match the local keys from {_presenceTracker.GetSelf().UserId}");
            }

            var response = new HandshakeResponse(syncValues, success);
            socket.SendHandshakeResponse(response, source);
        }

        private void CopyToGuestResponse<T>(VarRegistry<T> registry, Envelope<T> env)
        {

            foreach (var var in registry.SharedVars.Values)
            {
                T rawValue = var.GetValue();
                Logger?.DebugFormat("Shared variable value for initial payload: " + rawValue);
                int lockVersion = _lockVersionGuard.HasLockVersion(var.Key) ? _lockVersionGuard.GetLockVersion(var.Key) : 0;
                var sharedValue = new SharedVarValue<T>(var.Key, rawValue, lockVersion, var.ValidationStatus, isAck: false);
                env.SharedValues.Add(sharedValue);
            }

            foreach (var var in registry.PresenceVars.Values.SelectMany(l => l))
            {
                if (var.Presence == null)
                {
                    // expected, unoccupied presence var
                    continue;
                }

                T rawValue = var.GetValue();
                Logger?.DebugFormat("Shared variable value for initial payload: " + rawValue);
                // todo check for null presence on var?

                int lockVersion = _lockVersionGuard.HasLockVersion(var.Key) ? _lockVersionGuard.GetLockVersion(var.Key) : 0;

                var value = new PresenceVarValue<T>(var.Key, rawValue, lockVersion, var.ValidationStatus, isAck: false, var.Presence.UserId);
                env.PresenceValues.Add(value);
            }
        }

        private void CopyToGuestResponse<T>(IEnumerable<PresenceVar<T>> vars, List<PresenceVarValue<T>> values)
        {

        }
    }
}
