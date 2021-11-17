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
    // todo apply varid generator to presence var collections
    public class SyncTestGroupVars
    {
        public GroupVar<bool> BoolGroupVar { get; }
        public GroupVar<float> FloatGroupVar { get; }
        public GroupVar<int> IntGroupVar { get; }
        public GroupVar<string> StringGroupVar { get; }

        public SyncTestGroupVars(VarRegistry varRegistry, int presenceVarsPerCollection, bool delayRegistration)
        {
            BoolGroupVar = new GroupVar<bool>(opcode: 0);
            FloatGroupVar = new GroupVar<float>(opcode: 1);
            IntGroupVar = new GroupVar<int>(opcode: 2);
            StringGroupVar = new GroupVar<string>(opcode: 3);

            if (!delayRegistration)
            {
                varRegistry.Register(BoolGroupVar);
                varRegistry.Register(FloatGroupVar);
                varRegistry.Register(IntGroupVar);
                varRegistry.Register(StringGroupVar);
            }
        }
    }
}
