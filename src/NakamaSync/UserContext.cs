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
    internal class UserContext<T>
    {
        public UserVar<T> Var { get; }
        public UserValue<T> Value { get; }
        public UserVarAccessor<T> VarAccessor { get; }
        public AckAccessor AckAccessor { get; }

        private UserContext(UserVar<T> var, UserValue<T> value, UserVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            Var = var;
            Value = value;
            VarAccessor = accessor;
            AckAccessor = ackAccessor;
        }

        public static List<UserContext<T>> Create(Dictionary<string, UserVar<T>> vars, List<UserValue<T>> values, UserVarAccessor<T> varAccessor, AckAccessor ackAccessor)
        {
            var contexts = new List<UserContext<T>>();

            foreach (UserValue<T> value in values)
            {
                var context = new UserContext<T>(vars[value.Key], value, varAccessor, ackAccessor);
                contexts.Add(context);
            }

            return contexts;
        }
    }
}