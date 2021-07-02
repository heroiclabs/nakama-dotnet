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

namespace NakamaSync
{
    public class SyncVarRegistry : ISyncVarRegistry
    {
        public SyncVarDictionary<string, SharedVar<bool>> SharedBools { get; }
        public SyncVarDictionary<string, SharedVar<float>> SharedFloats { get; }
        public SyncVarDictionary<string, SharedVar<int>> SharedInts { get; }
        public SyncVarDictionary<string, SharedVar<string>> SharedStrings { get; }

        public SyncVarDictionary<string, UserVar<bool>> UserBools { get; }
        public SyncVarDictionary<string, UserVar<float>> UserFloats { get; }
        public SyncVarDictionary<string, UserVar<int>> UserInts { get; }
        public SyncVarDictionary<string, UserVar<string>> UserStrings { get; }

        public SyncVarRegistry()
        {
            SharedBools = new SyncVarDictionary<string, SharedVar<bool>>();
            SharedFloats = new SyncVarDictionary<string, SharedVar<float>>();
            SharedInts = new SyncVarDictionary<string, SharedVar<int>>();
            SharedStrings = new SyncVarDictionary<string, SharedVar<string>>();

            UserBools = new SyncVarDictionary<string, UserVar<bool>>();
            UserFloats = new SyncVarDictionary<string, UserVar<float>>();
            UserInts = new SyncVarDictionary<string, UserVar<int>>();
            UserStrings = new SyncVarDictionary<string, UserVar<string>>();
        }
    }
}