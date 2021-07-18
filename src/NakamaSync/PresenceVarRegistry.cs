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

namespace NakamaSync
{
    internal class PresenceVarRegistry
    {
        public Dictionary<string, PresenceVarCollection<bool>> PresenceBools { get; }
        public Dictionary<string, PresenceVarCollection<float>> PresenceFloats { get; }
        public Dictionary<string, PresenceVarCollection<int>> PresenceInts { get; }
        public Dictionary<string, PresenceVarCollection<string>> PresenceStrings { get; }

        public PresenceVarRegistry()
        {
            PresenceBools = new Dictionary<string, PresenceVarCollection<bool>>();
            PresenceFloats = new Dictionary<string, PresenceVarCollection<float>>();
            PresenceInts = new Dictionary<string, PresenceVarCollection<int>>();
            PresenceStrings = new Dictionary<string, PresenceVarCollection<string>>();
        }
    }
}