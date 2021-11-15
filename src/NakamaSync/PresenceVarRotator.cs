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
    // todo reuse same variable if user rejoins?
    internal class PresenceVarRotator<T>
    {
        public ILogger Logger { get; set; }

        private readonly Dictionary<long, List<PresenceVar<T>>> _varsByOpcode = new Dictionary<long, List<PresenceVar<T>>>();
        private readonly Dictionary<string, List<PresenceVar<T>>> _varsByUser = new Dictionary<string, List<PresenceVar<T>>>();

        private string _userId;

        public void ReceiveSyncMatch(SyncMatch syncMatch)
        {
            _userId = syncMatch.Self.UserId;
            syncMatch.PresenceTracker.OnPresenceAdded += HandlePresenceAdded;
            syncMatch.PresenceTracker.OnPresenceRemoved += HandlePresenceRemoved;

            foreach (IUserPresence presence in syncMatch.Presences)
            {
                HandlePresenceAdded(presence);
            }
        }

        public void AddOpcode(long opcode)
        {
            if (_varsByOpcode.ContainsKey(opcode))
            {
                throw new ArgumentException($"Not enough presence vars to handle presence.");
            }

            _varsByOpcode.Add(opcode, new List<PresenceVar<T>>());
        }

        private void HandlePresenceAdded(IUserPresence presence)
        {
            if (_varsByUser.ContainsKey(presence.UserId))
            {
                // normal for server to send duplicate presence additions in very specific situations.
                return;
            }

            var userVars =  new List<PresenceVar<T>>();
            _varsByUser.Add(presence.UserId, userVars);

            foreach (KeyValuePair<long, List<PresenceVar<T>>> rotatables in _varsByOpcode)
            {
                var newVar = new PresenceVar<T>(rotatables.Key);
                newVar.SetPresence(presence);
                rotatables.Value.Add(newVar);
                userVars.Add(newVar);
            }
        }

        private void HandlePresenceRemoved(IUserPresence presence)
        {
            if (!_varsByUser.ContainsKey(presence.UserId))
            {
                // todo log error
                return;
            }

            _varsByUser.Remove(presence.UserId);
        }
    }
}
