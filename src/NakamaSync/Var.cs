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

    public abstract class Var<T> : IVar<T>
    {
        public event Action<IVarEvent<T>> OnValueChanged;

        public ValidationHandler<T> ValidationHandler { get; set; }

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

        HostTracker IVar.HostTracker
        {
            get;
            set;
        }

        protected T _value;

        public Var(string key)
        {
            Key = key;
        }

        void IVar.SetValidationStatus(ValidationStatus status)
        {
            ValidationStatus = status;
        }

        public T GetValue()
        {
            return _value;
        }

        internal abstract void Reset();

        void IVar.Reset()
        {
            OnValueChanged = null;
            ValidationHandler = null;
            this.Reset();
            OnReset();
        }

        internal void InvokeOnValueChanged(IVarEvent<T> e)
        {
            OnValueChanged?.Invoke(e);
        }

        void IVar<T>.SetValue(IUserPresence source, T value)
        {
            T oldValue = _value;
            T newValue = ConvertToNewValue(value);

            ValidationStatus oldStatus = ValidationStatus;
            ValidationStatus newStatus = GetNewValidationStatus(source, oldValue, newValue);

            ValidationStatus = newStatus;

            if (newStatus != ValidationStatus.Invalid)
            {
                _value = newValue;
                // todo should we create a separate event for validation changes or even throw this event if var changes to invalid?
                InvokeOnValueChanged(new VarEvent<T>(source, new ValueChange<T>(oldValue, newValue), new ValidationChange(oldStatus, newStatus)));
            }
        }

        protected abstract T ConvertToNewValue(object newValue);

        private ValidationStatus GetNewValidationStatus(IUserPresence source, T oldValue, T newValue)
        {
            if (!(this as IVar).HostTracker.IsSelfHost())
            {
                return ValidationStatus.Pending;
            }

            if (ValidationHandler == null)
            {
                return ValidationStatus.None;
            }

            if (ValidationHandler.Invoke(source, new ValueChange<T>(oldValue, (T) newValue)))
            {
                return ValidationStatus.Valid;
            }
            else
            {
                return ValidationStatus.Invalid;
            }
        }
    }

    public abstract class ObjectVar<T> : Var<T>, IVar<T>
    {
        public ObjectVar(string key) : base(key) {}

        protected override T ConvertToNewValue(object newValue)
        {
            return (T) newValue;
        }
    }
/*
    public abstract class DictionaryVar<T, K> : Var<Dictionary<T, K>>
    {
        public DictionaryVar(string key) : base(key) {}

        protected override Dictionary<T, K> ConvertToNewValue(object newValue)
        {
            return (T) newValue;
        }
    }*/
}
