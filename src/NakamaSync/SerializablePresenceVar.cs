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

using System.Runtime.Serialization;

namespace NakamaSync
{
    /// <summary>
    /// A data-transfer object for a sync var.
    /// </summary>
    internal class SerializablePresenceVar<T> : ISerializableVar<T>
    {
        [DataMember(Name="var"), Preserve]
        public ISerializableVar<T> Var { get; set; }

        [DataMember(Name="user_id"), Preserve]
        public string UserId { get; set; }

        public T Value => Var.Value;
        public int LockVersion { get => Var.LockVersion; set => Var.LockVersion = value; }
        public ValidationStatus Status { get => Var.Status; set => Var.Status = value; }
        public bool IsAck { get => Var.IsAck; set => Var.IsAck = value; }
        public string Key { get => Var.Key; set => Var.Key = value; }

        public SerializablePresenceVar(ISerializableVar<T> var, string userId)
        {
            Var = var;
            UserId = userId;
        }
    }
}