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
        [DataMember(Name="bools"), Preserve]
        public Envelope<bool> Bools { get; set; }

        [DataMember(Name="floats"), Preserve]
        public Envelope<float> Floats { get; set; }

        [DataMember(Name="ints"), Preserve]
        public Envelope<int> Ints { get; set; }

        [DataMember(Name="strings"), Preserve]
        public Envelope<string> Strings { get; set; }

        internal Envelope()
        {
            Bools = new Envelope<bool>();
            Floats = new Envelope<float>();
            Ints = new Envelope<int>();
            Strings = new Envelope<string>();
        }

        public List<IVarValue> GetAllValues()
        {
            var allVars = new List<IVarValue>();

            allVars.AddRange(Bools.GetAllValues());
            allVars.AddRange(Floats.GetAllValues());
            allVars.AddRange(Ints.GetAllValues());
            allVars.AddRange(Strings.GetAllValues());

            return allVars;
        }
    }

    internal class Envelope<T>
    {
        [DataMember(Name="shared"), Preserve]
        public List<SharedVarValue<T>> SharedValues { get; set; }
        [DataMember(Name="presence"), Preserve]
        public List<PresenceVarValue<T>> PresenceValues { get; set; }

        public Envelope()
        {
            SharedValues = new List<SharedVarValue<T>>();
            PresenceValues = new List<PresenceVarValue<T>>();
        }

        public List<IVarValue<T>> GetAllValues()
        {
            var allValues = new List<IVarValue<T>>();
            allValues.AddRange(SharedValues);
            allValues.AddRange(PresenceValues);
            return allValues;
        }
    }
}
