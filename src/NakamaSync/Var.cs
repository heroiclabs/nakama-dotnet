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
using System.Threading;
using System.Threading.Tasks;
using Nakama;

namespace NakamaSync
{
    public delegate void ResetHandler();
    public delegate bool ValidationHandler<T>(IVarChangedEvent<T> evt);
    public delegate void ValueChangedHandler<T>(IVarChangedEvent<T> evt);
    public delegate void VersionConflictHandler<T>(IVersionConflict<T> conflict);

    public abstract class Var<T>
    {
        /// <summary>
        /// An event dispatched when the variable is reset, typically after a match has ended.
        /// </summary>
        public event ResetHandler OnReset;

        /// <summary>
        /// An event dispatched when the variable's value or validation status changes.
        /// </summary>
        public event ValueChangedHandler<T> OnValueChanged;

        /// <summary>
        /// A callback to be invoked whenever this user is the host and a potential value requires validation.
        /// If the callback returns true, the value is valid and accepted. Otherwise, the value is rejected and
        /// this client rolls back to the last valid value.
        ///
        /// In both cases, the resulting validation status is broadcast to all other clients in the match.
        /// </summary>
        public ValidationHandler<T> ValidationHandler { get; set; }
        public long Opcode { get; }

        /// <summary>
        /// A task representing the initial sharing of state, i.e., "handshake", between this
        /// variable and other variables.
        ///
        /// In cases where this variable was registered prior to the match starting, this task
        /// will be internally awaited by <see cref="SocketExtensions.JoinSyncMatch"/>.
        ///
        /// This property is primarily inteded for use for variables registered after match join, i.e.,
        /// "deferred registration."
        /// </summary>
        public Task HandshakeTask => _handshakeTcs.Task;

        protected ISyncMatch SyncMatch => _syncMatch;
        protected IVarValue<T> LastValue { get; set; }
        protected IVarValue<T> Value { get; set; }
        private IVarValue<T> _lastValid;

        private SyncMatch _syncMatch;
        private TaskCompletionSource<bool> _handshakeTcs = new TaskCompletionSource<bool>();

        public Var(long opcode)
        {
            LastValue = new VarValue<T>();
            Value = new VarValue<T>();

            Opcode = opcode;
        }

        public T GetValue()
        {
            return Value.Value;
        }

        internal virtual void Reset()
        {
            Value = new VarValue<T>();
            LastValue = new VarValue<T>();

            _syncMatch = null;

            if (!_handshakeTcs.Task.IsCompleted)
            {
                _handshakeTcs.SetCanceled();
            }

            _handshakeTcs = new TaskCompletionSource<bool>();
            OnReset?.Invoke();
        }

        internal void SetValueViaSelf(T newValue)
        {
            LastValue = Value;

            ValidationStatus oldStatus = this.Value.ValidationStatus;
            ValidationStatus newStatus;

            // are we doing deferred registration, or is current user hostt?
            if (_syncMatch != null && _syncMatch.HostTracker.IsSelfHost())
            {
                newStatus = ValidationHandler == null ? ValidationStatus.None : ValidationStatus.Valid;
            }
            else
            {
                newStatus = ValidationHandler == null ? ValidationStatus.None : ValidationStatus.Pending;
            }

            // todo are we handling a null source presence in other places in the code?
            // sync match will be null here sometimes.
            Value = new VarValue<T> (LastValue.Version + 1, SyncMatch?.Self, newStatus, newValue);

            var evt = new VarChangedEvent<T>(LastValue, Value);
            OnValueChanged?.Invoke(evt);

            // normal case where var is set before sync match has started.
            if (_syncMatch != null)
            {
                Send(new SerializableVar<T>{Value = Value.Value, Status = Value.ValidationStatus, AckType = VarMessageType.None, Version = Value.Version});
            }
        }

        internal Task ReceiveSyncMatch(SyncMatch syncMatch, int handshakeTimeoutSec)
        {
            _syncMatch = syncMatch;

            var self = syncMatch.PresenceTracker.GetSelf();

            // begin handshake process
            if (this.Value.Value != null && !this.Value.Value.Equals(default(T)))
            {
                // share value with other clients
                Send(new SerializableVar<T>{Value = Value.Value, Status = Value.ValidationStatus, AckType = VarMessageType.None, Version = Value.Version});
            }
            else
            {
                // don't have value to share, request it from other clients.
                Send(new SerializableVar<T>{Value = Value.Value, Status = Value.ValidationStatus, AckType = VarMessageType.HandshakeRequest, Version = Value.Version});
            }

            // not match creator
            // todo hack on self var type check.
            // self vars should not expect a handshake because only this client can write to them anyway.
            if (!(this is SelfVar<T>) && syncMatch.Presences.Any(user => user.UserId != syncMatch.Self.UserId))
            {
                var timeoutCts = new CancellationTokenSource();
                timeoutCts.Token.Register(() =>
                {
                    if (!_handshakeTcs.Task.IsCompleted)
                    {
                        _handshakeTcs.SetCanceled();
                    }
                });

                timeoutCts.CancelAfter(TimeSpan.FromSeconds(handshakeTimeoutSec));

                return _handshakeTcs.Task;
            }

            return Task.CompletedTask;
        }

        internal void Send(SerializableVar<T> serializable, IUserPresence[] presences = null)
        {
            _syncMatch.Socket.SendMatchStateAsync(_syncMatch.Id, Opcode, _syncMatch.Encoding.Encode(serializable), presences);
        }

        internal void ReceiveSerialized(UserPresence source, SerializableVar<T> incomingSerialized)
        {

            if (incomingSerialized.AckType == VarMessageType.VersionConflict)
            {
                HandleVersionConflict(incomingSerialized.VersionConflict);
                return;
            }

            if (incomingSerialized.AckType == VarMessageType.HandshakeResponse)
            {
                _handshakeTcs.TrySetResult(true);
            }

            if (incomingSerialized.AckType == VarMessageType.HandshakeRequest)
            {
                Send(new SerializableVar<T>{Value = Value.Value, Status = Value.ValidationStatus, AckType = VarMessageType.HandshakeResponse, Version = Value.Version}, new IUserPresence[]{source});
            }

            // ensure the lock versions match so this validation status isn't for a stale value
            if (incomingSerialized.Status == ValidationStatus.Invalid && incomingSerialized.Version != Value.Version)
            {
                // rollback to serialized value
                LastValue = Value;
                Value = _lastValid;
                OnValueChanged?.Invoke(new VarChangedEvent<T>(LastValue, Value));
            }
            else if (incomingSerialized.Version > Value.Version)
            {
                SetValueViaRemote(source, incomingSerialized);
            }
            else
            {
                var rejectedWrite = new VarValue<T>(incomingSerialized.Version, source, incomingSerialized.Status, incomingSerialized.Value);
                var conflict = new VersionConflict<T>(rejectedWrite, Value);
                HandleVersionConflict(conflict);
            }
        }

        internal virtual void HandleVersionConflict(VersionConflict<T> conflict)
        {

        }

        private void SetValueViaRemote(IUserPresence source, SerializableVar<T> incomingSerialized)
        {
            if (_syncMatch.HostTracker.IsSelfHost())
            {
                ValidationStatus newStatus = Validate(source, incomingSerialized);
                incomingSerialized.Status = newStatus;
                incomingSerialized.AckType = VarMessageType.ValidationStatus;
                Send(incomingSerialized);
            }

            LastValue = Value;

            if (LastValue.ValidationStatus == ValidationStatus.Valid || LastValue.ValidationStatus == ValidationStatus.None)
            {
                // last valid value can have ValidationStatus.None in the case that a handler
                // wasn't added to the variable at the time the value was received.
                // todo we could also force the validation handler to immutable and passed via constructor
                // to avoid this type of edge case.
                _lastValid = LastValue;
            }

            Value = new VarValue<T>(incomingSerialized.Version, source, incomingSerialized.Status, incomingSerialized.Value);
            OnValueChanged?.Invoke(new VarChangedEvent<T>(LastValue, Value));
        }

        private ValidationStatus Validate(IUserPresence source, ISerializableVar<T> serialized)
        {
            ValidationStatus newStatus = serialized.Status;

            if (ValidationHandler != null)
            {
                var potentialValue = new VarValue<T>(serialized.Version, source, newStatus, serialized.Value);
                var validationEvt = new VarChangedEvent<T>(Value, potentialValue);
                if (ValidationHandler != null && ValidationHandler.Invoke(validationEvt))
                {
                    newStatus = ValidationStatus.Valid;
                }
                else
                {
                    newStatus = ValidationStatus.Invalid;
                }
            }

            return newStatus;
        }
    }
}
