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
    public class UserVar<T> : IVar
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

        public ValidationStatus validationStatus => _validationStatus;

        IUserPresence IVar.Self
        {
            get => _self;
            set => _self = value;
        }

        private IUserPresence _self;

        internal IReadOnlyDictionary<string, T> Values => _values;

        private ValidationStatus _validationStatus;

        private readonly Dictionary<string, T> _values = new Dictionary<string, T>();

        public void SetValue(T value)
        {
            SetValue(value, _self, _self.UserId, _validationStatus, OnLocalValueChanged);
        }

        public T GetValue()
        {
            return GetValue(_self);
        }

        public bool HasValue(IUserPresence presence)
        {
            return _values.ContainsKey(presence.UserId);
        }

        public T GetValue(IUserPresence presence)
        {
            return GetValue(presence.UserId);
        }

        internal T GetValue(string presenceId)
        {
            if (_values.ContainsKey(presenceId))
            {
                return _values[presenceId];
            }
            else
            {
                throw new InvalidOperationException($"Tried retrieving a synced value from an unrecognized user id: {presenceId}");
            }
        }

        internal void SetValue(T value, IUserPresence source, string targetId, ValidationStatus validationStatus, Action<UserVarEvent<T>> eventDispatch)
        {
            T oldValue = _values.ContainsKey(targetId) ? _values[targetId] : default(T);

            if (oldValue != null && oldValue.Equals(value))
            {
                return;
            }

            _values[targetId] = value;
            _validationStatus = validationStatus;

            eventDispatch?.Invoke(new UserVarEvent<T>(source, targetId, oldValue, value));
        }

        ValidationStatus IVar.GetValidationStatus()
        {
            return _validationStatus;
        }

        void IVar.Reset()
        {
            _values.Clear();

            OnHostValidate = null;
            OnLocalValueChanged = null;
            OnRemoteValueChanged = null;
            _self = null;
        }

        void IVar.SetValidationStatus(ValidationStatus status)
        {
            _validationStatus = status;
        }
    }
}
