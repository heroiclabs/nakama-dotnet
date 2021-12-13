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
using System.Runtime.Serialization;

namespace NakamaSync
{
    /// <summary>
    /// A data-transfer object for a sync var.
    /// </summary>
    [Serializable]
    internal class SerializableVar<T> : ISerializableVar<T>
    {
        [DataMember(Name="value"), Preserve]
        public T Value { get; set; }

        [DataMember(Name="version"), Preserve]
        public int Version { get; set; }

        [DataMember(Name="validation_status"), Preserve]
        public ValidationStatus Status { get; set; }

        [DataMember(Name="ack_type"), Preserve]
        public VarMessageType AckType { get; set; }

        [DataMember(Name="version_conflict"), Preserve]
        public VersionConflict<T> VersionConflict { get; set; }
    }
}