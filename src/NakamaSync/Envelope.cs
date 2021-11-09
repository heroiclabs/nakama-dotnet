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

using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Collections;

namespace NakamaSync
{
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

        public List<IVarValue> GetAllValues()
        {
            var allValues = new List<IVarValue>();
            allValues.AddRange(SharedValues);
            allValues.AddRange(PresenceValues);
            return allValues;
        }
    }
}
