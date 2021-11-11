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
using System.Linq;
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

        public ValidationStatus Status { get; private set; }
        protected T Value { get; private set; }

        internal VarConnection<T> Connection { get; }

        private VarConnection<T> _connection;
        private int _lockVersion;

        public Var(string key)
        {
            Key = key;
        }

        public T GetValue()
        {
            return Value;
        }

        protected virtual void Reset()
        {
            Value = default(T);
            Status = ValidationStatus.None;
            OnReset();
        }

        internal void InvokeOnValueChanged(IVarEvent<T> e)
        {
            OnValueChanged?.Invoke(e);
        }

        internal void SetLocalValue(IUserPresence source, T value)
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("Cannot set a synchronized var before establishing a connection.");
            }

            T oldValue = Value;
            T newValue = value;

            ValidationStatus oldStatus = this.Status;

            if (_connection.HostTracker.IsSelfHost())
            {
                Status = ValidationHandler == null ? ValidationStatus.None : ValidationStatus.Valid;
            }
            else
            {
                Status = ValidationHandler == null ? ValidationStatus.Pending : ValidationStatus.Valid;
            }

            _lockVersion++;
            _connection.SendSyncDataToAll(ToSerializable(isAck: false));

            var evt = new VarEvent<T>(source, new ValueChange<T>(oldValue, newValue), new ValidationChange(oldStatus, Status));

            // todo should we create a separate event for validation changes or even throw this event if var changes to invalid?
            InvokeOnValueChanged(evt);
        }

        internal void ReceiveConnection(VarConnection<T> connection)
        {
            _connection = connection;

            // todo remove these handlers.
            connection.OnHandshakeRequest += HandleHandshakeRequest;
            connection.OnHandshakeSuccess += HandleSyncEnvelope;

            // no need to handle handshake failure because the task returned to the user will surface
            // the exception. TODO maybe handle by resetting any variable state?
            if (_connection.Match.Presences.Any())
            {
                _connection.SendHandshakeRequest(new HandshakeRequest(Key), _connection.Match.Presences.First());
            }
        }

        private void HandleSyncEnvelope(IUserPresence source, ISerializableVar<T> incomingSerialized)
        {
            if (_lockVersion >= incomingSerialized.LockVersion)
            {
                // expected race to occur
                return;
            }

            ValidationStatus newStatus = TryHostIntercept(source, incomingSerialized);

            if (newStatus != ValidationStatus.Invalid)
            {
                this.ReceiveSerializable(incomingSerialized);
            }
        }

        private ValidationStatus TryHostIntercept(IUserPresence source, ISerializableVar<T> serialized)
        {
            ValidationStatus newStatus = serialized.Status;

            if (_connection.HostTracker.IsSelfHost())
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
                    _connection.SendSyncDataToAll(responseSerialized);
                }
            }

            return newStatus;
        }

        private void HandleHandshakeRequest(IUserPresence source, HandshakeRequest request)
        {
            // todo this doesn't really make sense
            bool success = Key == request.Key;

            if (success)
            {
                var asSerializable = ToSerializable(isAck: true);
                var response = new HandshakeResponse<T>(asSerializable, success);
                _connection.SendHandshakeResponse(response, source);
            }

        }

        internal virtual ISerializableVar<T> ToSerializable(bool isAck)
        {
            return new SerializableVar<T>{Value = Value, LockVersion = _lockVersion, Status = Status, IsAck = isAck, Key = Key};
        }

        internal void ReceiveSerializable(ISerializableVar<T> serialized)
        {
            Value = serialized.Value;
            _lockVersion = serialized.LockVersion;
            Status = serialized.Status;
        }

        Task IVar.GetPendingHandshake()
        {
            return _connection.GetPendingHandshake();
        }
    }
}
