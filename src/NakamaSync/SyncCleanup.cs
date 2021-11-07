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
using System.Linq;
using Nakama;

namespace NakamaSync
{
    // TODO implement this
    internal class SyncCleanup : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly string _userId;
        private readonly VarRegistry _varRegistry;
        private readonly RpcTargetRegistry _registry;

        public SyncCleanup(string userId, VarRegistry varRegistry, RpcTargetRegistry registry)
        {
            _userId = userId;
            _varRegistry = varRegistry;
            _registry = registry;
        }

        public void Subscribe(PresenceTracker presenceTracker)
        {
            presenceTracker.OnPresenceRemoved += HandlePresenceRemoved;
        }

        private void HandlePresenceRemoved(IUserPresence leaver)
        {
            if (leaver.UserId == _userId)
            {
                ResetVars(_varRegistry.Bools.Values);
                ResetVars(_varRegistry.Floats.Values);
                ResetVars(_varRegistry.Ints.Values);
                ResetVars(_varRegistry.Strings.Values);
                ResetVars(_varRegistry.Objects.Values);
                ResetVars(_varRegistry.PresenceBools.Values.SelectMany(l => l));
                ResetVars(_varRegistry.PresenceFloats.Values.SelectMany(l => l));
                ResetVars(_varRegistry.PresenceInts.Values.SelectMany(l => l));
                ResetVars(_varRegistry.PresenceStrings.Values.SelectMany(l => l));
                ResetVars(_varRegistry.PresenceObjects.Values.SelectMany(l => l));
                ResetVars(_varRegistry.SelfBools.Values);
                ResetVars(_varRegistry.SelfFloats.Values);
                ResetVars(_varRegistry.SelfInts.Values);
                ResetVars(_varRegistry.SelfStrings.Values);
                ResetVars(_varRegistry.SelfObjects.Values);
            }
        }

        private void ResetVars<T>(IEnumerable<IVar<T>> vars)
        {
            foreach (var var in vars)
            {
                var.Reset();
            }
        }
    }
}