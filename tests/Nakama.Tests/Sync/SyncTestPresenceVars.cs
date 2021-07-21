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

namespace Nakama.Tests
{
    // todo apply varid generator to presence var collections
    public class SyncTestPresenceVars
    {
        public Dictionary<string, SelfVar<bool>> BoolSelfVars { get; } = new Dictionary<string, SelfVar<bool>>();
        public Dictionary<string, List<PresenceVar<bool>>> BoolPresenceVars { get; } = new Dictionary<string, List<PresenceVar<bool>>>();

        public Dictionary<string, SelfVar<float>> FloatSelfVars { get; } = new Dictionary<string, SelfVar<float>>();
        public Dictionary<string, List<PresenceVar<float>>> FloatPresenceVars { get; } = new Dictionary<string, List<PresenceVar<float>>>();

        public Dictionary<string, SelfVar<int>> IntSelfVars { get; } = new Dictionary<string, SelfVar<int>>();
        public Dictionary<string, List<PresenceVar<int>>> IntPresenceVars { get; } = new Dictionary<string, List<PresenceVar<int>>>();

        public Dictionary<string, SelfVar<string>> StringSelfVars { get; } = new Dictionary<string, SelfVar<string>>();
        public Dictionary<string, List<PresenceVar<string>>> StringPresenceVars { get; } = new Dictionary<string, List<PresenceVar<string>>>();

        public SyncTestPresenceVars(VarRegistry varRegistry, int numVarCollections, int presenceVarsPerCollection)
        {
            for (int i = 0; i < numVarCollections; i++)
            {
                var selfBool = new SelfVar<bool>();
                var selfFloat = new SelfVar<float>();
                var selfInt = new SelfVar<int>();
                var selfString = new SelfVar<string>();

                string boolKey = "presenceBools_" + i;
                string floatKey = "presenceFloats_" + i;
                string intKey = "presenceInts_" + i;
                string stringKey = "presenceStrings_" + i;

                BoolSelfVars[boolKey] = selfBool;
                varRegistry.Register(boolKey, selfBool);

                FloatSelfVars[floatKey] = selfFloat;
                varRegistry.Register(floatKey, selfFloat);

                IntSelfVars[intKey] = selfInt;
                varRegistry.Register(intKey, selfInt);

                StringSelfVars[stringKey] = selfString;
                varRegistry.Register(stringKey, selfString);


                var presenceBools = new List<PresenceVar<bool>>();
                var presenceFloats = new List<PresenceVar<float>>();
                var presenceInts = new List<PresenceVar<int>>();
                var presenceStrings = new List<PresenceVar<string>>();

                for (int j = 0; j < presenceVarsPerCollection; j++)
                {
                    var presenceBool = new PresenceVar<bool>();
                    varRegistry.Register(boolKey, presenceBool);
                    presenceBools.Add(presenceBool);

                    var presenceFloat = new PresenceVar<float>();
                    varRegistry.Register(floatKey, presenceFloat);
                    presenceFloats.Add(presenceFloat);

                    var presenceInt = new PresenceVar<int>();
                    varRegistry.Register(intKey, presenceInt);
                    presenceInts.Add(presenceInt);

                    var presenceString = new PresenceVar<string>();
                    varRegistry.Register(stringKey, presenceString);
                    presenceStrings.Add(presenceString);
                }

                BoolPresenceVars[boolKey] = presenceBools;
                FloatPresenceVars[floatKey] = presenceFloats;
                IntPresenceVars[intKey] = presenceInts;
                StringPresenceVars[stringKey] = presenceStrings;
            }
        }
    }
}
