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
    public delegate void PresenceAddHandler<T>(PresenceVar<T> var);

    /// <summary>
    /// A variable whose single value is synchronized across all clients connected to the same match
    /// and indexable by user id.
    ///
    /// todo sort of weird that this doesn't inherit from Var<T> but it also doesn't make sense for it do do so.
    /// </summary>
    public class GroupVar<T>
    {
        public event PresenceAddHandler<T> OnPresenceAdded;

        public SelfVar<T> Self { get; }
        public IEnumerable<PresenceVar<T>> Others => _others;
        public long Opcode { get; }

        internal List<PresenceVar<T>> OthersList => _others;

        private readonly List<PresenceVar<T>> _others = new List<PresenceVar<T>>();

        public GroupVar(long opcode)
        {
            Opcode = opcode;
            Self = new SelfVar<T>(opcode);
        }
    }
}
