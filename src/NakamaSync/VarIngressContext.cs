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
    internal class VarIngressContext<T>
    {
        public IVar<T> Var { get; }
        public VarValue<T> Value { get; }
        public VarValueAccessor<T> VarAccessor { get; }

        public VarIngressContext(IVar<T> var, VarValue<T> value, VarValueAccessor<T> accessor)
        {
            Var = var;
            Value = value;
            VarAccessor = accessor;
        }
    }

    internal class VarIngressContext
    {
        public static List<VarIngressContext<bool>> FromBoolValues(Envelope envelope, VarRegistry registry)
        {
            return VarIngressContext.FromValues<bool>(envelope.Bools, registry.Bools, env => env.Bools);
        }

        public static List<VarIngressContext<float>> FromFloatValues(Envelope envelope, VarRegistry registry)
        {
            return VarIngressContext.FromValues<float>(envelope.Floats, registry.Floats, env => env.Floats);
        }

        public static List<VarIngressContext<int>> FromIntValues(Envelope envelope, VarRegistry registry)
        {
            return VarIngressContext.FromValues<int>(envelope.Ints, registry.Ints, env => env.Ints);
        }

        public static List<VarIngressContext<string>> FromStringValues(Envelope envelope, VarRegistry registry)
        {
            return FromValues(envelope.Strings, registry.Strings, env => env.Strings);
        }

        public static List<VarIngressContext<object>> FromObjectValues(Envelope envelope, VarRegistry registry)
        {
            return FromValues(envelope.Objects, registry.Objects, env => env.Objects);
        }

        private static List<VarIngressContext<T>> FromValues<T>(List<VarValue<T>> values, Dictionary<string, List<IVar<T>>> vars, VarValueAccessor<T> varAccessor)
        {
            var contexts = new List<VarIngressContext<T>>();

            foreach (VarValue<T> value in values)
            {
                foreach (var kvp in vars)
                {
                    foreach (var var in kvp.Value)
                    {
                        var context = new VarIngressContext<T>(var, value, varAccessor);
                        contexts.Add(context);
                    }
                }
            }

            return contexts;
        }
    }
}
