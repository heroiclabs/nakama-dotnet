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
        public List<SharedValue<bool>> SharedBools { get; set; }

        [DataMember(Name="shared_floats"), Preserve]
        public List<SharedValue<float>> SharedFloats { get; set; }

        [DataMember(Name="shared_ints"), Preserve]
        public List<SharedValue<int>> SharedInts { get; set; }

        [DataMember(Name="shared_strings"), Preserve]
        public List<SharedValue<string>> SharedStrings { get; set; }

        [DataMember(Name="user_bools"), Preserve]
        public List<UserValue<bool>> UserBools { get; set; }

        [DataMember(Name="user_floats"), Preserve]
        public List<UserValue<float>> UserFloats { get; set; }

        [DataMember(Name="user_ints"), Preserve]
        public List<UserValue<int>> UserInts { get; set; }

        [DataMember(Name="user_strings"), Preserve]
        public List<UserValue<string>> UserStrings { get; set; }

        [DataMember(Name="shared_bool_acks"), Preserve]
        public List<ValidationAck> SharedBoolAcks { get; set; }

        [DataMember(Name="shared_float_acks"), Preserve]
        public List<ValidationAck> SharedFloatAcks { get; set; }

        [DataMember(Name="shared_int_acks"), Preserve]
        public List<ValidationAck> SharedIntAcks { get; set; }

        [DataMember(Name="shared_string_acks"), Preserve]
        public List<ValidationAck> SharedStringAcks { get; set; }

        [DataMember(Name="user_bool_acks"), Preserve]
        public List<ValidationAck> UserBoolAcks { get; set; }

        [DataMember(Name="user_float_acks"), Preserve]
        public List<ValidationAck> UserFloatAcks { get; set; }

        [DataMember(Name="user_int_acks"), Preserve]
        public List<ValidationAck> UserIntAcks { get; set; }

        [DataMember(Name="user_string_acks"), Preserve]
        public List<ValidationAck> UserStringAcks { get; set; }

        internal Envelope()
        {
            SharedBools = new List<SharedValue<bool>>();
            SharedFloats = new List<SharedValue<float>>();
            SharedInts = new List<SharedValue<int>>();
            SharedStrings = new List<SharedValue<string>>();

            UserBools = new List<UserValue<bool>>();
            UserFloats = new List<UserValue<float>>();
            UserInts = new List<UserValue<int>>();
            UserStrings = new List<UserValue<string>>();

            SharedBoolAcks = new List<ValidationAck>();
            SharedFloatAcks = new List<ValidationAck>();
            SharedIntAcks = new List<ValidationAck>();
            SharedStringAcks = new List<ValidationAck>();

            UserBoolAcks = new List<ValidationAck>();
            UserFloatAcks = new List<ValidationAck>();
            UserIntAcks = new List<ValidationAck>();
            UserStringAcks = new List<ValidationAck>();
        }
    }
}
