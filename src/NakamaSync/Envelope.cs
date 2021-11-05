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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NakamaSync
{
    internal class Envelope
    {
        [DataMember(Name="shared_bools"), Preserve]
        public List<VarValue<bool>> SharedBools { get; set; }

        [DataMember(Name="shared_floats"), Preserve]
        public List<VarValue<float>> SharedFloats { get; set; }

        [DataMember(Name="shared_ints"), Preserve]
        public List<VarValue<int>> SharedInts { get; set; }

        [DataMember(Name="shared_strings"), Preserve]
        public List<VarValue<string>> SharedStrings { get; set; }

        [DataMember(Name="shared_objects"), Preserve]
        public List<VarValue<object>> SharedObjects { get; set; }

        [DataMember(Name="user_bools"), Preserve]
        public List<PresenceValue<bool>> PresenceBools { get; set; }

        [DataMember(Name="user_floats"), Preserve]
        public List<PresenceValue<float>> PresenceFloats { get; set; }

        [DataMember(Name="user_ints"), Preserve]
        public List<PresenceValue<int>> PresenceInts { get; set; }

        [DataMember(Name="user_strings"), Preserve]
        public List<PresenceValue<string>> PresenceStrings { get; set; }

        [DataMember(Name="shared_bool_acks"), Preserve]
        public List<ValidationAck> SharedBoolAcks { get; set; }

        [DataMember(Name="shared_float_acks"), Preserve]
        public List<ValidationAck> SharedFloatAcks { get; set; }

        [DataMember(Name="shared_int_acks"), Preserve]
        public List<ValidationAck> SharedIntAcks { get; set; }

        [DataMember(Name="shared_string_acks"), Preserve]
        public List<ValidationAck> SharedStringAcks { get; set; }

        [DataMember(Name="shared_object_acks"), Preserve]
        public List<ValidationAck> SharedObjectAcks { get; set; }

        [DataMember(Name="user_bool_acks"), Preserve]
        public List<ValidationAck> PresenceBoolAcks { get; set; }

        [DataMember(Name="user_float_acks"), Preserve]
        public List<ValidationAck> PresenceFloatAcks { get; set; }

        [DataMember(Name="user_int_acks"), Preserve]
        public List<ValidationAck> PresenceIntAcks { get; set; }

        [DataMember(Name="user_string_acks"), Preserve]
        public List<ValidationAck> PresenceStringAcks { get; set; }

        internal Envelope()
        {
            SharedBools = new List<VarValue<bool>>();
            SharedFloats = new List<VarValue<float>>();
            SharedInts = new List<VarValue<int>>();
            SharedStrings = new List<VarValue<string>>();
            SharedObjects = new List<VarValue<object>>();

            PresenceBools = new List<PresenceValue<bool>>();
            PresenceFloats = new List<PresenceValue<float>>();
            PresenceInts = new List<PresenceValue<int>>();
            PresenceStrings = new List<PresenceValue<string>>();

            SharedBoolAcks = new List<ValidationAck>();
            SharedFloatAcks = new List<ValidationAck>();
            SharedIntAcks = new List<ValidationAck>();
            SharedStringAcks = new List<ValidationAck>();

            PresenceBoolAcks = new List<ValidationAck>();
            PresenceFloatAcks = new List<ValidationAck>();
            PresenceIntAcks = new List<ValidationAck>();
            PresenceStringAcks = new List<ValidationAck>();
        }
    }
}
