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
    internal class IncomingVarIngressContext<T>
    {
        public IIncomingVar<T> Var { get; }
        public VarValue<T> Value { get; }
        public VarValueAccessor<T> VarAccessor { get; }
        public AckAccessor AckAccessor { get; }

        public IncomingVarIngressContext(IIncomingVar<T> var, VarValue<T> value, VarValueAccessor<T> accessor, AckAccessor ackAccessor)
        {
            Var = var;
            Value = value;
            VarAccessor = accessor;
            AckAccessor = ackAccessor;
        }
    }

    internal class IncomingVarIngressContext
    {
        public static List<IncomingVarIngressContext<bool>> FromBoolValues(Envelope envelope, VarRegistry registry)
        {
            return IncomingVarIngressContext.FromValues<bool>(envelope.Bools, registry.IncomingVarRegistry.Bools, env => env.Bools, env => env.SharedBoolAcks);
        }

        public static List<IncomingVarIngressContext<float>> FromFloatValues(Envelope envelope, VarRegistry registry)
        {
            return IncomingVarIngressContext.FromValues<float>(envelope.Floats, registry.IncomingVarRegistry.Floats, env => env.Floats, env => env.SharedFloatAcks);
        }

        public static List<IncomingVarIngressContext<int>> FromIntValues(Envelope envelope, VarRegistry registry)
        {
            return IncomingVarIngressContext.FromValues<int>(envelope.Ints, registry.IncomingVarRegistry.Ints, env => env.Ints, env => env.SharedIntAcks);
        }

        public static List<IncomingVarIngressContext<string>> FromStringValues(Envelope envelope, VarRegistry registry)
        {
            return FromValues(envelope.Strings, registry.IncomingVarRegistry.Strings, env => env.Strings, env => env.SharedStringAcks);
        }

        public static List<IncomingVarIngressContext<object>> FromObjectValues(Envelope envelope, VarRegistry registry)
        {
            return FromValues(envelope.Objects, registry.IncomingVarRegistry.Objects, env => env.Objects, env => env.SharedObjectAcks);
        }

        private static List<IncomingVarIngressContext<T>> FromValues<T>(List<VarValue<T>> values, Dictionary<string, IIncomingVar<T>> vars, VarValueAccessor<T> varAccessor, AckAccessor ackAccessor)
        {
            var contexts = new List<IncomingVarIngressContext<T>>();

            foreach (VarValue<T> value in values)
            {
                var context = new IncomingVarIngressContext<T>(vars[value.Key], value, varAccessor, ackAccessor);
                contexts.Add(context);
            }

            return contexts;
        }
    }
}
