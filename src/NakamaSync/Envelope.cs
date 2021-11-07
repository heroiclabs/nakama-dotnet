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
        public List<SharedVarValue<bool>> Bools { get; set; }

        [DataMember(Name="floats"), Preserve]
        public List<SharedVarValue<float>> Floats { get; set; }

        [DataMember(Name="ints"), Preserve]
        public List<SharedVarValue<int>> Ints { get; set; }

        [DataMember(Name="strings"), Preserve]
        public List<SharedVarValue<string>> Strings { get; set; }

        [DataMember(Name="objects"), Preserve]
        public List<SharedVarValue<object>> Objects { get; set; }

        [DataMember(Name="presence_bools"), Preserve]
        public List<PresenceVarValue<bool>> PresenceBools { get; set; }

        [DataMember(Name="presence_floats"), Preserve]
        public List<PresenceVarValue<float>> PresenceFloats { get; set; }

        [DataMember(Name="presence_ints"), Preserve]
        public List<PresenceVarValue<int>> PresenceInts { get; set; }

        [DataMember(Name="presence_strings"), Preserve]
        public List<PresenceVarValue<string>> PresenceStrings { get; set; }

        [DataMember(Name="presence_objects"), Preserve]
        public List<PresenceVarValue<object>> PresenceObjects { get; set; }

        internal Envelope()
        {
            Bools = new List<SharedVarValue<bool>>();
            Floats = new List<SharedVarValue<float>>();
            Ints = new List<SharedVarValue<int>>();
            Strings = new List<SharedVarValue<string>>();
            Objects = new List<SharedVarValue<object>>();

            PresenceBools = new List<PresenceVarValue<bool>>();
            PresenceFloats = new List<PresenceVarValue<float>>();
            PresenceInts = new List<PresenceVarValue<int>>();
            PresenceStrings = new List<PresenceVarValue<string>>();
            PresenceObjects = new List<PresenceVarValue<object>>();
        }

        public List<IVarValue> AllVars()
        {
            var allVars = new List<IVarValue>();

            allVars.AddRange(Bools);
            allVars.AddRange(Floats);
            allVars.AddRange(Ints);
            allVars.AddRange(Strings);
            allVars.AddRange(Objects);
            allVars.AddRange(PresenceBools);
            allVars.AddRange(PresenceFloats);
            allVars.AddRange(PresenceInts);
            allVars.AddRange(PresenceStrings);
            allVars.AddRange(PresenceObjects);

            return allVars;
        }
    }
}
