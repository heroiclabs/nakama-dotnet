// Copyright 2022 The Satori Authors
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

namespace Satori
{
    /// <summary>
    /// A request for a flag from the Satori server.
    /// </summary>
    public class FlagRequest
    {
        /// <summary>
        /// The name of the flag.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The value of the flag if the Satori server temporarily cannot be reached.
        /// <summary/>
        public string OfflineValue { get; }

        public FlagRequest(string name, string offlineValue)
        {
            Name = name;
            OfflineValue = offlineValue;
        }
    }
}
