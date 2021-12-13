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

        public SharedVar(long opcode) : base(opcode) {}

        public void SetValue(T value)
        {
            this.SetValueViaSelf(value);
        }

        internal override void HandleVersionConflict(VersionConflict<T> conflict)
        {
            // only host is responsible for notifying other clients of lock version conflicts.
            // imagine if they weren't -- every client would report a lock version conflict to the sender
            // the sender would be spammed for just a single unobserved write!
            if (!SyncMatch.IsSelfHost())
            {
                return;
            }

            var serializable = new SerializableVar<T>
            {
                Value = Value.Value,
                Version = Value.Version,
                Status = ValidationStatus.Valid,
                AckType = VarMessageType.VersionConflict,
                VersionConflict = conflict
            };

            Send(serializable, new IUserPresence[]{conflict.RejectedWrite.Source});

            OnVersionConflict?.Invoke(conflict);
        }
    }
}
