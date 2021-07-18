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
    internal class SharedVarRegistry
    {
        public Dictionary<string, SharedVar<bool>> SharedBools { get; }
        public Dictionary<string, SharedVar<float>> SharedFloats { get; }
        public Dictionary<string, SharedVar<int>> SharedInts { get; }
        public Dictionary<string, SharedVar<string>> SharedStrings { get; }

        public SharedVarRegistry()
        {
            SharedBools = new Dictionary<string, SharedVar<bool>>();
            SharedFloats = new Dictionary<string, SharedVar<float>>();
            SharedInts = new Dictionary<string, SharedVar<int>>();
            SharedStrings = new Dictionary<string, SharedVar<string>>();
        }
    }
}