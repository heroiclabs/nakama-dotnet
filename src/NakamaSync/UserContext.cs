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

    // todo I think only the ingresses use the full context? is that something that makes sense?
    // should they be renamed to IngressContext?
    internal class UserContext<T>
    {
        public UserVar<T> Var { get; }
        public UserValue<T> Value { get; }
        public UserVarAccessor<T> VarAccessor { get; }
        public AckAccessor AckAccessor { get; }

        public UserContext(UserVar<T> var, UserValue<T> value, UserVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            Var = var;
            Value = value;
            VarAccessor = accessor;
            AckAccessor = ackAccessor;
        }
    }

    internal class UserContext
    {
        public static List<UserContext<bool>> FromBoolValues(Envelope envelope, SyncVarRegistry registry)
        {
            return UserContext.FromValues<bool>(envelope.UserBools, registry.UserBools, env => env.UserBools, env => env.UserBoolAcks);
        }

        public static List<UserContext<float>> FromFloatValues(Envelope envelope, SyncVarRegistry registry)
        {
            return UserContext.FromValues<float>(envelope.UserFloats, registry.UserFloats, env => env.UserFloats, env => env.UserFloatAcks);
        }

        public static List<UserContext<int>> FromIntValues(Envelope envelope, SyncVarRegistry registry)
        {
            return UserContext.FromValues<int>(envelope.UserInts, registry.UserInts, env => env.UserInts, env => env.UserIntAcks);
        }

        public static List<UserContext<string>> FromStringValues(Envelope envelope, SyncVarRegistry registry)
        {
            return UserContext.FromValues<string>(envelope.UserStrings, registry.UserStrings, env => env.UserStrings, env => env.UserStringAcks);
        }

        private static List<UserContext<T>> FromValues<T>(List<UserValue<T>> values, Dictionary<string, UserVar<T>> vars, UserVarAccessor<T> varAccessor, AckAccessor ackAccessor)
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
