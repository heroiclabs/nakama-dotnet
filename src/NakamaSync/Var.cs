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
using System.Threading.Tasks;
using Nakama;

namespace NakamaSync
{
    public delegate void ResetHandler();
    public delegate bool ValidationHandler<T>(IUserPresence source, IValueChange<T> change);

    public abstract class Var<T> : IVar
    {
        public event Action<IVarEvent<T>> OnValueChanged;
        public string Key { get; }
        public event ResetHandler OnReset;
        public ValidationHandler<T> ValidationHandler { get; set; }

        // todo rename to validation status
        public ValidationStatus Status { get; private set; }

        protected IUserPresence Self => _matchState.Match.Self;

        private VarMatchState _matchState;
        private VarSharedMatchState<T> _sharedState;

        void IVar.Initialize(ISocket socket, ISession session)
        {

        }

        private ValidationStatus GetNewValidationStatus(IUserPresence source, T oldValue, T newValue)
        {
            if (!_matchState.HostTracker.IsSelfHost())
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

        private VarUserState<T> _userState;
        protected T Value { get; set; }
        public ValidationStatus ValidationStatus { get; internal set; }

        public Var(string key)
        {
            Key = key;
            _userState = new VarUserState<T>(key);
        }

        public T GetValue()
        {
            return Value;
        }

        protected virtual void Reset()
        {
            _userState = new VarUserState<T>(Key);
            OnReset();
        }

        internal void InvokeOnValueChanged(IVarEvent<T> e)
        {
            OnValueChanged?.Invoke(e);
        }

        internal void SetValue(IUserPresence source, T value)
        {
            T oldValue = value;
            T newValue = ConvertToNewValue(value);

            ValidationStatus oldStatus = this.ValidationStatus;
            ValidationStatus newStatus = GetNewValidationStatus(source, oldValue, newValue);

            Status = newStatus;

            if (newStatus != NakamaSync.ValidationStatus.Invalid)
            {
                value = newValue;
                // todo should we create a separate event for validation changes or even throw this event if var changes to invalid?
                InvokeOnValueChanged(new VarEvent<T>(source, new ValueChange<T>(oldValue, newValue), new ValidationChange(oldStatus, newStatus)));
            }
        }

        protected virtual T ConvertToNewValue(object newValue)
        {
            return (T) newValue;
        }

        public Task GetHandshakeTask()
        {
            return _sharedState.HandshakeResponseHandler.GetHandshakeTask();
        }

        void IVar.ReceiveMatch(VarMatchState matchState)
        {
            _matchState = matchState;
        }
    }
}
