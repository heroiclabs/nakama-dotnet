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
    // todo assign errorhandler and logger to this
    // todo reuse same variable if user rejoins?
    internal class PresenceVarRotator<T>
    {
        public ILogger Logger { get; set; }

        private readonly Dictionary<long, List<PresenceVar<T>>> _varsByOpcode = new Dictionary<long, List<PresenceVar<T>>>();
        private readonly Dictionary<string, List<PresenceVar<T>>> _varsByUser = new Dictionary<string, List<PresenceVar<T>>>();

        private string _userId;

        // todo flush this value where appropriate
        private SyncMatch _syncMatch;

        public void ReceiveSyncMatch(SyncMatch syncMatch)
        {
            _userId = syncMatch.Self.UserId;
            _syncMatch = syncMatch;
            syncMatch.PresenceTracker.OnPresenceAdded += HandlePresenceAdded;
            syncMatch.PresenceTracker.OnPresenceRemoved += HandlePresenceRemoved;

            foreach (IUserPresence presence in syncMatch.Presences)
            {
                HandlePresenceAdded(presence);
            }
        }

        public void HandleSerialized(IUserPresence source, long opcode, SerializableVar<T> serializable)
        {
            if (!_varsByOpcode.ContainsKey(opcode))
            {
                // todo I think either unnecessary or log an error
                return;
            }

            foreach (PresenceVar<T> var in _varsByOpcode[opcode])
            {
                if (var.Presence.UserId == source.UserId)
                {
                    System.Console.WriteLine("CALLING HANDLE SERIALIZED");
                    var.HandleSerialized(source, serializable);
                }
            }
        }

        public void AddOpcode(long opcode, List<PresenceVar<T>> presenceVars)
        {
            if (_varsByOpcode.ContainsKey(opcode))
            {
                throw new ArgumentException($"Already added opcode: {opcode}");
            }

            _varsByOpcode.Add(opcode, presenceVars);
        }

        private void HandlePresenceAdded(IUserPresence presence)
        {
            if (_varsByUser.ContainsKey(presence.UserId) || presence.UserId == _userId)
            {
                // note: normal for server to send duplicate presence additions in very specific situations.
                return;
            }

            var userVars =  new List<PresenceVar<T>>();
            _varsByUser.Add(presence.UserId, userVars);

            foreach (KeyValuePair<long, List<PresenceVar<T>>> vars in _varsByOpcode)
            {
                var newVar = new PresenceVar<T>(vars.Key);
                newVar.SetPresence(presence);
                newVar.ReceiveSyncMatch(_syncMatch);

                System.Console.WriteLine("adding new var to list " + presence.UserId + ", " + _userId);

                vars.Value.Add(newVar);
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

            List<PresenceVar<T>> userVars = _varsByUser[_userId];

            foreach (PresenceVar<T> presenceVar in userVars)
            {
                presenceVar.Reset();
                _varsByUser.Remove(presence.UserId);
            }
        }
    }
}
