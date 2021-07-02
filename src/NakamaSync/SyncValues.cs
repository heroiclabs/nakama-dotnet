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
    internal class SyncValues
    {
        public List<SyncSharedValue<bool>> SharedBools => _sharedBools;
        public List<SyncSharedValue<float>> SharedFloats => _sharedFloats;
        public List<SyncSharedValue<int>> SharedInts => _sharedInts;
        public List<SyncSharedValue<string>> SharedStrings => _sharedStrings;

        public List<SyncUserValue<bool>> UserBools => _userBools;
        public List<SyncUserValue<float>> UserFloats => _userFloats;
        public List<SyncUserValue<int>> UserInts => _userInts;
        public List<SyncUserValue<string>> UserStrings => _userStrings;

        [DataMember(Name="shared_bools"), Preserve]
        private List<SyncSharedValue<bool>> _sharedBools = new List<SyncSharedValue<bool>>();

        [DataMember(Name="shared_floats"), Preserve]
        private List<SyncSharedValue<float>> _sharedFloats = new List<SyncSharedValue<float>>();

        [DataMember(Name="shared_ints"), Preserve]
        private List<SyncSharedValue<int>> _sharedInts = new List<SyncSharedValue<int>>();

        [DataMember(Name="shared_strings"), Preserve]
        private List<SyncSharedValue<string>> _sharedStrings = new List<SyncSharedValue<string>>();

        [DataMember(Name="user_bools"), Preserve]
        private List<SyncUserValue<bool>> _userBools = new List<SyncUserValue<bool>>();

        [DataMember(Name="user_floats"), Preserve]
        private List<SyncUserValue<float>> _userFloats = new List<SyncUserValue<float>>();

        [DataMember(Name="user_ints"), Preserve]
        private List<SyncUserValue<int>> _userInts = new List<SyncUserValue<int>>();

        [DataMember(Name="user_strings"), Preserve]
        private List<SyncUserValue<string>> _userStrings = new List<SyncUserValue<string>>();

        public bool IsEmpty()
        {
            return SharedBools.Count + SharedFloats.Count + SharedInts.Count + SharedStrings.Count +
               UserBools.Count + UserFloats.Count + UserStrings.Count == 0;
        }
    }
}
