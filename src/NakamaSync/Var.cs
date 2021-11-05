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
    public delegate void ResetHandler();
    public delegate bool ValidationHandler<T>(IUserPresence source, IValueChange<T> change);

    public abstract class Var<T> : IIncomingVar<T>
    {
        public event Action<IVarEvent<T>> OnValueChanged;

        ValidationHandler<T> ValidationHandler { get; set; }

        public event ResetHandler OnReset;

        public string Key { get; }

        internal IUserPresence Self
        {
            get;
            set;
        }

        IUserPresence IVar.Self
        {
            get => Self;
            set => Self = value;
        }

        public ValidationStatus ValidationStatus
        {
            get;
            protected set;
        }

        public bool IsHost
        {
            get
            {
                return (this as IVar).IsHost;
            }
            protected set
            {
                (this as IVar).IsHost = value;
            }
        }

        bool IVar.IsHost
        {
            get;
            set;
        }

        internal ILogger Logger
        {
            get;
            set;
        }

        ILogger IVar.Logger
        {
            get => Logger;
            set => Logger = value;
        }

        protected T _value;

        public T GetValue()
        {
            return _value;
        }

        public Var(string key)
        {
            Key = key;
        }

        internal abstract void Reset();

        void IVar.Reset()
        {
            OnValueChanged = null;
            ValidationHandler = null;
            this.Reset();
            OnReset();
        }

        void IVar.SetValidationStatus(ValidationStatus status)
        {
            ValidationStatus = status;
        }

        protected void InvokeOnValueChanged(IVarEvent<T> e)
        {
            OnValueChanged?.Invoke(e);
        }

        bool IIncomingVar<T>.SetValue(IUserPresence source, object value, ValidationStatus validationStatus)
        {
            Logger?.DebugFormat($"Setting shared value. Source: {source.UserId}, Value: {value}");

            T oldValue = _value;
            ValidationStatus oldStatus = ValidationStatus;

            bool success = ValidationHandler == null ||
                            ValidationHandler.Invoke(source, new ValueChange<T>(oldValue, (T) value));

            if (success)
            {
                ValidationStatus = ValidationHandler == null ? ValidationStatus.None : ValidationStatus.Invalid; // how/when do we toggle this back to valid
                _value = (T) value;
                ValidationStatus = validationStatus;

                var valueChange = new ValueChange<T>(oldValue, (T) value);
                var statusChange = new ValidationChange(oldStatus, ValidationStatus);
                InvokeOnValueChanged(new VarEvent<T>(source, valueChange, statusChange));
            }

            return success;
        }
    }

    public class VarEvent<T> : IVarEvent<T>
    {
        public IUserPresence Source { get; }
        public IValueChange<T> ValueChange { get; }
        public IValidationChange ValidationChange { get; }

        public VarEvent(IUserPresence source, IValueChange<T> valueChange, IValidationChange validationChange)
        {
            Source = source;
            ValueChange = valueChange;
            ValidationChange = validationChange;
        }
    }
}
