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

using Nakama;

namespace NakamaSync
{
    internal class PresenceGuestEgress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly VarKeys _keys;
        private readonly EnvelopeBuilder _builder;

        public PresenceGuestEgress(VarKeys keys, EnvelopeBuilder builder)
        {
            _keys = keys;
            _builder = builder;
        }

        public void HandleLocalPresenceVarChanged<T>(string key, T newValue, string targetId, PresenceVarAccessor<T> accessor)
        {
            var status = _keys.GetValidationStatus(key);

            // this value was validated and now we've
            // modified it as a guest so revert it to pending status
            if (status == ValidationStatus.Validated)
            {
                status = ValidationStatus.Pending;
                _keys.SetValidationStatus(key, status);
            }

            _keys.IncrementLockVersion(key);
            var newSyncedValue = new PresenceValue<T>(key, newValue, _keys.GetLockVersion(key), status, targetId);

            _builder.AddPresenceVar(accessor, newSyncedValue);
            _builder.SendEnvelope();
        }
    }
}
