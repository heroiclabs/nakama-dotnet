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
    public class SyncTestPresenceVars
    {
        public SelfVar<bool> BoolSelfVar { get; }
        public List<PresenceVar<bool>> BoolPresenceVars { get; } = new List<PresenceVar<bool>>();

        public SelfVar<float> FloatSelfVar { get; }
        public List<PresenceVar<float>> FloatPresenceVars { get; } = new List<PresenceVar<float>>();

        public SelfVar<int> IntSelfVar { get; }
        public List<PresenceVar<int>> IntPresenceVars { get; } = new List<PresenceVar<int>>();

        public SelfVar<string> StringSelfVar { get; }
        public List<PresenceVar<string>> StringPresenceVars { get; } = new List<PresenceVar<string>>();

        public SyncTestPresenceVars(VarRegistry varRegistry, int presenceVarsPerCollection)
        {
            BoolSelfVar = new SelfVar<bool>(opcode: 0);
            FloatSelfVar = new SelfVar<float>(opcode: 1);
            IntSelfVar = new SelfVar<int>(opcode: 2);
            StringSelfVar = new SelfVar<string>(opcode: 3);

            varRegistry.Register(BoolSelfVar);
            varRegistry.Register(FloatSelfVar);
            varRegistry.Register(IntSelfVar);
            varRegistry.Register(StringSelfVar);

            var presenceBools = new List<PresenceVar<bool>>();
            var presenceFloats = new List<PresenceVar<float>>();
            var presenceInts = new List<PresenceVar<int>>();
            var presenceStrings = new List<PresenceVar<string>>();

            for (int j = 0; j < presenceVarsPerCollection; j++)
            {
                var presenceBool = new PresenceVar<bool>(BoolSelfVar.Opcode);
                varRegistry.Register(presenceBool);
                presenceBools.Add(presenceBool);

                var presenceFloat = new PresenceVar<float>(FloatSelfVar.Opcode);
                varRegistry.Register(presenceFloat);
                presenceFloats.Add(presenceFloat);

                var presenceInt = new PresenceVar<int>(IntSelfVar.Opcode);
                varRegistry.Register(presenceInt);
                presenceInts.Add(presenceInt);

                var presenceString = new PresenceVar<string>(StringSelfVar.Opcode);
                varRegistry.Register(presenceString);
                presenceStrings.Add(presenceString);
            }
        }
    }
}
