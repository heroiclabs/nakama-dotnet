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
    internal class SyncVarRegistryInternal
    {
        public SyncVarDictionary<SyncVarKey, SharedVar<bool>> SharedBools { get; }
        public SyncVarDictionary<SyncVarKey, SharedVar<float>> SharedFloats { get; }
        public SyncVarDictionary<SyncVarKey, SharedVar<int>> SharedInts { get; }
        public SyncVarDictionary<SyncVarKey, SharedVar<string>> SharedStrings { get; }

        public SyncVarDictionary<SyncVarKey, UserVar<bool>> UserBools { get; }
        public SyncVarDictionary<SyncVarKey, UserVar<float>> UserFloats { get; }
        public SyncVarDictionary<SyncVarKey, UserVar<int>> UserInts { get; }
        public SyncVarDictionary<SyncVarKey, UserVar<string>> UserStrings { get; }

        public SyncVarRegistryInternal()
        {
            SharedBools = new SyncVarDictionary<SyncVarKey, SharedVar<bool>>();
            SharedFloats = new SyncVarDictionary<SyncVarKey, SharedVar<float>>();
            SharedInts = new SyncVarDictionary<SyncVarKey, SharedVar<int>>();
            SharedStrings = new SyncVarDictionary<SyncVarKey, SharedVar<string>>();

            UserBools = new SyncVarDictionary<SyncVarKey, UserVar<bool>>();
            UserFloats = new SyncVarDictionary<SyncVarKey, UserVar<float>>();
            UserInts = new SyncVarDictionary<SyncVarKey, UserVar<int>>();
            UserStrings = new SyncVarDictionary<SyncVarKey, UserVar<string>>();
        }
    }
}
