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
    internal class SyncVarValues
    {
        public IEnumerable<SyncVarValue<bool>> Bools => _bools;
        public IEnumerable<SyncVarValue<float>> Floats => _floats;
        public IEnumerable<SyncVarValue<int>> Ints => _ints;
        public IEnumerable<SyncVarValue<string>> Strings => _strings;

        private readonly object _boolLock = new object();
        private readonly object _floatLock = new object();
        private readonly object _intLock = new object();
        private readonly object _stringLock = new object();

        [DataMember(Name="synced_bools"), Preserve]
        private List<SyncVarValue<bool>> _bools = new List<SyncVarValue<bool>>();

        [DataMember(Name="synced_floats"), Preserve]
        private List<SyncVarValue<float>> _floats = new List<SyncVarValue<float>>();

        [DataMember(Name="synced_ints"), Preserve]
        private List<SyncVarValue<int>> _ints = new List<SyncVarValue<int>>();

        [DataMember(Name="synced_strings"), Preserve]
        private List<SyncVarValue<string>> _strings = new List<SyncVarValue<string>>();

        public void AddBool(SyncVarValue<bool> value)
        {
            lock (_boolLock)
            {
                _bools.Add(value);
            }
        }

        public void AddFloat(SyncVarValue<float> value)
        {
            lock (_floatLock)
            {
                _floats.Add(value);
            }
        }

        public void AddInt(SyncVarValue<int> value)
        {
            lock (_intLock)
            {
                _ints.Add(value);
            }
        }

        public void AddString(SyncVarValue<string> value)
        {
            lock (_stringLock)
            {
                _strings.Add(value);
            }
        }
    }
}
