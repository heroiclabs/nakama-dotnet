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
    internal class SharedContext<T>
    {
        public SharedVar<T> Var { get; }
        public SharedValue<T> Value { get; }
        public SharedVarAccessor<T> VarAccessor { get; }
        public AckAccessor AckAccessor { get; }

        public SharedContext(SharedVar<T> var, SharedValue<T> value, SharedVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            Var = var;
            Value = value;
            VarAccessor = accessor;
            AckAccessor = ackAccessor;
        }
    }

    internal class SharedContext
    {
        public static List<SharedContext<bool>> FromBoolValues(Envelope envelope, SyncVarRegistry registry)
        {
            return SharedContext.FromValues<bool>(envelope.SharedBools, registry.SharedBools, env => env.SharedBools, env => env.SharedBoolAcks);
        }

        public static List<SharedContext<bool>> FromBoolVars(Envelope envelope, SyncVarRegistry registry)
        {
            return SharedContext.FromValues<bool>(envelope.SharedBools, registry.SharedBools, env => env.SharedBools, env => env.SharedBoolAcks);
        }

        public static List<SharedContext<float>> FromFloatValues(Envelope envelope, SyncVarRegistry registry)
        {
            return SharedContext.FromValues<float>(envelope.SharedFloats, registry.SharedFloats, env => env.SharedFloats, env => env.SharedFloatAcks);
        }

        public static List<SharedContext<int>> FromIntValues(Envelope envelope, SyncVarRegistry registry)
        {
            return SharedContext.FromValues<int>(envelope.SharedInts, registry.SharedInts, env => env.SharedInts, env => env.SharedIntAcks);
        }

        public static List<SharedContext<string>> FromStringValues(Envelope envelope, SyncVarRegistry registry)
        {
            return FromValues(envelope.SharedStrings, registry.SharedStrings, env => env.SharedStrings, env => env.SharedStringAcks);
        }

        private static List<SharedContext<T>> FromValues<T>(List<SharedValue<T>> values, Dictionary<string, SharedVar<T>> vars, SharedVarAccessor<T> varAccessor, AckAccessor ackAccessor)
        {
            var contexts = new List<SharedContext<T>>();

            foreach (SharedValue<T> value in values)
            {
                var context = new SharedContext<T>(vars[value.Key], value, varAccessor, ackAccessor);
                contexts.Add(context);
            }

            return contexts;
        }
    }
}
