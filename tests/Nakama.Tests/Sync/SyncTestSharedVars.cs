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
    public class SyncTestSharedVars
    {
        public List<SharedVar<bool>> SharedBools { get; }
        public List<SharedVar<float>> SharedFloats { get; }
        public List<SharedVar<int>> SharedInts { get; }
        public List<SharedVar<string>> SharedStrings { get; }
        public List<SharedVar<IDictionary<string, string>>> SharedDicts { get; }

        public SyncTestSharedVars(string userId, VarRegistry registry, int numTestVars, VarIdGenerator keyGenerator)
        {
            SharedBools = new List<SharedVar<bool>>();
            SharedFloats = new List<SharedVar<float>>();
            SharedInts = new List<SharedVar<int>>();
            SharedStrings = new List<SharedVar<string>>();
            SharedDicts = new List<SharedVar<IDictionary<string, string>>>();

            for (int i = 0; i < numTestVars; i++)
            {
                var sharedBool = new SharedVar<bool>(keyGenerator(userId, "sharedBool", i));
                registry.Register(sharedBool);
                SharedBools.Add(sharedBool);

                var sharedFloat = new SharedVar<float>(keyGenerator(userId, "sharedFloat", i));
                registry.Register(sharedFloat);
                SharedFloats.Add(sharedFloat);

                var sharedInt = new SharedVar<int>(keyGenerator(userId, "sharedInt", i));
                registry.Register(sharedInt);
                SharedInts.Add(sharedInt);

                var sharedString = new SharedVar<string>(keyGenerator(userId, "sharedString", i));
                registry.Register(sharedString);
                SharedStrings.Add(sharedString);

                var sharedObject = new SharedVar<IDictionary<string, string>>(keyGenerator(userId, "sharedObject", i));
                registry.Register(sharedObject);
                SharedDicts.Add(sharedObject);
            }
        }
    }
}
