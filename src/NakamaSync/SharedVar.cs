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
    /// <summary>
    /// A variable whose single value is synchronized across all clients connected to the same match.
    /// </summary>
    public class SharedVar<T>
    {
        /// <summary>
        /// If this delegate is set and the current client is a guest, then
        /// when a synced value is set, this client will reach out to the
        /// host who will validate and if it's validated the host will send to all clients
        /// otherwise a ReplicationValidationException will be thrown on this device.
        /// </summary>
        public Action<ISharedVarEvent<T>> OnHostValidate;
        public Action<ISharedVarEvent<T>> OnRemoteValueChanged;
        internal Action<ISharedVarEvent<T>> OnLocalValueChanged;
        public KeyValidationStatus KeyValidationStatus => _validationStatus;

        internal IUserPresence Self
        {
            get;
            set;
        }

        private KeyValidationStatus _validationStatus;
        private T _value;
        private readonly object _valueLock = new object();


        public void SetValue(T value)
        {
            SetValue(value, Self, _validationStatus, OnLocalValueChanged);
        }

        public T GetValue()
        {
            lock (_valueLock)
            {
                return _value;
            }
        }

        internal void SetValue(T value, IUserPresence presence, KeyValidationStatus validationStatus, Action<SharedVarEvent<T>> eventDispatch)
        {
            lock (_valueLock)
            {
                T oldValue = _value;

                if (oldValue.Equals(value))
                {
                    return;
                }

                _value = value;
                _validationStatus = validationStatus;
                eventDispatch?.Invoke(new SharedVarEvent<T>(presence, oldValue, value));
            }
        }

        // todo this should not be public!
        public void Reset()
        {
            lock (_valueLock)
            {
                _value = default(T);
            }

            OnHostValidate = null;
            OnLocalValueChanged = null;
            OnRemoteValueChanged = null;
        }
    }
}