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
    public class SyncTestOtherVars
    {
        public Dictionary<string, SelfVar<bool>> BoolSelfVars { get; } = new Dictionary<string, SelfVar<bool>>();
        public Dictionary<string, List<OtherVar<bool>>> BoolOtherVars { get; } = new Dictionary<string, List<OtherVar<bool>>>();

        public Dictionary<string, SelfVar<float>> FloatSelfVars { get; } = new Dictionary<string, SelfVar<float>>();
        public Dictionary<string, List<OtherVar<float>>> FloatOtherVars { get; } = new Dictionary<string, List<OtherVar<float>>>();

        public Dictionary<string, SelfVar<int>> IntSelfVars { get; } = new Dictionary<string, SelfVar<int>>();
        public Dictionary<string, List<OtherVar<int>>> IntOtherVars { get; } = new Dictionary<string, List<OtherVar<int>>>();

        public Dictionary<string, SelfVar<string>> StringSelfVars { get; } = new Dictionary<string, SelfVar<string>>();
        public Dictionary<string, List<OtherVar<string>>> StringOtherVars { get; } = new Dictionary<string, List<OtherVar<string>>>();

        public SyncTestOtherVars(VarRegistry varRegistry, int numVarCollections, int OtherVarsPerCollection)
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


                var presenceBools = new List<OtherVar<bool>>();
                var presenceFloats = new List<OtherVar<float>>();
                var presenceInts = new List<OtherVar<int>>();
                var presenceStrings = new List<OtherVar<string>>();

                for (int j = 0; j < OtherVarsPerCollection; j++)
                {
                    var presenceBool = new OtherVar<bool>();
                    varRegistry.Register(boolKey, presenceBool);
                    presenceBools.Add(presenceBool);

                    var presenceFloat = new OtherVar<float>();
                    varRegistry.Register(floatKey, presenceFloat);
                    presenceFloats.Add(presenceFloat);

                    var presenceInt = new OtherVar<int>();
                    varRegistry.Register(intKey, presenceInt);
                    presenceInts.Add(presenceInt);

                    var presenceString = new OtherVar<string>();
                    varRegistry.Register(stringKey, presenceString);
                    presenceStrings.Add(presenceString);
                }

                BoolOtherVars[boolKey] = presenceBools;
                FloatOtherVars[floatKey] = presenceFloats;
                IntOtherVars[intKey] = presenceInts;
                StringOtherVars[stringKey] = presenceStrings;
            }
        }
    }
}
