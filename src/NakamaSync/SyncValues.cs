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
using System.Runtime.Serialization;

namespace NakamaSync
{
    internal delegate List<SharedValue<T>> SharedVarAccessor<T>(SyncValues values);
    internal delegate List<UserValue<T>> UserVarAccessor<T>(SyncValues values);

    internal class SyncValues
    {
        public List<SharedValue<bool>> SharedBools => _sharedBools;
        public List<SharedValue<float>> SharedFloats => _sharedFloats;
        public List<SharedValue<int>> SharedInts => _sharedInts;
        public List<SharedValue<string>> SharedStrings => _sharedStrings;

        public List<UserValue<bool>> UserBools => _userBools;
        public List<UserValue<float>> UserFloats => _userFloats;
        public List<UserValue<int>> UserInts => _userInts;
        public List<UserValue<string>> UserStrings => _userStrings;

        [DataMember(Name="shared_bools"), Preserve]
        private List<SharedValue<bool>> _sharedBools = new List<SharedValue<bool>>();

        [DataMember(Name="shared_floats"), Preserve]
        private List<SharedValue<float>> _sharedFloats = new List<SharedValue<float>>();

        [DataMember(Name="shared_ints"), Preserve]
        private List<SharedValue<int>> _sharedInts = new List<SharedValue<int>>();

        [DataMember(Name="shared_strings"), Preserve]
        private List<SharedValue<string>> _sharedStrings = new List<SharedValue<string>>();

        [DataMember(Name="user_bools"), Preserve]
        private List<UserValue<bool>> _userBools = new List<UserValue<bool>>();

        [DataMember(Name="user_floats"), Preserve]
        private List<UserValue<float>> _userFloats = new List<UserValue<float>>();

        [DataMember(Name="user_ints"), Preserve]
        private List<UserValue<int>> _userInts = new List<UserValue<int>>();

        [DataMember(Name="user_strings"), Preserve]
        private List<UserValue<string>> _userStrings = new List<UserValue<string>>();
    }
}
