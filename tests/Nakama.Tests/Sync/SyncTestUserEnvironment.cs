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

    public class SyncTestUserEnvironment
    {
        public List<SharedVar<bool>> SharedBools { get; }
        public List<SharedVar<float>> SharedFloats { get; }
        public List<SharedVar<int>> SharedInts { get; }
        public List<SharedVar<string>> SharedStrings { get; }

        public List<PresenceVar<bool>> UserBools { get; }
        public List<PresenceVar<float>> UserFloats { get; }
        public List<PresenceVar<int>> UserInts { get; }
        public List<PresenceVar<string>> UserStrings { get; }

        public SyncTestUserEnvironment(ISession session, VarRegistry registry, int numTestVars, VarIdGenerator keyGenerator)
        {
            SharedBools = new List<SharedVar<bool>>();
            SharedFloats = new List<SharedVar<float>>();
            SharedInts = new List<SharedVar<int>>();
            SharedStrings = new List<SharedVar<string>>();

            UserBools = new List<PresenceVar<bool>>();
            UserFloats = new List<PresenceVar<float>>();
            UserInts = new List<PresenceVar<int>>();
            UserStrings = new List<PresenceVar<string>>();

            for (int i = 0; i < numTestVars; i++)
            {
                var sharedBool = new SharedVar<bool>();
                registry.Register(keyGenerator(session.UserId, nameof(sharedBool), i),  sharedBool);
                SharedBools.Add(sharedBool);

                var sharedFloat = new SharedVar<float>();
                registry.Register(keyGenerator(session.UserId, nameof(sharedFloat), i), sharedFloat);
                SharedFloats.Add(sharedFloat);

                var sharedInt = new SharedVar<int>();
                registry.Register(keyGenerator(session.UserId, nameof(sharedInt), i), sharedInt);
                SharedInts.Add(sharedInt);

                var sharedString = new SharedVar<string>();
                registry.Register(keyGenerator(session.UserId, nameof(sharedString), i), sharedString);
                SharedStrings.Add(sharedString);
            }

            var selfBool = new SelfVar<bool>();
            var selfFloat = new SelfVar<float>();
            var selfInt = new SelfVar<int>();
            var selfString = new SelfVar<string>();

            var presenceBools = new List<PresenceVar<bool>>();
            var presenceFloats = new List<PresenceVar<float>>();
            var presenceInts = new List<PresenceVar<int>>();
            var presenceStrings = new List<PresenceVar<string>>();

            for (int i = 0; i < numTestVars; i++)
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

            // todo key generator doesn't affect presence/self vars.
            if (numTestVars != 0)
            {
                registry.Register("presenceBools", selfBool, presenceBools);
                registry.Register("presenceFloats", selfFloat, presenceFloats);
                registry.Register("presenceInts", selfInt, presenceInts);
                registry.Register("presenceStrings", selfString, presenceStrings);
            }
        }

        public static string DefaultVarIdGenerator(string userId, string varName, int varIndex)
        {
            return varName + varIndex.ToString();
        }
    }
}
