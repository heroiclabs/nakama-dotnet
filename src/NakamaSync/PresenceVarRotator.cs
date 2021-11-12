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
    internal class PresenceVarRotator
    {
        public ILogger Logger { get; set; }

        public Dictionary<string, IPresenceRotatable> AssignedPresenceVars => _assignedRotatables;
        public IList<IPresenceRotatable> UnassignedPresenceVars => _unassignedRotatables;

        private readonly List<IPresenceRotatable> _unassignedRotatables = new List<IPresenceRotatable>();
        private readonly Dictionary<string, IPresenceRotatable> _assignedRotatables = new Dictionary<string, IPresenceRotatable>();


        public void Subscribe(PresenceTracker presenceTracker)
        {
            presenceTracker.OnPresenceAdded += HandlePresenceAdded;
            presenceTracker.OnPresenceRemoved += HandlePresenceRemoved;
        }

        public void AddPresenceVar(IPresenceRotatable PresenceVar)
        {
            if (PresenceVar.Presence == null)
            {
                Logger?.DebugFormat($"Rotator is adding unassigned presence var: {PresenceVar}");
                _unassignedRotatables.Add(PresenceVar);
            }
            else
            {
                Logger?.DebugFormat($"Rotator is adding assigned presence var: {PresenceVar}");
                _assignedRotatables.Add(PresenceVar.Presence.UserId, PresenceVar);
            }
        }

        public void HandlePresencesAdded(IEnumerable<IUserPresence> presences)
        {
            foreach (IUserPresence presence in presences)
            {
                HandlePresenceAdded(presence);
            }
        }

        private void HandlePresenceAdded(IUserPresence presence)
        {

            Logger?.DebugFormat($"Presence var rotator notified of added presence: {presence.UserId}");

            if (_unassignedRotatables.Count == 0)
            {
                throw new InvalidOperationException($"Not enough presence vars to handle presence.");
            }
            // normal for server to send duplicate presence additions in very specific situations.
            else if (!_assignedRotatables.ContainsKey(presence.UserId))
            {
                Logger?.DebugFormat($"Presence var rotator is moving var from unassigned to assigned for presence: {presence.UserId} ");
                var varToAssign = _unassignedRotatables.First();
                varToAssign.Presence = presence;
                _unassignedRotatables.Remove(varToAssign);
                _assignedRotatables.Add(presence.UserId, varToAssign);
            }
        }

        private void HandlePresenceRemoved(IUserPresence presence)
        {
            if (!_assignedRotatables.ContainsKey(presence.UserId))
            {
                // user presence left that hadn't been assigned to any vars.
                // this is perfectly valid.
                return;
            }

            IPresenceRotatable rotatable = _assignedRotatables[presence.UserId];
            rotatable.Presence = null;
            _assignedRotatables.Remove(presence.UserId);
            _unassignedRotatables.Add(rotatable);
        }
    }
}
