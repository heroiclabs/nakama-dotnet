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

    public abstract class Var<T>
    {
        public event Action<IVarEvent<T>> OnValueChanged;
        public long Opcode { get; }
        public event ResetHandler OnReset;
        public ValidationHandler<T> ValidationHandler { get; set; }

        public ValidationStatus Status { get; private set; }
        protected T Value { get; private set; }
        protected SyncMatch SyncMatch => _syncMatch;

        private int _lockVersion;
        private SyncMatch _syncMatch;

        public Var(long opcode)
        {
            Opcode = opcode;
        }

        public T GetValue()
        {
            return Value;
        }

        internal virtual void Reset()
        {
            Value = default(T);
            Status = ValidationStatus.None;
            _syncMatch = null;
            OnReset();
        }

        internal void InvokeOnValueChanged(IVarEvent<T> e)
        {
            OnValueChanged?.Invoke(e);
        }

        internal void SetLocalValue(IUserPresence source, T newValue)
        {
            T oldValue = Value;
            Value = newValue;
            _lockVersion++;

            ValidateAndDispatch(source, oldValue, newValue);

                // defer synchronization until reception of sync match.
            if (_syncMatch != null)
            {
                Send();
                return;
            }



            System.Console.Write("old value " + oldValue);
            System.Console.Write("new value " + newValue);


        }

        internal void ReceiveSyncMatch(SyncMatch syncMatch)
        {
            _syncMatch = syncMatch;

            if (this.Value != null && !this.Value.Equals(default(T)))
            {
                ValidateAndDispatch(syncMatch.PresenceTracker.GetSelf(), Value, Value);
                Send();
            }
        }

        private void ValidateAndDispatch(IUserPresence source, T oldValue, T newValue)
        {
            ValidationStatus oldStatus = this.Status;

            if (_syncMatch != null && _syncMatch.HostTracker.IsSelfHost())
            {
                Status = ValidationHandler == null ? ValidationStatus.None : ValidationStatus.Valid;
            }
            else
            {
                // todo if this user becomes host let's move validation status to valid
                Status = ValidationHandler == null ? ValidationStatus.Pending : ValidationStatus.Valid;
            }

            var evt = new VarEvent<T>(source, new ValueChange<T>(oldValue, newValue), new ValidationChange(oldStatus, Status));

            InvokeOnValueChanged(evt);

            // todo should we create a separate event for validation changes or even throw this event if var changes to invalid?
            // todo also check that there is an actual change!!! think about how to do for collections
        }

        private void Send()
        {
            _syncMatch.Socket.SendMatchStateAsync(_syncMatch.Id, Opcode, _syncMatch.Encoding.Encode(ToSerializable(isAck: false)));

        }

        internal void HandleSerialized(IUserPresence source, SerializableVar<T> incomingSerialized)
        {
            if (_lockVersion > incomingSerialized.LockVersion)
            {
                // expected race to occur
                return;
            }

            ValidationStatus oldStatus = Status;
            T oldValue = Value;

            ValidationStatus newStatus = TryHostIntercept(source, incomingSerialized);


            if (newStatus != ValidationStatus.Invalid)
            {
                Value = incomingSerialized.Value;
                _lockVersion = incomingSerialized.LockVersion;
                Status = incomingSerialized.Status;
                InvokeOnValueChanged(new VarEvent<T>(source, new ValueChange<T>(oldValue, Value), new ValidationChange(oldStatus, Status)));
            }

            // todo notify if invalid?
        }

        private ValidationStatus TryHostIntercept(IUserPresence source, ISerializableVar<T> serialized)
        {
            ValidationStatus newStatus = serialized.Status;

            if (_syncMatch.HostTracker.IsSelfHost())
            {
                if (ValidationHandler != null)
                {
                    ISerializableVar<T> responseSerialized = null;

                    if (ValidationHandler.Invoke(source, new ValueChange<T>(Value, serialized.Value)))
                    {
                        responseSerialized = serialized;
                        responseSerialized.IsAck = true;
                        newStatus = ValidationStatus.Valid;
                    }
                    else
                    {
                        responseSerialized = ToSerializable(isAck: true);
                        newStatus = ValidationStatus.Invalid;
                    }

                    responseSerialized.Status = newStatus;
                    responseSerialized.IsAck = true;
                    _syncMatch.Socket.SendMatchStateAsync(_syncMatch.Id, Opcode,  _syncMatch.Encoding.Encode(responseSerialized));
                }
            }

            return newStatus;
        }

        internal ISerializableVar<T> ToSerializable(bool isAck)
        {
            return new SerializableVar<T>{Value = Value, LockVersion = _lockVersion, Status = Status, IsAck = isAck};
        }
    }
}
