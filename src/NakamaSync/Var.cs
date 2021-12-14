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

        private readonly LocalValues<T> _values = new LocalValues<T>();
        private SyncMatch _syncMatch;
        private TaskCompletionSource<bool> _handshakeTcs = new TaskCompletionSource<bool>();

        public Var(long opcode)
        {
            Opcode = opcode;
        }

        public T GetValue()
        {
            return _values.CurrentValue.Value;
        }

        public int GetVersion()
        {
            return _values.CurrentValue.Version;
        }

        internal virtual void Reset()
        {
            _values.Reset();

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
            ValidationStatus oldStatus = this._values.CurrentValue.ValidationStatus;
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
            _values.SetNew(new VarValue<T> (_values.CurrentValue.Version + 1, _syncMatch?.Self, newStatus, newValue));

            var evt = new VarChangedEvent<T>(_values.LastValue, _values.CurrentValue);
            OnValueChanged?.Invoke(evt);

            // normal case where var is set before sync match has started.
            if (_syncMatch != null && _syncMatch.Presences.Any())
            {
                Send(SerializableVar<T>.FromVarValue(_values.CurrentValue, VarMessageType.DataTransfer, _syncMatch?.Self));
            }
        }

        internal Task ReceiveSyncMatch(SyncMatch syncMatch, int handshakeTimeoutSec)
        {
            System.Console.WriteLine("receive sync match called for " +syncMatch.Self.UserId + " " + Opcode);
            _syncMatch = syncMatch;

            if (syncMatch.Presences.Any())
            {
                // begin handshake process
                if (HasHandshakeResponse())
                {
                    // share value with other clients.
                    Send(SerializableVar<T>.FromVarValue(_values.CurrentValue, VarMessageType.DataTransfer, _syncMatch.Self));
                }
                else
                {
                    // don't have value to share, request it from other clients. todo only request from host?
                    Send(SerializableVar<T>.FromVarValue(_values.CurrentValue, VarMessageType.HandshakeRequest, _syncMatch.Self));
                }
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
            System.Console.WriteLine("client " + source.UserId + " received type " + incomingSerialized.MessageType);
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

            if (incomingSerialized.MessageType == VarMessageType.HandshakeRequest && HasHandshakeResponse())
            {
                var serializable = SerializableVar<T>.FromVarValue(_values.CurrentValue, VarMessageType.HandshakeResponse, _syncMatch.Self);
                Send(serializable, new IUserPresence[]{source});
            }

            // ensure the lock versions match so this validation status isn't for a stale value
            if (incomingSerialized.ValidationStatus == ValidationStatus.Invalid && incomingSerialized.Version != _values.CurrentValue.Version)
            {
                // rollback to serialized value
                _values.Rollback();
                OnValueChanged?.Invoke(new VarChangedEvent<T>(_values.LastValidValue, _values.CurrentValue));
            }
            // handshake responses are not subject to lock version checks.
            // they are considered authoritative from the perspective of the joining client, at least in this iteration of the library.
            else if (incomingSerialized.Version > _values.CurrentValue.Version ||
            incomingSerialized.MessageType == VarMessageType.HandshakeResponse ||
            _values.CurrentValue.CanMerge(incomingSerialized.Value))
            {
                System.Console.WriteLine($"{_syncMatch.Self.UserId} setting from remote");
                SetValueViaRemote(source, incomingSerialized);
            }
            else if (incomingSerialized.MessageType == VarMessageType.DataTransfer)
            {
                System.Console.WriteLine("Detected version conflict " + incomingSerialized.MessageType);
                var rejectedWrite = incomingSerialized.ToVarValue();
                var conflict = new VersionConflict<T>(_values.CurrentValue, rejectedWrite);
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

            _values.SetNew(incomingSerialized.ToVarValue());
            OnValueChanged?.Invoke(new VarChangedEvent<T>(_values.LastValue, _values.CurrentValue));
        }

        private ValidationStatus Validate(SerializableVar<T> serialized)
        {
            ValidationStatus newStatus = serialized.ValidationStatus;

            var potentialValue = serialized.ToVarValue();
            var validationEvt = new VarChangedEvent<T>(_values.CurrentValue, potentialValue);

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

        // todo what if a client explicitly sets their value to nothing and wants this to be authoratitve?
        // is this issue solved by just having the host be the handshake responder?
        private bool HasHandshakeResponse()
        {
            return this._values.CurrentValue.Value != null && !this._values.CurrentValue.Value.Equals(default(T));
        }
    }
}
