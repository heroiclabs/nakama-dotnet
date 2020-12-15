/**
 * Copyright 2020 The Nakama Authors
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

namespace Nakama.SocketInternal
{
    /// <summary>
    /// Add the user to the matchmaker pool with properties.
    /// </summary>
    [DataContract]
    public class MatchmakerAddMessage
    {
        [DataMember(Name = "max_count", Order = 1), Preserve] public int MaxCount { get; set; }

        [DataMember(Name = "min_count", Order = 2), Preserve] public int MinCount { get; set; }

        [DataMember(Name = "numeric_properties", Order = 3), Preserve]
        public Dictionary<string, double> NumericProperties { get; set; }

        [DataMember(Name = "query", Order = 4), Preserve] public string Query { get; set; }

        [DataMember(Name = "string_properties", Order = 5), Preserve]
        public Dictionary<string, string> StringProperties { get; set; }

        public override string ToString()
        {
            return
                $"MatchmakerAddMessage(MaxCount={MaxCount}, MinCount={MinCount}, NumericProperties={NumericProperties}, Query='{Query}', StringProperties={StringProperties})";
        }
    }
}
