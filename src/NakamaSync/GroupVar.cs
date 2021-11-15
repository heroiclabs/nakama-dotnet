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
using Nakama;

namespace NakamaSync
{
    /// <summary>
    /// A variable whose single value is synchronized across all clients connected to the same match
    /// and indexable by user id.
    /// </summary>
    public class GroupVar<T> : Var<T>
    {
        public SelfVar<T> Self { get; }
        public IEnumerable<PresenceVar<T>> Others => _others;

        private readonly List<PresenceVar<T>> _others = new List<PresenceVar<T>>();

        public GroupVar(long opcode) : base(opcode)
        {
            Self = new SelfVar<T>(opcode);
        }

        internal override ISerializableVar<T> ToSerializable(bool isAck)
        {
            throw new InvalidOperationException("Group vars cannot be directly serialized");
        }
    }
}
