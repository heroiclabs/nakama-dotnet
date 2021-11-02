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
    // like, do they all need to be filled with a presence or can some be unassigned.
    // todo assign errorhandler and logger to this
    internal class OtherVarRotator<T> : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        public Dictionary<string, OtherVar<T>> AssignedOtherVars => _assignedOtherVars;
        public IList<OtherVar<T>> UnassignedOtherVars => _unassignedOtherVars;

        private readonly List<OtherVar<T>> _unassignedOtherVars = new List<OtherVar<T>>();
        private readonly Dictionary<string, OtherVar<T>> _assignedOtherVars = new Dictionary<string, OtherVar<T>>();
        private readonly PresenceTracker _presenceTracker;
        private IUserPresence _self;

        public OtherVarRotator(PresenceTracker presenceTracker)
        {
            _presenceTracker = presenceTracker;
        }

        public void AddOtherVar(OtherVar<T> OtherVar)
        {
            if (OtherVar.Presence == null)
            {
                Logger?.DebugFormat($"Rotator is adding unassigned presence var: {OtherVar}");
                _unassignedOtherVars.Add(OtherVar);
            }
            else
            {
                Logger?.DebugFormat($"Rotator is adding assigned presence var: {OtherVar}");
                _assignedOtherVars.Add(OtherVar.Presence.UserId, OtherVar);
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

            if (_unassignedOtherVars.Count == 0)
            {
                ErrorHandler?.Invoke(new InvalidOperationException($"Not enough presence vars to handle presence."));
            }
            // normal for server to send duplicate presence additions in very specific situations.
            else if (!_assignedOtherVars.ContainsKey(presence.UserId))
            {
                Logger?.DebugFormat($"Presence var rotator for {_self.UserId} is moving var from unassigned to assigned for presence: {presence.UserId} ");
                var varToAssign = _unassignedOtherVars.First();
                varToAssign.SetPresence(presence);
                _unassignedOtherVars.Remove(varToAssign);
                _assignedOtherVars.Add(presence.UserId, varToAssign);
            }
        }

        private void HandlePresenceRemoved(IUserPresence presence)
        {
            if (!_assignedOtherVars.ContainsKey(presence.UserId))
            {
                // user presence left that hadn't been assigned to any vars.
                // this is perfectly valid.
                return;
            }

            OtherVar<T> varToRemove = _assignedOtherVars[presence.UserId];
            varToRemove.SetPresence(null);
            _assignedOtherVars.Remove(presence.UserId);
            _unassignedOtherVars.Add(varToRemove);
        }
    }
}
