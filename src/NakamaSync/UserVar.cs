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
using Nakama;

namespace NakamaSync
{
    /// <summary>
    /// A variable containing a value for each user in the match. Each value is synchronized across all users.
    /// </summary>
    public class UserVar<T> : ISyncVar
    {
        /// <summary>
        /// If this delegate is set and the current client is a guest, then
        /// when a synced value is set, this client will reach out to the
        /// host who will validate and if it's validated the host will send to all clients
        /// otherwise a ReplicationValidationException will be thrown on this device.
        /// </summary>
        public Func<IUserVarEvent<T>, bool> OnHostValidate;
        public Action<IUserVarEvent<T>> OnRemoteValueChanged;
        internal Action<IUserVarEvent<T>> OnLocalValueChanged;

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
            SetValue(value, source, target, _validationStatus, OnLocalValueChanged);
        }

        public void SetValue(T value, IUserPresence target)
        {
            SetValue(value, Self, target, _validationStatus, OnLocalValueChanged);
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
                    throw new InvalidOperationException($"Tried retrieving a synced value from an unrecognized user id: {presence.UserId}");
                }
            }
        }

        internal void SetValue(T value, IUserPresence source, IUserPresence target, KeyValidationStatus validationStatus, Action<UserVarEvent<T>> eventDispatch)
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

                eventDispatch?.Invoke(new UserVarEvent<T>(source, target, oldValue, value));
            }
        }

        // TODO this should not be public!!!
        public void Reset()
        {
            lock (_valueLock)
            {
                _values.Clear();
            }

            OnHostValidate = null;
            OnLocalValueChanged = null;
            OnRemoteValueChanged = null;
            Self = null;
        }
    }
}
