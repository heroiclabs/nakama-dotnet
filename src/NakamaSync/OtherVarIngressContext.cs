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
    internal class OtherVarIngressContext<T>
    {
        public OtherVar<T> Var { get; }
        public PresenceValue<T> Value { get; }
        public OtherVarAccessor<T> VarAccessor { get; }
        public AckAccessor AckAccessor { get; }

        public OtherVarIngressContext(OtherVar<T> var, PresenceValue<T> value, OtherVarAccessor<T> accessor, AckAccessor ackAccessor)
        {
            Var = var;
            Value = value;
            VarAccessor = accessor;
            AckAccessor = ackAccessor;
        }
    }

    internal class OtherVarIngressContext
    {
        public static List<OtherVarIngressContext<bool>> FromBoolValues(string selfId, Envelope envelope, OtherVarRegistry registry, OtherVarRotators OtherVarRotators)
        {
            return OtherVarIngressContext.FromValues<bool>(selfId, envelope.PresenceBools, registry.PresenceBools, env => env.PresenceBools, env => env.PresenceBoolAcks, OtherVarRotators.GetPresenceBoolRotator);
        }

        public static List<OtherVarIngressContext<float>> FromFloatValues(string selfId, Envelope envelope, OtherVarRegistry registry, OtherVarRotators OtherVarRotators)
        {
            return OtherVarIngressContext.FromValues<float>(selfId, envelope.PresenceFloats, registry.PresenceFloats, env => env.PresenceFloats, env => env.PresenceFloatAcks, OtherVarRotators.GetPresenceFloatRotator);
        }

        public static List<OtherVarIngressContext<int>> FromIntValues(string selfId, Envelope envelope, OtherVarRegistry registry, OtherVarRotators OtherVarRotators)
        {
            return OtherVarIngressContext.FromValues<int>(selfId, envelope.PresenceInts, registry.PresenceInts, env => env.PresenceInts, env => env.PresenceIntAcks, OtherVarRotators.GetPresenceIntRotator);
        }

        public static List<OtherVarIngressContext<string>> FromStringValues(string selfId, Envelope envelope, OtherVarRegistry registry, OtherVarRotators OtherVarRotators)
        {
            return OtherVarIngressContext.FromValues<string>(selfId, envelope.PresenceStrings, registry.PresenceStrings, env => env.PresenceStrings, env => env.PresenceStringAcks, OtherVarRotators.GetPresenceStringRotator);
        }

        private static List<OtherVarIngressContext<T>> FromValues<T>(string selfId, List<PresenceValue<T>> values, Dictionary<string, OtherVarCollection<T>> vars, OtherVarAccessor<T> varAccessor, AckAccessor ackAccessor, OtherVarRotatorAccessor<T> rotatorAccessor)
        {
            var contexts = new List<OtherVarIngressContext<T>>();

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

                OtherVarCollection<T> collection = vars[value.Key.CollectionKey];
                OtherVarRotator<T> OtherVarRotator = rotatorAccessor(value.Key.CollectionKey);

                if (OtherVarRotator.AssignedOtherVars.ContainsKey(value.Key.UserId))
                {
                    var assignedOtherVar = OtherVarRotator.AssignedOtherVars[value.Key.UserId];
                    assignedOtherVar.SetValue(value.Value, value.ValidationStatus);
                    contexts.Add(new OtherVarIngressContext<T>(assignedOtherVar, value, varAccessor, ackAccessor));
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
