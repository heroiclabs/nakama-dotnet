/**
 * Copyright 2018 The Nakama Authors
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

namespace Nakama
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Add the user to the matchmaker pool with properties.
    /// </summary>
    internal class MatchmakerAddMessage
    {
        [DataMember(Name="numeric_properties")]
        public Dictionary<string, double> NumericProperties { get; set; }

        [DataMember(Name="max_count")]
        public int MaxCount { get; set; }

        [DataMember(Name="min_count")]
        public int MinCount { get; set; }

        [DataMember(Name="query")]
        public string Query { get; set; }

        [DataMember(Name="string_properties")]
        public Dictionary<string, string> StringProperties { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"MatchmakerAddMessage[NumericProperties={NumericProperties}, MaxCount={MaxCount}, MinCount={MinCount}, Query={Query}, StringProperties={StringProperties}]";
        }
    }
}
