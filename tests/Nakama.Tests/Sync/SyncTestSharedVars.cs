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
        public SharedVar<bool> SharedBool { get; }
        public SharedVar<Dictionary<string, string>> SharedDict { get; }
        public SharedVar<float> SharedFloat { get; }
        public SharedVar<int> SharedInt { get; }
        public SharedVar<SyncTestObject> SharedObject { get; }
        public SharedVar<string> SharedString { get; }
        public SharedVar<Dictionary<object, object>> SharedAnonymousDict { get; }


        public SyncTestSharedVars(string userId, VarRegistry registry, bool delayRegistration)
        {
            SharedBool = new SharedVar<bool>(100);
            SharedDict = new SharedVar<Dictionary<string, string>>(101);
            SharedFloat = new SharedVar<float>(102);
            SharedInt = new SharedVar<int>(103);
            SharedObject = new SharedVar<SyncTestObject>(104);
            SharedString = new SharedVar<string>(105);
            SharedAnonymousDict = new SharedVar<Dictionary<object, object>>(106);

            if (!delayRegistration)
            {
                registry.Register(SharedBool);
                registry.Register(SharedDict);
                registry.Register(SharedFloat);
                registry.Register(SharedInt);
                registry.Register(SharedObject);
                registry.Register(SharedString);
                registry.Register(SharedAnonymousDict);
            }
        }
    }
}
