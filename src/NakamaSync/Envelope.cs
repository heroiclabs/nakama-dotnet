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
        public List<VarValue<bool>> Bools { get; set; }

        [DataMember(Name="shared_floats"), Preserve]
        public List<VarValue<float>> Floats { get; set; }

        [DataMember(Name="shared_ints"), Preserve]
        public List<VarValue<int>> Ints { get; set; }

        [DataMember(Name="shared_strings"), Preserve]
        public List<VarValue<string>> Strings { get; set; }

        [DataMember(Name="shared_objects"), Preserve]
        public List<VarValue<object>> Objects { get; set; }

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

        internal Envelope()
        {
            Bools = new List<VarValue<bool>>();
            Floats = new List<VarValue<float>>();
            Ints = new List<VarValue<int>>();
            Strings = new List<VarValue<string>>();
            Objects = new List<VarValue<object>>();
        }
    }
}
