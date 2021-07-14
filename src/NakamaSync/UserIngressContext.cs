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
    internal class UserIngressContext<T>
    {
        public UserVar<T> Var { get; }
        public UserValue<T> Value { get; }
        public UserVarAccessor<T> VarAccessor { get; }
        public AckAccessor AckAccessor { get; }

        public UserIngressContext(UserVar<T> var, UserValue<T> value, UserVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            Var = var;
            Value = value;
            VarAccessor = accessor;
            AckAccessor = ackAccessor;
        }
    }

    internal class UserIngressContext
    {
        public static List<UserIngressContext<bool>> FromBoolValues(Envelope envelope, VarRegistry registry)
        {
            return UserIngressContext.FromValues<bool>(envelope.UserBools, registry.UserBools, env => env.UserBools, env => env.UserBoolAcks);
        }

        public static List<UserIngressContext<float>> FromFloatValues(Envelope envelope, VarRegistry registry)
        {
            return UserIngressContext.FromValues<float>(envelope.UserFloats, registry.UserFloats, env => env.UserFloats, env => env.UserFloatAcks);
        }

        public static List<UserIngressContext<int>> FromIntValues(Envelope envelope, VarRegistry registry)
        {
            return UserIngressContext.FromValues<int>(envelope.UserInts, registry.UserInts, env => env.UserInts, env => env.UserIntAcks);
        }

        public static List<UserIngressContext<string>> FromStringValues(Envelope envelope, VarRegistry registry)
        {
            return UserIngressContext.FromValues<string>(envelope.UserStrings, registry.UserStrings, env => env.UserStrings, env => env.UserStringAcks);
        }

        private static List<UserIngressContext<T>> FromValues<T>(List<UserValue<T>> values, Dictionary<string, UserVar<T>> vars, UserVarAccessor<T> varAccessor, AckAccessor ackAccessor)
        {
            var contexts = new List<UserIngressContext<T>>();

            foreach (UserValue<T> value in values)
            {
                var context = new UserIngressContext<T>(vars[value.Key], value, varAccessor, ackAccessor);
                contexts.Add(context);
            }

            return contexts;
        }
    }
}
