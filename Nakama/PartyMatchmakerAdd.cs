// Copyright 2021 The Nakama Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Nakama
{
    /// <summary>
    /// Begin matchmaking as a party.
    /// </summary>
    internal class PartyMatchmakerAdd
    {
        [DataMember(Name = "party_id"), Preserve]
        public string PartyId { get; set; }

        [DataMember(Name = "max_count"), Preserve]
        public int MaxCount { get; set; }

        [DataMember(Name = "min_count"), Preserve]
        public int MinCount { get; set; }

        [DataMember(Name = "query"), Preserve] public string Query { get; set; }

        [DataMember(Name = "string_properties"), Preserve]
        public Dictionary<string, string> StringProperties { get; set; }

        [DataMember(Name = "numeric_properties"), Preserve]
        public Dictionary<string, double> NumericProperties { get; set; }
        
        [DataMember(Name = "count_multiple"), Preserve]
        public int? CountMultiple { get; set; }

        public override string ToString() =>
            $"PartyMatchmakerAdd(PartyId='{PartyId}', MaxCount={MaxCount}, MinCount={MinCount}, NumericProperties={NumericProperties}, Query='{Query}', StringProperties={StringProperties}, CountMultiple={CountMultiple})";
    }
}
