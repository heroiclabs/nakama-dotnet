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

namespace NakamaSync
{
    /// <summary>
    /// A variable whose single value is synchronized across all clients connected to the same match.
    /// </summary>
    public class SelfVar<T> : Var<T>
    {
        public event Action<ISelfVarEvent<T>> OnValueChanged;

        public void SetValue(T value)
        {
            SetValue(value, _validationStatus);
        }

        internal override void Reset()
        {
            Self = null;
            _value = default(T);
            ValidationHandler = null;
            _validationStatus = ValidationStatus.None;
            IsHost = false;
            OnValueChanged = null;
        }

        internal void SetValue(T value, ValidationStatus validationStatus)
        {
            T oldValue = _value;
            _value = value;

            ValidationStatus oldStatus = _validationStatus;
            _validationStatus = validationStatus;

            var valueChange = new ValueChange<T>(oldValue, value);
            var statusChange = new ValidationChange(oldStatus, _validationStatus);

            OnValueChanged?.Invoke(new SelfVarEvent<T>(this, valueChange, statusChange));
        }
    }
}
