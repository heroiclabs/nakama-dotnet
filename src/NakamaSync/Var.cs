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

        private VarValue<T> _lastValue;
        private VarValue<T> _value;
        private VarValue<T> _lastValid;
        private SyncMatch _syncMatch;
        private TaskCompletionSource<bool> _handshakeTcs = new TaskCompletionSource<bool>();

        public Var(long opcode)
        {
            _lastValue = new VarValue<T>();
            _value = new VarValue<T>();
            Opcode = opcode;
        }

        public T GetValue()
        {
            return _value.Value;
        }

        public int GetVersion()
        {
            return _value.Version;
        }

        internal virtual void Reset()
        {
            _value = new VarValue<T>();
            _lastValue = new VarValue<T>();

            _syncMatch = null;

            if (!_handshakeTcs.Task.IsCompleted)
            {
                                        System.Console.WriteLine("setting canceled from reset");

                _handshakeTcs.SetCanceled();
            }

            _handshakeTcs = new TaskCompletionSource<bool>();
            OnReset?.Invoke();
        }

        internal void SetValueViaSelf(T newValue)
        {
            _lastValue = _value;

            ValidationStatus oldStatus = this._value.ValidationStatus;
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
            _value = new VarValue<T> (_lastValue.Version + 1, _syncMatch?.Self, newStatus, newValue);

            var evt = new VarChangedEvent<T>(_lastValue, _value);
            OnValueChanged?.Invoke(evt);

            // normal case where var is set before sync match has started.
            if (_syncMatch != null)
            {
                Send(SerializableVar<T>.FromVarValue(_value, VarMessageType.DataTransfer, _syncMatch?.Self));
            }
        }

        internal Task ReceiveSyncMatch(SyncMatch syncMatch, int handshakeTimeoutSec)
        {
            _syncMatch = syncMatch;

            var self = syncMatch.PresenceTracker.GetSelf();

            // begin handshake process
            if (this._value.Value != null && !this._value.Value.Equals(default(T)))
            {
                // share value with other clients.
                Send(SerializableVar<T>.FromVarValue(_value, VarMessageType.DataTransfer, _syncMatch.Self));
            }
            else
            {
                // don't have value to share, request it from other clients. todo only request from host?
                Send(SerializableVar<T>.FromVarValue(_value, VarMessageType.HandshakeRequest, _syncMatch.Self));
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
            if (incomingSerialized.MessageType == VarMessageType.VersionConflict)
            {
                var serializedConflict = incomingSerialized.VersionConflict;
                var acceptedWrite = serializedConflict.AcceptedWrite.ToVarValue();
                var rejectedWrite = serializedConflict.RejectedWrite.ToVarValue();
                var conflict = new VersionConflict<T>(acceptedWrite, rejectedWrite);
                ReceivedVersionConflict(conflict);
                return;
            }

            if (incomingSerialized.MessageType == VarMessageType.HandshakeResponse)
            {
                _handshakeTcs.TrySetResult(true);
            }

            if (incomingSerialized.MessageType == VarMessageType.HandshakeRequest)
            {
                System.Console.WriteLine("sending handshake response " + _value.Value);
                var serializable = SerializableVar<T>.FromVarValue(_value, VarMessageType.HandshakeResponse, _syncMatch.Self);
                Send(serializable, new IUserPresence[]{source});
            }

            // ensure the lock versions match so this validation status isn't for a stale value
            if (incomingSerialized.ValidationStatus == ValidationStatus.Invalid && incomingSerialized.Version != _value.Version)
            {
                // rollback to serialized value
                _lastValue = _value;
                _value = _lastValid;
                OnValueChanged?.Invoke(new VarChangedEvent<T>(_lastValue, _value));
            }
            // handshake responses are not subject to lock version checks.
            // they are considered authoritative from the perspective of the joining client, at least in this iteration of the library.
            else if (incomingSerialized.Version > _value.Version ||
            incomingSerialized.MessageType == VarMessageType.HandshakeResponse ||
            _value.CanMergeWith(incomingSerialized.Value))
            {
                System.Console.WriteLine($"{_syncMatch.Self.UserId} setting from remote");
                SetValueViaRemote(source, incomingSerialized);
            }
            else if (incomingSerialized.MessageType == VarMessageType.DataTransfer)
            {
                System.Console.WriteLine("Detected version conflict " + incomingSerialized.MessageType);
                var rejectedWrite = incomingSerialized.ToVarValue();
                var conflict = new VersionConflict<T>(_value, rejectedWrite);
                DetectedVersionConflict(conflict);
            }
        }

        internal virtual void ReceivedVersionConflict(VersionConflict<T> conflict) {}
        internal virtual void DetectedVersionConflict(VersionConflict<T> conflict) {}

        private void SetValueViaRemote(UserPresence source, SerializableVar<T> incomingSerialized)
        {
            if (_syncMatch.HostTracker.IsSelfHost() && ValidationHandler != null)
            {
                ValidationStatus newStatus = Validate(incomingSerialized);
                incomingSerialized.ValidationStatus = newStatus;
                incomingSerialized.MessageType = VarMessageType.ValidationStatus;
                Send(incomingSerialized);
            }

            _lastValue = _value;

            if (_lastValue.ValidationStatus == ValidationStatus.Valid ||
                _lastValue.ValidationStatus == ValidationStatus.None)
            {
                // last valid value can have ValidationStatus.None in the case that a handler
                // wasn't added to the variable at the time the value was received.
                // todo we could also force the validation handler to immutable and passed via constructor
                // to avoid this type of edge case.
                _lastValid = _lastValue;
            }

            _value = incomingSerialized.ToVarValue();
            OnValueChanged?.Invoke(new VarChangedEvent<T>(_lastValue, _value));
        }

        private ValidationStatus Validate(SerializableVar<T> serialized)
        {
            ValidationStatus newStatus = serialized.ValidationStatus;

            var potentialValue = serialized.ToVarValue();
            var validationEvt = new VarChangedEvent<T>(_value, potentialValue);

            if (ValidationHandler.Invoke(validationEvt))
            {
                newStatus = ValidationStatus.Valid;
            }
            else
            {
                newStatus = ValidationStatus.Invalid;
            }

            return newStatus;
        }
    }
}
