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
                ResetVars(_varRegistry.SharedVarRegistry.SharedBools);
                ResetVars(_varRegistry.SharedVarRegistry.SharedFloats);
                ResetVars(_varRegistry.SharedVarRegistry.SharedInts);
                ResetVars(_varRegistry.SharedVarRegistry.SharedStrings);
                ResetVars(_varRegistry.PresenceVarRegistry.PresenceBools);
                ResetVars(_varRegistry.PresenceVarRegistry.PresenceFloats);
                ResetVars(_varRegistry.PresenceVarRegistry.PresenceInts);
                ResetVars(_varRegistry.PresenceVarRegistry.PresenceStrings);
            }
        }

        private void ResetVars<T>(Dictionary<string, SharedVar<T>> vars)
        {
            foreach (var var in vars.Values)
            {
                var.Reset();
            }
        }

        private void ResetVars<T>(Dictionary<string, PresenceVarCollection<T>> presenceVarCollections)
        {
            foreach (var presenceVarCollection in presenceVarCollections.Values)
            {
                presenceVarCollection.SelfVar.Reset();

                foreach (var presenceVar in presenceVarCollection.PresenceVars)
                {
                    presenceVar.Reset();
                }
            }
        }
    }
}