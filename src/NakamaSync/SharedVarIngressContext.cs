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
    internal class SharedVarIngressContext<T>
    {
        public SharedVar<T> Var { get; }
        public SharedValue<T> Value { get; }
        public SharedVarAccessor<T> VarAccessor { get; }
        public AckAccessor AckAccessor { get; }

        public SharedVarIngressContext(SharedVar<T> var, SharedValue<T> value, SharedVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            Var = var;
            Value = value;
            VarAccessor = accessor;
            AckAccessor = ackAccessor;
        }
    }

    internal class SharedVarIngressContext
    {
        public static List<SharedVarIngressContext<bool>> FromBoolValues(Envelope envelope, VarRegistry registry)
        {
            return SharedVarIngressContext.FromValues<bool>(envelope.SharedBools, registry.SharedVarRegistry.SharedBools, env => env.SharedBools, env => env.SharedBoolAcks);
        }

        public static List<SharedVarIngressContext<bool>> FromBoolVars(Envelope envelope, VarRegistry registry)
        {
            return SharedVarIngressContext.FromValues<bool>(envelope.SharedBools, registry.SharedVarRegistry.SharedBools, env => env.SharedBools, env => env.SharedBoolAcks);
        }

        public static List<SharedVarIngressContext<float>> FromFloatValues(Envelope envelope, VarRegistry registry)
        {
            return SharedVarIngressContext.FromValues<float>(envelope.SharedFloats, registry.SharedVarRegistry.SharedFloats, env => env.SharedFloats, env => env.SharedFloatAcks);
        }

        public static List<SharedVarIngressContext<int>> FromIntValues(Envelope envelope, VarRegistry registry)
        {
            return SharedVarIngressContext.FromValues<int>(envelope.SharedInts, registry.SharedVarRegistry.SharedInts, env => env.SharedInts, env => env.SharedIntAcks);
        }

        public static List<SharedVarIngressContext<string>> FromStringValues(Envelope envelope, VarRegistry registry)
        {
            return FromValues(envelope.SharedStrings, registry.SharedVarRegistry.SharedStrings, env => env.SharedStrings, env => env.SharedStringAcks);
        }

        public static List<SharedVarIngressContext<object>> FromObjectValues(Envelope envelope, VarRegistry registry)
        {
            return FromValues(envelope.SharedObjects, registry.SharedVarRegistry.SharedObjects, env => env.SharedObjects, env => env.SharedObjectAcks);
        }

        private static List<SharedVarIngressContext<T>> FromValues<T>(List<SharedValue<T>> values, Dictionary<string, SharedVar<T>> vars, SharedVarAccessor<T> varAccessor, AckAccessor ackAccessor)
        {
            var contexts = new List<SharedVarIngressContext<T>>();

            foreach (SharedValue<T> value in values)
            {
                var context = new SharedVarIngressContext<T>(vars[value.Key], value, varAccessor, ackAccessor);
                contexts.Add(context);
            }

            return contexts;
        }
    }
}
