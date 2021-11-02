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
    ///
    /// Todo rename to "OtherVar"? or something like that.
    /// </summary>
    public class OtherVar<T> : Var<T>
    {
        public event PresenceChangedHandler OnPresenceChanged;

        public event Action<IOtherVarEvent<T>> OnValueChanged;

        public IUserPresence Presence => _presence;

        private IUserPresence _presence;

        internal void SetValue(T value, ValidationStatus validationStatus)
        {
            T oldValue = _value;
            _value = value;

            ValidationStatus oldStatus = ValidationStatus;
            ValidationStatus = validationStatus;

            var valueChange = new ValueChange<T>(oldValue, value);
            var statusChange = new ValidationChange(oldStatus, validationStatus);

            OnValueChanged?.Invoke(new OtherVarEvent<T>(Presence, valueChange, statusChange));
        }

        internal override void Reset()
        {
            SetPresence(null);
            OnPresenceChanged = null;
            Self = null;
            _value = default(T);
            _validationHandler = null;
            _validationStatus = ValidationStatus.None;
            OnValueChanged = null;
            IsHost = false;
        }

        internal void SetPresence(IUserPresence presence)
        {
            if (presence?.UserId == _presence?.UserId)
            {
                // todo log warning here?
                return;
            }

            if (presence?.UserId == Self?.UserId)
            {
                throw new InvalidOperationException("OtherVar cannot have a presence id equal to self id.");
            }

            var oldOwner = _presence;
            _presence = presence;
            var presenceChange = new PresenceChange(oldOwner, presence);
            OnPresenceChanged?.Invoke(presenceChange);
        }
    }
}
