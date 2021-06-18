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
using System.Collections.Generic;

namespace Nakama.Replicated
{
    public delegate bool HostValidationHandler<T>(T oldValue, T newValue, IUserPresence source, IUserPresence target);
    public delegate void OwnedChangedHandler<T>(T oldValue, T newValue, IUserPresence source, IUserPresence target);

    /// <summary>
    /// A variable whose value for each user is synchronized across all clients connected to the same match.
    /// </summary>
    public class Owned<T>
    {
        /// <summary>
        /// If this delegate is set and the current client is a guest, then
        /// when a replicated value is set, this client will reach out to the
        /// host who will validate and if it's validated the host will send to all clients
        /// otherwise a ReplicationValidationException will be thrown on this device.
        /// </summary>
        public HostValidationHandler<T> OnHostValidate;
        public OwnedChangedHandler<T> OnValueChanged;
        public KeyValidationStatus KeyValidationStatus => _validationStatus;

        // todo throw exception if reassigning self. maybe not here?
        internal IUserPresence Self
        {
            get;
            set;
        }

        private KeyValidationStatus _validationStatus;
        private readonly Dictionary<string, T> _values = new Dictionary<string, T>();

        private readonly object _valueLock = new object();

        public void SetValue(T value, IUserPresence source, IUserPresence target)
        {
            SetValue(value, source, target, _validationStatus);
        }

        public void SetValue(T value, IUserPresence target)
        {
            SetValue(value, Self, target, _validationStatus);
        }

        public T GetValue()
        {
            return GetValue(Self);
        }

        public T GetValue(IUserPresence presence)
        {

            lock (_valueLock)
            {
                if (_values.ContainsKey(presence.UserId))
                {
                    return _values[presence.UserId];
                }
                else
                {
                    throw new InvalidOperationException($"Tried retrieving a replicated value from an unrecognized user id: {presence.UserId}");
                }
            }
        }

        internal void SetValue(T value, IUserPresence source, IUserPresence target, KeyValidationStatus validationStatus)
        {
            lock (_valueLock)
            {
                T oldValue = _values.ContainsKey(target.UserId) ? _values[target.UserId] : default(T);

                if (oldValue.Equals(value))
                {
                    return;
                }

                _values[target.UserId] = value;
                _validationStatus = validationStatus;

                OnValueChanged?.Invoke(oldValue, value, source, target);
            }
        }

        internal void Clear()
        {
            lock (_valueLock)
            {
                _values.Clear();
            }

            OnHostValidate = null;
            OnValueChanged = null;
            Self = null;
        }
    }
}
