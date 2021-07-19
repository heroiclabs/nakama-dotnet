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
    public class SyncTestPresenceVars
    {
        public List<PresenceVarCollection<bool>> PresenceBools { get; }
        public List<PresenceVarCollection<float>> PresenceFloats { get; }
        public List<PresenceVarCollection<int>> PresenceInts { get; }
        public List<PresenceVarCollection<string>> PresenceStrings { get; }

        public SyncTestPresenceVars(VarRegistry varRegistry, int numVarCollections, int presenceVarsPerCollection)
        {
            PresenceBools = new List<PresenceVarCollection<bool>>();
            PresenceFloats = new List<PresenceVarCollection<float>>();
            PresenceInts = new List<PresenceVarCollection<int>>();
            PresenceStrings = new List<PresenceVarCollection<string>>();

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

                varRegistry.Register("presenceBools_" + i, new PresenceVarCollection<bool>(selfBool, presenceBools));
                varRegistry.Register("presenceFloats_" + i, new PresenceVarCollection<float>(selfFloat, presenceFloats));
                varRegistry.Register("presenceInts_" + i, new PresenceVarCollection<int>(selfInt, presenceInts));
                varRegistry.Register("presenceStrings_" + i, new PresenceVarCollection<string>(selfString, presenceStrings));
            }
        }
    }
}
