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
    internal class PresenceIngressContext<T>
    {
        public PresenceVar<T> Var { get; }
        public PresenceValue<T> Value { get; }
        public PresenceVarAccessor<T> VarAccessor { get; }
        public AckAccessor AckAccessor { get; }

        public PresenceIngressContext(PresenceVar<T> var, PresenceValue<T> value, PresenceVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            Var = var;
            Value = value;
            VarAccessor = accessor;
            AckAccessor = ackAccessor;
        }
    }

    internal class UserIngressContext
    {
        public static List<PresenceIngressContext<bool>> FromBoolValues(Envelope envelope, VarRegistry registry)
        {
            return UserIngressContext.FromValues<bool>(envelope.PresenceBools, registry.UserBools, env => env.PresenceBools, env => env.PresenceBoolAcks);
        }

        public static List<PresenceIngressContext<float>> FromFloatValues(Envelope envelope, VarRegistry registry)
        {
            return UserIngressContext.FromValues<float>(envelope.PresenceFloats, registry.UserFloats, env => env.PresenceFloats, env => env.PresenceFloatAcks);
        }

        public static List<PresenceIngressContext<int>> FromIntValues(Envelope envelope, VarRegistry registry)
        {
            return UserIngressContext.FromValues<int>(envelope.PresenceInts, registry.UserInts, env => env.PresenceInts, env => env.PresenceIntAcks);
        }

        public static List<PresenceIngressContext<string>> FromStringValues(Envelope envelope, VarRegistry registry)
        {
            return UserIngressContext.FromValues<string>(envelope.PResenceStrings, registry.UserStrings, env => env.PResenceStrings, env => env.PresenceStringAcks);
        }

        private static List<PresenceIngressContext<T>> FromValues<T>(List<PresenceValue<T>> values, Dictionary<string, PresenceVar<T>> vars, PresenceVarAccessor<T> varAccessor, AckAccessor ackAccessor)
        {
            var contexts = new List<PresenceIngressContext<T>>();

            foreach (PresenceValue<T> value in values)
            {
                var context = new PresenceIngressContext<T>(vars[value.Key], value, varAccessor, ackAccessor);
                contexts.Add(context);
            }

            return contexts;
        }
    }
}
