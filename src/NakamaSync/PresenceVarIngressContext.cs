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

using System;
using System.Collections.Generic;

namespace NakamaSync
{
    internal class PresenceVarIngressContext<T>
    {
        public PresenceVar<T> Var { get; }
        public PresenceValue<T> Value { get; }
        public PresenceVarAccessor<T> VarAccessor { get; }
        public AckAccessor AckAccessor { get; }

        public PresenceVarIngressContext(PresenceVar<T> var, PresenceValue<T> value, PresenceVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            Var = var;
            Value = value;
            VarAccessor = accessor;
            AckAccessor = ackAccessor;
        }
    }

    internal class PresenceVarIngressContext
    {
        public static List<PresenceVarIngressContext<bool>> FromBoolValues(string selfId, Envelope envelope, PresenceVarRegistry registry, PresenceVarRotators presenceVarRotators)
        {
            return PresenceVarIngressContext.FromValues<bool>(selfId, envelope.PresenceBools, registry.PresenceBools, env => env.PresenceBools, env => env.PresenceBoolAcks, presenceVarRotators.GetPresenceBoolRotator);
        }

        public static List<PresenceVarIngressContext<float>> FromFloatValues(string selfId, Envelope envelope, PresenceVarRegistry registry, PresenceVarRotators presenceVarRotators)
        {
            return PresenceVarIngressContext.FromValues<float>(selfId, envelope.PresenceFloats, registry.PresenceFloats, env => env.PresenceFloats, env => env.PresenceFloatAcks, presenceVarRotators.GetPresenceFloatRotator);
        }

        public static List<PresenceVarIngressContext<int>> FromIntValues(string selfId, Envelope envelope, PresenceVarRegistry registry, PresenceVarRotators presenceVarRotators)
        {
            return PresenceVarIngressContext.FromValues<int>(selfId, envelope.PresenceInts, registry.PresenceInts, env => env.PresenceInts, env => env.PresenceIntAcks, presenceVarRotators.GetPresenceIntRotator);
        }

        public static List<PresenceVarIngressContext<string>> FromStringValues(string selfId, Envelope envelope, PresenceVarRegistry registry, PresenceVarRotators presenceVarRotators)
        {
            return PresenceVarIngressContext.FromValues<string>(selfId, envelope.PresenceStrings, registry.PresenceStrings, env => env.PresenceStrings, env => env.PresenceStringAcks, presenceVarRotators.GetPresenceStringRotator);
        }

        private static List<PresenceVarIngressContext<T>> FromValues<T>(string selfId, List<PresenceValue<T>> values, Dictionary<string, PresenceVarCollection<T>> vars, PresenceVarAccessor<T> varAccessor, AckAccessor ackAccessor, PresenceVarRotatorAccessor<T> rotatorAccessor)
        {
            var contexts = new List<PresenceVarIngressContext<T>>();

            foreach (PresenceValue<T> value in values)
            {
                if (!vars.ContainsKey(value.Key.CollectionKey))
                {
                    // todo find a way to continue looping but still bubble up exception.
                    throw new InvalidOperationException($"No var found with collection key {value.Key.CollectionKey}");
                }

                if (value.Key.UserId == selfId)
                {
                    // self is not tracked by presence vars
                    continue;
                }

                PresenceVarCollection<T> collection = vars[value.Key.CollectionKey];
                PresenceVarRotator<T> presenceVarRotator = rotatorAccessor(value.Key.CollectionKey);

                if (presenceVarRotator.AssignedPresenceVars.ContainsKey(value.Key.UserId))
                {
                    var assignedPresenceVar = presenceVarRotator.AssignedPresenceVars[value.Key.UserId];
                    assignedPresenceVar.SetValue(value.Value, value.ValidationStatus);
                    contexts.Add(new PresenceVarIngressContext<T>(assignedPresenceVar, value, varAccessor, ackAccessor));
                }
                else
                {
                    throw new InvalidOperationException($"Presence var rotator for did not recognize value belonging to user id: {value.Key.UserId}");
                }

            }

            return contexts;
        }
    }
}
