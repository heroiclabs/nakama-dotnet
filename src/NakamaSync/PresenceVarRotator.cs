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
    internal class PresenceVarRotator<T>
    {
        public ILogger Logger { get; set; }

        public Dictionary<string, PresenceVar<T>> AssignedPresenceVars => _assignedPresenceVars;
        public IList<PresenceVar<T>> UnassignedPresenceVars => _unassignedPresenceVars;

        private readonly List<PresenceVar<T>> _unassignedPresenceVars = new List<PresenceVar<T>>();
        private readonly Dictionary<string, PresenceVar<T>> _assignedPresenceVars = new Dictionary<string, PresenceVar<T>>();
        private readonly ISession _session;

        public PresenceVarRotator(ISession session)
        {
            _session = session;
        }

        public void Subscribe(PresenceTracker presenceTracker)
        {
            presenceTracker.OnPresenceAdded += HandlePresenceAdded;
            presenceTracker.OnPresenceRemoved += HandlePresenceRemoved;
        }

        public void AddPresenceVar(PresenceVar<T> PresenceVar)
        {
            if (PresenceVar.Presence == null)
            {
                Logger?.DebugFormat($"Rotator is adding unassigned presence var: {PresenceVar}");
                _unassignedPresenceVars.Add(PresenceVar);
            }
            else
            {
                Logger?.DebugFormat($"Rotator is adding assigned presence var: {PresenceVar}");
                _assignedPresenceVars.Add(PresenceVar.Presence.UserId, PresenceVar);
            }
        }

        public void ReceiveMatch(IMatch match)
        {
            foreach (IUserPresence joiner in match.Presences)
            {
                HandlePresenceAdded(joiner);
            }
        }

        private void HandlePresenceAdded(IUserPresence presence)
        {
            if (presence.UserId == _session.UserId)
            {
                return;
            }

            Logger?.DebugFormat($"Presence var rotator notified of added presence: {presence.UserId}");

            if (_unassignedPresenceVars.Count == 0)
            {
                throw new InvalidOperationException($"Not enough presence vars to handle presence.");
            }
            // normal for server to send duplicate presence additions in very specific situations.
            else if (!_assignedPresenceVars.ContainsKey(presence.UserId))
            {
                Logger?.DebugFormat($"Presence var rotator is moving var from unassigned to assigned for presence: {presence.UserId} ");
                var varToAssign = _unassignedPresenceVars.First();
                varToAssign.SetPresence(presence);
                _unassignedPresenceVars.Remove(varToAssign);
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
