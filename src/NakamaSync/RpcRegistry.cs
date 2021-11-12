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
    public class RpcRegistry
    {
        private readonly HashSet<object> _targets = new HashSet<object>();
        private readonly Dictionary<string, object> _targetsById = new Dictionary<string, object>();
        private readonly long _opcode;

        public long Opcode => _opcode;

        public RpcRegistry(long opcode)
        {
            _opcode = opcode;
        }

        public void AddTarget(string targetId, object target)
        {
            if (_targets.Contains(target))
            {
                throw new ArgumentException("Tried registering duplicate target.");
            }

            if (_targetsById.ContainsKey(targetId))
            {
                throw new ArgumentException("Tried registering duplicate target id: " + targetId);
            }

            _targetsById[targetId] = target;
            _targets.Add(target);
        }

        public bool HasTarget(string targetId)
        {
            return _targetsById.ContainsKey(targetId);
        }

        public object GetTarget(string targetId)
        {
            return _targetsById[targetId];
        }

        public void RemoveTarget(string targetId)
        {
            if (_targets.Contains(targetId))
            {
                _targets.Remove(targetId);
                _targetsById.Remove(targetId);
            }
        }
    }
}
