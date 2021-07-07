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
    internal class UserVars
    {
        public SyncVarDictionary<SyncVarKey, UserVar<bool>> Bools { get; }
        public SyncVarDictionary<SyncVarKey, UserVar<float>> Floats { get; }
        public SyncVarDictionary<SyncVarKey, UserVar<int>> Ints { get; }
        public SyncVarDictionary<SyncVarKey, UserVar<string>> Strings { get; }

        public UserVars()
        {
            Bools = new SyncVarDictionary<SyncVarKey, UserVar<bool>>();
            Floats = new SyncVarDictionary<SyncVarKey, UserVar<float>>();
            Ints = new SyncVarDictionary<SyncVarKey, UserVar<int>>();
            Strings = new SyncVarDictionary<SyncVarKey, UserVar<string>>();
        }
    }
}