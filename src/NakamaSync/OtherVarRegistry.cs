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
    internal class OtherVarRegistry
    {
        public Dictionary<string, OtherVarCollection<bool>> PresenceBools { get; }
        public Dictionary<string, OtherVarCollection<float>> PresenceFloats { get; }
        public Dictionary<string, OtherVarCollection<int>> PresenceInts { get; }
        public Dictionary<string, OtherVarCollection<string>> PresenceStrings { get; }

        public OtherVarRegistry()
        {
            PresenceBools = new Dictionary<string, OtherVarCollection<bool>>();
            PresenceFloats = new Dictionary<string, OtherVarCollection<float>>();
            PresenceInts = new Dictionary<string, OtherVarCollection<int>>();
            PresenceStrings = new Dictionary<string, OtherVarCollection<string>>();
        }
    }
}