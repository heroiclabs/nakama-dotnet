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

namespace Nakama.Replicated
{
    internal class ReplicatedValueStore
    {
        public IEnumerable<ReplicatedValue<bool>> Bools => _bools;
        public IEnumerable<ReplicatedValue<float>> Floats => _floats;
        public IEnumerable<ReplicatedValue<int>> Ints => _ints;
        public IEnumerable<ReplicatedValue<string>> Strings => _strings;

        private readonly object _boolLock = new object();
        private readonly object _floatLock = new object();
        private readonly object _intLock = new object();
        private readonly object _stringLock = new object();

        [DataMember(Name="replicated_bools"), Preserve]
        private List<ReplicatedValue<bool>> _bools = new List<ReplicatedValue<bool>>();

        [DataMember(Name="replicated_floats"), Preserve]
        private List<ReplicatedValue<float>> _floats = new List<ReplicatedValue<float>>();

        [DataMember(Name="replicated_ints"), Preserve]
        private List<ReplicatedValue<int>> _ints = new List<ReplicatedValue<int>>();

        [DataMember(Name="replicated_strings"), Preserve]
        private List<ReplicatedValue<string>> _strings = new List<ReplicatedValue<string>>();

        public void AddBool(ReplicatedValue<bool> value)
        {
            lock (_boolLock)
            {
                _bools.Add(value);
            }
        }

        public void AddFloat(ReplicatedValue<float> value)
        {
            lock (_floatLock)
            {
                _floats.Add(value);
            }
        }

        public void AddInt(ReplicatedValue<int> value)
        {
            lock (_intLock)
            {
                _ints.Add(value);
            }
        }

        public void AddString(ReplicatedValue<string> value)
        {
            lock (_stringLock)
            {
                _strings.Add(value);
            }
        }
    }
}
