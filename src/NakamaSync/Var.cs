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
    public delegate bool ValidationHandler<T>(IUserPresence source, IValueChange<T> change);

    public abstract class Var<T>
    {
        public event Action<IVarEvent<T>> OnValueChanged;
        public event ResetHandler OnReset;

        public ValidationHandler<T> ValidationHandler { get; set; }
        public long Opcode { get; }
        public ValidationStatus Status { get; private set; }

        internal bool HasSyncMatch => _syncMatch != null;

        protected T Value { get; private set; }
        protected SyncMatch SyncMatch => _syncMatch;

        private int _lockVersion;
        private SyncMatch _syncMatch;
        private TaskCompletionSource<bool> _handshakeTcs = new TaskCompletionSource<bool>();

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

            if (!_handshakeTcs.Task.IsCanceled)
            {
                _handshakeTcs.SetCanceled();
            }

            _handshakeTcs = new TaskCompletionSource<bool>();
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

            // don't increment lock version if haven't done handshake yet.
            // remote clients should overwrite any local value during the handshake.
            if (_syncMatch != null)
            {
                _lockVersion++;
            }

            ValidateAndDispatch(source, oldValue, newValue);

            // defer synchronization until reception of sync match.
            if (_syncMatch != null)
            {
                Send(AckType.None);
                return;
            }
        }

        internal Task ReceiveSyncMatch(SyncMatch syncMatch, int handshakeTimeoutSec)
        {
            _syncMatch = syncMatch;

            if (this.Value != null && !this.Value.Equals(default(T)))
            {
                ValidateAndDispatch(syncMatch.PresenceTracker.GetSelf(), Value, Value);
                Send(AckType.None);
            }

            syncMatch.PresenceTracker.OnPresenceAdded += (p) =>
            {
                if (p.UserId == syncMatch.PresenceTracker.GetSelf().UserId)
                {
                    return;
                }
                // share state with the new user
                // note that in the case of a shared var, last lock version will win
                // when all clients try to greet the new one.
                // todo put this into its own method and virtualize it instead
                // of doing a type check?
                Send(AckType.Handshake, new IUserPresence[]{p});
            };

            // not match creator
            // todo hack on self var type check
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

        private void Send(AckType ackType, IUserPresence[] presences = null)
        {
            _syncMatch.Socket.SendMatchStateAsync(_syncMatch.Id, Opcode, _syncMatch.Encoding.Encode(ToSerializable(ackType)), presences);
        }

        internal void ReceiveSerialized(IUserPresence source, SerializableVar<T> incomingSerialized)
        {
            if (_lockVersion > incomingSerialized.LockVersion)
            {
                // expected race to occur
                return;
            }

            // complete the task vefore invoking a value changed.
            // we want the top level user-facing match task to end
            // prior to the event dispatching. this is so they can
            // handle any match received events prior to handling specific var events.
            if (incomingSerialized.AckType == AckType.Handshake)
            {
                // if multiple users in match, they will all send handshake responses.
                // complete the task after the first handshake and no-op the remaining.
                _handshakeTcs.TrySetResult(true);
            }

            ValidationStatus oldStatus = Status;
            T oldValue = Value;

            ValidationStatus newStatus = TryHostIntercept(source, incomingSerialized);

            // todo notify if invalid?
            if (newStatus != ValidationStatus.Invalid)
            {
                Value = incomingSerialized.Value;
                _lockVersion = incomingSerialized.LockVersion;
                Status = incomingSerialized.Status;
                InvokeOnValueChanged(new VarEvent<T>(source, new ValueChange<T>(oldValue, Value), new ValidationChange(oldStatus, Status)));
            }

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
                        responseSerialized.AckType = AckType.Validation;
                        newStatus = ValidationStatus.Valid;
                    }
                    else
                    {
                        responseSerialized = ToSerializable(AckType.Validation);
                        newStatus = ValidationStatus.Invalid;
                    }

                    responseSerialized.Status = newStatus;
                    responseSerialized.AckType = AckType.Validation;
                    _syncMatch.Socket.SendMatchStateAsync(_syncMatch.Id, Opcode,  _syncMatch.Encoding.Encode(responseSerialized));
                }
            }

            return newStatus;
        }

        internal ISerializableVar<T> ToSerializable(AckType ackType)
        {
            return new SerializableVar<T>{Value = Value, LockVersion = _lockVersion, Status = Status, AckType = ackType};
        }
    }
}
