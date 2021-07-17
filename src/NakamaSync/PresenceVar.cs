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

using System;
using Nakama;

namespace NakamaSync
{
    public delegate void PresenceChangedHandler(PresenceChange presenceChange);

    /// <summary>
    /// A variable containing a value for a user in the match. The value is synchronized across all users
    /// but can only be set by the associated user.
    /// </summary>
    public class PresenceVar<T> : SyncVar<T>
    {
        /// <summary>
        /// If this delegate is set and the current client is a guest, then
        /// when a synced value is set, this client will reach out to the
        /// host who will validate and if it's validated the host will send to all clients
        /// otherwise a ReplicationValidationException will be thrown on this device.
        /// </summary>
        public event Action<IPresenceVarEvent<T>> OnValueChanged;

        public event PresenceChangedHandler OnPresenceChanged;
        public IUserPresence Owner { get; internal set; }

        internal void SetValue(T value, ValidationStatus validationStatus)
        {
            T oldValue = _value;
            _value = value;

            ValidationStatus oldStatus = ValidationStatus;
            ValidationStatus = validationStatus;

            var valueChange = new ValueChange<T>(oldValue, value);
            var statusChange = new ValidationChange(oldStatus, validationStatus);

            OnValueChanged?.Invoke(new PresenceVarEvent<T>(Owner, valueChange, statusChange));
        }

        internal override void Reset()
        {
            Owner = null;
            Self = null;
            _value = default(T);
            _validationHandler = null;
            _validationStatus = ValidationStatus.None;
            OnValueChanged = null;
        }
    }
}
