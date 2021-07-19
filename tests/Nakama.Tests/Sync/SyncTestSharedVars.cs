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
    public delegate string VarIdGenerator(string userId, string varName, int varIndex);

    public class SyncTestSharedVars
    {
        public List<SharedVar<bool>> SharedBools { get; }
        public List<SharedVar<float>> SharedFloats { get; }
        public List<SharedVar<int>> SharedInts { get; }
        public List<SharedVar<string>> SharedStrings { get; }

        public SyncTestSharedVars(string userId, VarRegistry registry, int numTestVars, VarIdGenerator keyGenerator)
        {
            SharedBools = new List<SharedVar<bool>>();
            SharedFloats = new List<SharedVar<float>>();
            SharedInts = new List<SharedVar<int>>();
            SharedStrings = new List<SharedVar<string>>();

            for (int i = 0; i < numTestVars; i++)
            {
                var sharedBool = new SharedVar<bool>();
                registry.Register(keyGenerator(userId, nameof(sharedBool), i),  sharedBool);
                SharedBools.Add(sharedBool);

                var sharedFloat = new SharedVar<float>();
                registry.Register(keyGenerator(userId, nameof(sharedFloat), i), sharedFloat);
                SharedFloats.Add(sharedFloat);

                var sharedInt = new SharedVar<int>();
                registry.Register(keyGenerator(userId, nameof(sharedInt), i), sharedInt);
                SharedInts.Add(sharedInt);

                var sharedString = new SharedVar<string>();
                registry.Register(keyGenerator(userId, nameof(sharedString), i), sharedString);
                SharedStrings.Add(sharedString);
            }
        }

        public static string DefaultVarIdGenerator(string userId, string varName, int varIndex)
        {
            return varName + varIndex.ToString();
        }
    }
}
