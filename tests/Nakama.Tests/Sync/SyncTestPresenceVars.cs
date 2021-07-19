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
        public List<PresenceVarCollection<bool>> PresenceBoolCollections { get; }
        public List<PresenceVarCollection<float>> PresenceFloatCollections { get; }
        public List<PresenceVarCollection<int>> PresenceIntCollections { get; }
        public List<PresenceVarCollection<string>> PresenceStringCollections { get; }

        public SyncTestPresenceVars(VarRegistry varRegistry, int numVarCollections, int presenceVarsPerCollection)
        {
            PresenceBoolCollections = new List<PresenceVarCollection<bool>>();
            PresenceFloatCollections = new List<PresenceVarCollection<float>>();
            PresenceIntCollections = new List<PresenceVarCollection<int>>();
            PresenceStringCollections = new List<PresenceVarCollection<string>>();

            for (int i = 0; i < numVarCollections; i++)
            {
                var selfBool = new SelfVar<bool>();
                var selfFloat = new SelfVar<float>();
                var selfInt = new SelfVar<int>();
                var selfString = new SelfVar<string>();

                var presenceBools = new List<PresenceVar<bool>>();
                var presenceFloats = new List<PresenceVar<float>>();
                var presenceInts = new List<PresenceVar<int>>();
                var presenceStrings = new List<PresenceVar<string>>();

                for (int j = 0; j < presenceVarsPerCollection; j++)
                {
                    var presenceBool = new PresenceVar<bool>();
                    presenceBools.Add(presenceBool);

                    var presenceFloat = new PresenceVar<float>();
                    presenceFloats.Add(presenceFloat);

                    var presenceInt = new PresenceVar<int>();
                    presenceInts.Add(presenceInt);

                    var presenceString = new PresenceVar<string>();
                    presenceStrings.Add(presenceString);
                }

                var boolCollection = new PresenceVarCollection<bool>(selfBool, presenceBools);
                PresenceBoolCollections.Add(boolCollection);
                varRegistry.Register("presenceBools_" + i, boolCollection);

                var floatCollection = new PresenceVarCollection<float>(selfFloat, presenceFloats);
                PresenceFloatCollections.Add(floatCollection);
                varRegistry.Register("presenceFloats_" + i, floatCollection);

                var intCollection = new PresenceVarCollection<int>(selfInt, presenceInts);
                PresenceIntCollections.Add(intCollection);
                varRegistry.Register("presenceInts_" + i, intCollection);

                var stringCollection = new PresenceVarCollection<string>(selfString, presenceStrings);
                PresenceStringCollections.Add(stringCollection);
                varRegistry.Register("presenceStrings_" + i, stringCollection);
            }
        }
    }
}
