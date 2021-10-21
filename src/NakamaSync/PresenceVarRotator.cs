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
using System.Linq;
using Nakama;

namespace NakamaSync
{
    // todo add some enum or option on how you would like the presence vars to be assigned.
    // like, do they all need to be filled or what.
    // todo assign errorhandler and logger to this
    internal class PresenceVarRotator<T> : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        public Dictionary<string, PresenceVar<T>> AssignedPresenceVars => _assignedPresenceVars;
        public IList<PresenceVar<T>> UnassignedPresenceVars => _unassignedPresenceVars;

        private readonly List<PresenceVar<T>> _unassignedPresenceVars = new List<PresenceVar<T>>();
        private readonly Dictionary<string, PresenceVar<T>> _assignedPresenceVars = new Dictionary<string, PresenceVar<T>>();
        private readonly PresenceTracker _presenceTracker;
        private IUserPresence _self;

        public PresenceVarRotator(PresenceTracker presenceTracker)
        {
            _presenceTracker = presenceTracker;
        }

        public void AddPresenceVar(PresenceVar<T> presenceVar)
        {
            if (presenceVar.Presence == null)
            {
                Logger?.DebugFormat($"Rotator is adding unassigned presence var: {presenceVar}");
                _unassignedPresenceVars.Add(presenceVar);
            }
            else if (_assignedPresenceVars.ContainsKey(presenceVar.Presence.UserId))
            {
                Logger?.DebugFormat($"Rotator is adding assigned presence var: {presenceVar}");
                // todo not really sure what situation this could occur in but I suppose it's okay to support it.
                _assignedPresenceVars.Add(presenceVar.Presence.UserId, presenceVar);
            }
        }

        public void ReceiveMatch(IMatch match)
        {
            _self = match.Self;

            foreach (IUserPresence joiner in match.Presences)
            {
                HandlePresenceAdded(joiner);
            }

            _presenceTracker.OnPresenceAdded += HandlePresenceAdded;
            _presenceTracker.OnPresenceRemoved += HandlePresenceRemoved;
        }

        private void HandlePresenceAdded(IUserPresence presence)
        {
            Logger?.DebugFormat($"Presence var rotator for {_presenceTracker.UserId} notified of added presence: {presence.UserId}");

            if (_unassignedPresenceVars.Count == 0)
            {
                ErrorHandler?.Invoke(new InvalidOperationException($"Not enough presence vars to handle presence."));
            }
            // normal for server to send duplicate presence additions in very specific situations.
            else if (!_assignedPresenceVars.ContainsKey(presence.UserId))
            {
                Logger?.DebugFormat($"Presence var rotator for assigning presence: {presence.UserId}");
                var varToAssign = _unassignedPresenceVars.First();
                varToAssign.SetPresence(presence);
                _assignedPresenceVars.Add(presence.UserId, varToAssign);
            }
        }

        private void HandlePresenceRemoved(IUserPresence presence)
        {
            if (!_assignedPresenceVars.ContainsKey(presence.UserId))
            {
                // user presence left that hadn't been assigned to any vars.
                // this is perfectly valid.
                return;
            }

            PresenceVar<T> varToRemove = _assignedPresenceVars[presence.UserId];
            varToRemove.SetPresence(null);
            _assignedPresenceVars.Remove(presence.UserId);
            _unassignedPresenceVars.Add(varToRemove);
        }
    }
}
