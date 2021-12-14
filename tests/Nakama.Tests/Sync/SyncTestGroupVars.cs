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
using NakamaSync;

namespace Nakama.Tests.Sync
{
    public class SyncTestGroupVars
    {
        public GroupVar<bool> GroupBool { get; }
        public GroupVar<float> GroupFloat { get; }
        public GroupVar<int> GroupInt { get; }
        public GroupVar<string> GroupString { get; }
        public GroupVar<Dictionary<string, string>> GroupDict { get; }

        public SyncTestGroupVars(VarRegistry varRegistry, bool delayRegistration)
        {
            GroupBool = new GroupVar<bool>(opcode: 0);
            GroupFloat = new GroupVar<float>(opcode: 1);
            GroupInt = new GroupVar<int>(opcode: 2);
            GroupString = new GroupVar<string>(opcode: 3);
            GroupDict = new GroupVar<Dictionary<string, string>>(opcode: 4);

            if (!delayRegistration)
            {
                varRegistry.Register(GroupBool);
                varRegistry.Register(GroupFloat);
                varRegistry.Register(GroupInt);
                varRegistry.Register(GroupString);
                varRegistry.Register(GroupDict);
            }
        }
    }
}
