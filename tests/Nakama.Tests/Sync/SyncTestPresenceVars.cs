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
        public Dictionary<long, SelfVar<bool>> BoolSelfVars { get; } = new Dictionary<long, SelfVar<bool>>();
        public Dictionary<long, List<PresenceVar<bool>>> BoolPresenceVars { get; } = new Dictionary<long, List<PresenceVar<bool>>>();

        public Dictionary<long, SelfVar<float>> FloatSelfVars { get; } = new Dictionary<long, SelfVar<float>>();
        public Dictionary<long, List<PresenceVar<float>>> FloatPresenceVars { get; } = new Dictionary<long, List<PresenceVar<float>>>();

        public Dictionary<long, SelfVar<int>> IntSelfVars { get; } = new Dictionary<long, SelfVar<int>>();
        public Dictionary<long, List<PresenceVar<int>>> IntPresenceVars { get; } = new Dictionary<string, List<PresenceVar<int>>>();

        public Dictionary<long, SelfVar<string>> StringSelfVars { get; } = new Dictionary<long, SelfVar<string>>();
        public Dictionary<long, List<PresenceVar<string>>> StringPresenceVars { get; } = new Dictionary<long, List<PresenceVar<string>>>();

        public SyncTestPresenceVars(VarRegistry varRegistry, int numVarCollections, int PresenceVarsPerCollection)
        {
            for (int i = 0; i < numVarCollections; i++)
            {
                var selfBool = new SelfVar<bool>("presenceBools_" + i);
                var selfFloat = new SelfVar<float>("presenceFloats_" + i);
                var selfInt = new SelfVar<int>("presenceInts_" + i);
                var selfString = new SelfVar<string>("presenceStrings_" + i);

                BoolSelfVars[selfBool.Opcode] = selfBool;
                varRegistry.Register(selfBool);

                FloatSelfVars[selfFloat.Opcode] = selfFloat;
                varRegistry.Register(selfFloat);

                IntSelfVars[selfInt.Opcode] = selfInt;
                varRegistry.Register(selfInt);

                StringSelfVars[selfString.Opcode] = selfString;
                varRegistry.Register(selfString);

                var presenceBools = new List<PresenceVar<bool>>();
                var presenceFloats = new List<PresenceVar<float>>();
                var presenceInts = new List<PresenceVar<int>>();
                var presenceStrings = new List<PresenceVar<string>>();

                for (int j = 0; j < PresenceVarsPerCollection; j++)
                {
                    var presenceBool = new PresenceVar<bool>(selfBool.Opcode);
                    varRegistry.Register(presenceBool);
                    presenceBools.Add(presenceBool);

                    var presenceFloat = new PresenceVar<float>(selfFloat.Opcode);
                    varRegistry.Register(presenceFloat);
                    presenceFloats.Add(presenceFloat);

                    var presenceInt = new PresenceVar<int>(selfInt.Opcode);
                    varRegistry.Register(presenceInt);
                    presenceInts.Add(presenceInt);

                    var presenceString = new PresenceVar<string>(selfString.Opcode);
                    varRegistry.Register(presenceString);
                    presenceStrings.Add(presenceString);
                }

                BoolPresenceVars[selfBool.Opcode] = presenceBools;
                FloatPresenceVars[selfFloat.Opcode] = presenceFloats;
                IntPresenceVars[selfInt.Opcode] = presenceInts;
                StringPresenceVars[selfString.Opcode] = presenceStrings;
            }
        }
    }
}
