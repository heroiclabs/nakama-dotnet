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

using System;
using System.Collections.Generic;

namespace NakamaSync
{
    // todo should this be internal if possible?
    public class PresenceVarCollection<T>
    {
        public SelfVar<T> SelfVar { get; set; }
        public List<PresenceVar<T>> PresenceVars => _presenceVars;

        private readonly List<PresenceVar<T>> _presenceVars = new List<PresenceVar<T>>();

        public PresenceVarCollection(SelfVar<T> selfVar, IEnumerable<PresenceVar<T>> presenceVars)
        {
            SelfVar = selfVar;

            // copy to internal representation
            foreach (var presenceVar in presenceVars)
            {
                _presenceVars.Add(presenceVar);
            }
        }
    }
}