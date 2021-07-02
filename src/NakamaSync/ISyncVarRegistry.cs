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
    interface ISyncVarRegistry
    {
        SyncVarDictionary<string, SharedVar<bool>> SharedBools { get; }
        SyncVarDictionary<string, SharedVar<float>> SharedFloats { get; }
        SyncVarDictionary<string, SharedVar<int>> SharedInts { get; }
        SyncVarDictionary<string, SharedVar<string>> SharedStrings { get; }
        SyncVarDictionary<string, UserVar<bool>> UserBools { get; }
        SyncVarDictionary<string, UserVar<float>> UserFloats { get; }
        SyncVarDictionary<string, UserVar<int>> UserInts { get; }
        SyncVarDictionary<string, UserVar<string>> UserStrings { get; }
    }
}