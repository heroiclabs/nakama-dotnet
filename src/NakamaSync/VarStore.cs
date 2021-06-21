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

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NakamaSync
{
    internal class VarStore
    {
        public ConcurrentDictionary<VarKey, UserVar<bool>> UserBools => _userBools;
        public ConcurrentDictionary<VarKey, UserVar<float>> UserFloats => _userFloats;
        public ConcurrentDictionary<VarKey, UserVar<int>> UserInts => _userInts;
        public ConcurrentDictionary<VarKey, UserVar<string>> UserStrings => _userStrings;

        private readonly ConcurrentDictionary<VarKey, UserVar<bool>> _userBools = new ConcurrentDictionary<VarKey, UserVar<bool>>();
        private readonly ConcurrentDictionary<VarKey, UserVar<float>> _userFloats = new ConcurrentDictionary<VarKey, UserVar<float>>();
        private readonly ConcurrentDictionary<VarKey, UserVar<int>> _userInts = new ConcurrentDictionary<VarKey, UserVar<int>>();
        private readonly ConcurrentDictionary<VarKey, UserVar<string>> _userStrings = new ConcurrentDictionary<VarKey, UserVar<string>>();
    }
}