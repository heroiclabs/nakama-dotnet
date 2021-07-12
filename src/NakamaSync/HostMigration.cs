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
    internal class HostMigration
    {
        public void Migrate(SyncVarRegistry registry)
        {
            ValidatePendingVars<SharedVar<bool>>(registry.SharedBools);
            ValidatePendingVars<SharedVar<float>>(registry.SharedFloats);
            ValidatePendingVars<SharedVar<int>>(registry.SharedInts);
            ValidatePendingVars<SharedVar<string>>(registry.SharedStrings);

            ValidatePendingVars<UserVar<bool>>(registry.UserBools);
            ValidatePendingVars<UserVar<float>>(registry.UserFloats);
            ValidatePendingVars<UserVar<int>>(registry.UserInts);
            ValidatePendingVars<UserVar<string>>(registry.UserStrings);
        }

        private void ValidatePendingVars<TVar>(Dictionary<string, TVar> vars) where TVar : IVar
        {
            foreach (KeyValuePair<string, TVar> kvp in vars)
            {
                if (vars[kvp.Key].GetValidationStatus() == KeyValidationStatus.Pending)
                {
                    // todo do
                }
            }
        }
    }
}