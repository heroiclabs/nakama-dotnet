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
    internal class VarIngress : ISyncService
    {
        public ILogger Logger
        {
            get;
            set;
        }

        public SyncErrorHandler ErrorHandler
        {
            get;
            set;
        }

        private readonly VarRegistry _registry;
        private readonly LockVersionGuard _lockVersionGuard;
        private readonly HostTracker _hostTracker;

        public VarIngress(VarRegistry registry, LockVersionGuard lockVersionGuard, HostTracker hostTracker)
        {
            _registry = registry;
            _lockVersionGuard = lockVersionGuard;
            _hostTracker = hostTracker;
        }

        public void Subscribe(SyncSocket socket)
        {
            socket.OnSyncEnvelope += (source, envelope) =>
            {
                ReceiveSyncEnvelope(source, envelope, socket);
            };
        }

        public void ReceiveSyncEnvelope(IUserPresence source, Envelope envelope, SyncSocket socket)
        {
            Logger?.DebugFormat($"Ingress received sync envelope.");

            var responseEnvelope = new Envelope();

            var boolResponses = HandleSyncEnvelope(source, envelope.Bools, _registry.Bools);
            var floatResponses = HandleSyncEnvelope(source, envelope.Floats, _registry.Floats);
            var intResponses = HandleSyncEnvelope(source, envelope.Ints, _registry.Ints);
            var stringResponses = HandleSyncEnvelope(source, envelope.Strings, _registry.Strings);
            var objectResponses = HandleSyncEnvelope(source, envelope.Objects, _registry.Objects);

            var presenceBoolResponses = HandleSyncEnvelope(source, envelope.PresenceBools, _registry.PresenceBools);
            var presenceFloatResponses = HandleSyncEnvelope(source, envelope.PresenceFloats, _registry.PresenceFloats);
            var presenceIntResponses = HandleSyncEnvelope(source, envelope.PresenceInts, _registry.PresenceInts);
            var presenceStringResponses = HandleSyncEnvelope(source, envelope.PresenceStrings, _registry.PresenceStrings);
            var presenceObjectResponses = HandleSyncEnvelope(source, envelope.PresenceObjects, _registry.PresenceObjects);

            responseEnvelope.Bools.AddRange(boolResponses);
            responseEnvelope.Floats.AddRange(floatResponses);
            responseEnvelope.Ints.AddRange(intResponses);
            responseEnvelope.Objects.AddRange(objectResponses);
            responseEnvelope.Strings.AddRange(stringResponses);

            responseEnvelope.PresenceBools.AddRange(presenceBoolResponses);
            responseEnvelope.PresenceFloats.AddRange(presenceFloatResponses);
            responseEnvelope.PresenceInts.AddRange(presenceIntResponses);
            responseEnvelope.PresenceObjects.AddRange(presenceObjectResponses);
            responseEnvelope.PresenceStrings.AddRange(presenceStringResponses);

            socket.SendSyncDataToAll(responseEnvelope);
            Logger?.DebugFormat($"Ingress done processing sync envelope.");
        }

        private List<SharedVarValue<T>> HandleSyncEnvelope<T>(IUserPresence source, List<SharedVarValue<T>> values, Dictionary<string, IVar<T>> vars)
        {
            Logger?.DebugFormat($"Ingress processing num contexts: {values.Count}");

            var responses = new List<SharedVarValue<T>>();

            foreach (SharedVarValue<T> value in values)
            {
                if (!_lockVersionGuard.IsValidLockVersion(value.Key, value.LockVersion))
                {
                    // expected race to occur
                    Logger?.DebugFormat($"Ingress received invalid lock version: {value.LockVersion}");
                    continue;
                }

                if (!vars.ContainsKey(value.Key))
                {
                    // not expected
                    ErrorHandler?.Invoke(new KeyNotFoundException($"Could not find var with key: {value.Key}"));
                    continue;
                }

                var var = vars[value.Key];
                HandleSyncValue(source, var, value, responses);
            }

            return responses;
        }

        private List<PresenceVarValue<T>> HandleSyncEnvelope<T>(IUserPresence source, List<PresenceVarValue<T>> values, Dictionary<string, List<PresenceVar<T>>> vars)
        {
            Logger?.DebugFormat($"Ingress processing num contexts: {values.Count}");

            var responses = new List<PresenceVarValue<T>>();

            foreach (PresenceVarValue<T> value in values)
            {
                if (!vars.ContainsKey(value.Key))
                {
                    // not expected
                    ErrorHandler?.Invoke(new KeyNotFoundException($"Could not find var with key: {value.Key}"));
                    continue;
                }


                foreach (var var in vars[value.Key])
                {
                    HandleSyncValue(source, var, value, responses);
                }
            }

            return responses;
        }

        private void HandleSyncValue<T>(IUserPresence source, IVar<T> var, SharedVarValue<T> value, List<SharedVarValue<T>> responses)
        {
            if (value.IsAck)
            {
                var.SetValidationStatus(ValidationStatus.Valid);
                return;
            }

            bool wasValid = var.SetValue(source, value.Value, value.ValidationStatus);

            if (_hostTracker.IsSelfHost())
            {
                ValidationStatus newStatus = wasValid ? ValidationStatus.Valid : ValidationStatus.Invalid;
                responses.Add(new SharedVarValue<T>(value.Key, value.Value, value.LockVersion, newStatus, isAck: true));
            }
        }

        private void HandleSyncValue<T>(IUserPresence source, IVar<T> var, PresenceVarValue<T> value, List<PresenceVarValue<T>> responses)
        {
            if (value.IsAck)
            {
                var.SetValidationStatus(ValidationStatus.Valid);
                return;
            }

            bool wasValid = var.SetValue(source, value.Value, value.ValidationStatus);

            if (_hostTracker.IsSelfHost())
            {
                ValidationStatus newStatus = wasValid ? ValidationStatus.Valid : ValidationStatus.Invalid;
                responses.Add(new PresenceVarValue<T>(value.Key, value.Value, value.LockVersion, newStatus, isAck: true, value.UserId));
            }
        }
    }
}
