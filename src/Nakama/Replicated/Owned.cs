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
    public delegate bool HostValidationHandler<T>(T oldValue, T newValue);
    public delegate void ValueChangedHandler<T>(T oldValue, T newValue);

    /// <summary>
    /// A variable whose value is synchronized across all clients connected to the same match.
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
        public ValueChangedHandler<T> OnValueChangedLocal;
        public ValueChangedHandler<T> OnValueChangedRemote;
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

        public void SetValue(IUserPresence presence, T value)
        {
            SetValue(presence, value, ReplicatedClientType.Local, _validationStatus);
        }

        public void SetValue(T value)
        {
            SetValue(Self, value, ReplicatedClientType.Local, _validationStatus);
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

        internal void SetValue(IUserPresence presence, T value, ReplicatedClientType source, KeyValidationStatus validationStatus)
        {
            lock (_valueLock)
            {
                T oldValue = _values.ContainsKey(presence.UserId) ? _values[presence.UserId] : default(T);

                if (oldValue.Equals(value))
                {
                    return;
                }

                _values[presence.UserId] = value;
                _validationStatus = validationStatus;

                switch (source)
                {
                    case ReplicatedClientType.Local:
                    OnValueChangedLocal?.Invoke(oldValue, value);
                    break;
                    default:
                    OnValueChangedRemote?.Invoke(oldValue, value);
                    break;
                }
            }
        }

        internal void Clear()
        {
            lock (_valueLock)
            {
                _values.Clear();
            }

            OnHostValidate = null;
            OnValueChangedLocal = null;
            OnValueChangedRemote = null;
            Self = null;
        }
    }
}
