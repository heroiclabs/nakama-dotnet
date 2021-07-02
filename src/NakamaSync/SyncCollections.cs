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

namespace NakamaSync
{
    internal class SyncCollections
    {
        public SharedVarCollections<bool> SharedBoolCollections { get; }
        public SharedVarCollections<float> SharedFloatCollections { get; }
        public SharedVarCollections<int> SharedIntCollections { get; }
        public SharedVarCollections<string> SharedStringCollections { get; }

        public UserVarCollections<bool> UserBoolCollections { get; }
        public UserVarCollections<float> UserFloatCollections { get; }
        public UserVarCollections<int> UserIntCollections { get; }
        public UserVarCollections<string> UserStringCollections { get; }

        public SyncCollections(SyncVarRegistryInternal registry, SyncValues outgoingToAll, SyncValues outgoingToHost, Dictionary<string, SyncValues> outgoingToGuest, SyncValues incomingValues)
        {
            SharedBoolCollections = new SharedVarCollections<bool>(outgoingToAll.SharedBools, outgoingToHost.SharedBools, outgoingToGuest.SharedBools, incomingValues.SharedBools, registry.SharedBools);
            SharedFloatCollections = new SharedVarCollections<float>(outgoingToAll.SharedFloats, outgoingToHost.SharedFloats, outgoingToGuest.SharedFloats, incomingValues.SharedFloats, registry.SharedFloats);
            SharedIntCollections = new SharedVarCollections<int>(outgoingToAll.SharedInts, outgoingToHost.SharedInts, outgoingToGuest.SharedInts, incomingValues.SharedInts, registry.SharedInts);
            SharedStringCollections = new SharedVarCollections<string>(outgoingToAll.SharedStrings, outgoingToHost.SharedStrings, outgoingToGuest.SharedStrings, incomingValues.SharedStrings, registry.SharedStrings);

            UserBoolCollections = new UserVarCollections<bool>(outgoingToAll.UserBools, outgoingToHost.UserBools, outgoingToGuest.UserBools, incomingValues.UserBools, registry.UserBools);
            UserFloatCollections = new UserVarCollections<float>(outgoingToAll.UserFloats, outgoingToHost.UserFloats, outgoingToGuest.UserFloats, incomingValues.UserFloats, registry.UserFloats);
            UserIntCollections = new UserVarCollections<int>(outgoingToAll.UserInts, outgoingToHost.UserInts, outgoingToGuest.UserInts, incomingValues.UserInts, registry.UserInts);
            UserStringCollections = new UserVarCollections<string>(outgoingToAll.UserStrings, outgoingToHost.UserStrings, outgoingToGuest.UserStrings, incomingValues.UserStrings, registry.UserStrings);
        }
    }
    internal class SharedVarCollections<T>
    {
        public List<SyncSharedValue<T>> SharedValuesToAll { get; }
        public List<SyncSharedValue<T>> SharedValuesToHost { get; }
        public GuestSharedValues<T> SharedValuesToGuest { get; }
        public List<SyncSharedValue<T>> SharedValuesIncoming { get; }

        public SyncVarDictionary<SyncVarKey, SharedVar<T>> SharedVars { get; }

        public SharedVarCollections(
            List<SyncSharedValue<T>> sharedValuesToAll,
            List<SyncSharedValue<T>> sharedValuesToHost,
            GuestSharedValues<T> sharedValuesToGuest,
            List<SyncSharedValue<T>> sharedValuesIncoming,
            SyncVarDictionary<SyncVarKey, SharedVar<T>> sharedVars)
        {
            SharedValuesToAll = sharedValuesToAll;
            SharedValuesToHost = sharedValuesToHost;
            SharedValuesToGuest = sharedValuesToGuest;
            SharedValuesIncoming = sharedValuesIncoming;
            SharedVars = sharedVars;
        }
    }

    internal class UserVarCollections<T>
    {
        public List<SyncUserValue<T>> UserValuesToAll { get; }
        public List<SyncUserValue<T>> UserValuesToHost { get; }
        public GuestUserValues<T> UserValuesToGuest { get; }
        public List<SyncUserValue<T>> UserValuesIncoming { get; }

        public SyncVarDictionary<SyncVarKey, UserVar<T>> UserVars { get; }

        public UserVarCollections(
            List<SyncUserValue<T>> userValuesToAll,
            List<SyncUserValue<T>> userValuesToHost,
            GuestUserValues<T> userValuesToGuest,
            List<SyncUserValue<T>> userValuesIncoming,
            SyncVarDictionary<SyncVarKey, UserVar<T>> userVars)
        {
            UserValuesToAll = userValuesToAll;
            UserValuesToHost = userValuesToHost;
            UserValuesToGuest = userValuesToGuest;
            UserValuesIncoming = userValuesIncoming;
            UserVars = userVars;
        }
    }
}
