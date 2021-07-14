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
    internal class SharedIngressContext<T>
    {
        public SharedVar<T> Var { get; }
        public SharedValue<T> Value { get; }
        public SharedVarAccessor<T> VarAccessor { get; }
        public AckAccessor AckAccessor { get; }

        public SharedIngressContext(SharedVar<T> var, SharedValue<T> value, SharedVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            Var = var;
            Value = value;
            VarAccessor = accessor;
            AckAccessor = ackAccessor;
        }
    }

    internal class SharedIngressContext
    {
        public static List<SharedIngressContext<bool>> FromBoolValues(Envelope envelope, VarRegistry registry)
        {
            return SharedIngressContext.FromValues<bool>(envelope.SharedBools, registry.SharedBools, env => env.SharedBools, env => env.SharedBoolAcks);
        }

        public static List<SharedIngressContext<bool>> FromBoolVars(Envelope envelope, VarRegistry registry)
        {
            return SharedIngressContext.FromValues<bool>(envelope.SharedBools, registry.SharedBools, env => env.SharedBools, env => env.SharedBoolAcks);
        }

        public static List<SharedIngressContext<float>> FromFloatValues(Envelope envelope, VarRegistry registry)
        {
            return SharedIngressContext.FromValues<float>(envelope.SharedFloats, registry.SharedFloats, env => env.SharedFloats, env => env.SharedFloatAcks);
        }

        public static List<SharedIngressContext<int>> FromIntValues(Envelope envelope, VarRegistry registry)
        {
            return SharedIngressContext.FromValues<int>(envelope.SharedInts, registry.SharedInts, env => env.SharedInts, env => env.SharedIntAcks);
        }

        public static List<SharedIngressContext<string>> FromStringValues(Envelope envelope, VarRegistry registry)
        {
            return FromValues(envelope.SharedStrings, registry.SharedStrings, env => env.SharedStrings, env => env.SharedStringAcks);
        }

        private static List<SharedIngressContext<T>> FromValues<T>(List<SharedValue<T>> values, Dictionary<string, SharedVar<T>> vars, SharedVarAccessor<T> varAccessor, AckAccessor ackAccessor)
        {
            var contexts = new List<SharedIngressContext<T>>();

            foreach (SharedValue<T> value in values)
            {
                var context = new SharedIngressContext<T>(vars[value.Key], value, varAccessor, ackAccessor);
                contexts.Add(context);
            }

            return contexts;
        }
    }
}
