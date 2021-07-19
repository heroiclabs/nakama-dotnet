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

        private readonly Queue<PresenceVar<T>> _unassignedPresenceVars = new Queue<PresenceVar<T>>();
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
                _unassignedPresenceVars.Enqueue(presenceVar);
            }
            else
            {
                _assignedPresenceVars.Add(presenceVar.Presence.UserId, presenceVar);
            }
        }

        public void ReceiveMatch(IMatch match)
        {
            _self = match.Self;
            _presenceTracker.OnPresenceAdded -= HandlePresenceAdded;
            _presenceTracker.OnPresenceRemoved -= HandlePresenceRemoved;
        }

        private void HandlePresenceAdded(IUserPresence presence)
        {
            if (_unassignedPresenceVars.Count == 0)
            {
                ErrorHandler?.Invoke(new InvalidOperationException($"Not enough presence vars to handle presence."));
            }
            else if (_assignedPresenceVars.ContainsKey(presence.UserId))
            {
                ErrorHandler?.Invoke(new InvalidOperationException($"User {presence.UserId} already has var assigned."));
            }
            else
            {
                _assignedPresenceVars.Add(presence.UserId, _unassignedPresenceVars.Dequeue());
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
            _unassignedPresenceVars.Enqueue(varToRemove);
        }
    }
}
