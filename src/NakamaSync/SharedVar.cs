using Nakama;

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
namespace NakamaSync
{
    /// <summary>
    /// A variable whose single value is synchronized across all clients connected to the same match.
    /// </summary>
    public class SharedVar<T> : Var<T>
    {
        public event VersionConflictHandler<T> OnVersionConflict;

        private int _lockVersion;

        public SharedVar(long opcode) : base(opcode) {}

        public void SetValue(T value)
        {
            // don't increment lock version if haven't done handshake yet.
            // remote clients should overwrite any local value during the handshake.
            if (SyncMatch != null)
            {
                _lockVersion++;
            }

            this.SetLocalValue(SyncMatch?.Self, value);
        }

        internal override void ReceiveSerialized(IUserPresence source, SerializableVar<T> incomingSerialized)
        {
            if (_lockVersion >= incomingSerialized.LockVersion)
            {
                var rejectedWrite = new VersionedWrite<T>(source, incomingSerialized.Value, incomingSerialized.LockVersion);
                var acceptedWrite = new VersionedWrite<T>(source, GetValue(), _lockVersion);
                var conflict = new VersionConflict<T>(rejectedWrite, acceptedWrite);
                var serializable = new SerializableVar<T>
                {
                    Value = Value,
                    LockVersion = _lockVersion,
                    Status = ValidationStatus.Valid,
                    AckType = AckType.LockVersionConflict,
                    LockVersionConflict = conflict,
                };

                Send(serializable, new IUserPresence[]{source});
                return;
            }

            if (incomingSerialized.AckType == AckType.LockVersionConflict)
            {
                OnVersionConflict?.Invoke(incomingSerialized.LockVersionConflict);
                return;
            }

            _lockVersion = incomingSerialized.LockVersion;
            base.ReceiveSerialized(source, incomingSerialized);
        }
    }
}
