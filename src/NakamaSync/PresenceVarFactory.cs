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
using System.Threading.Tasks;
using Nakama;

namespace NakamaSync
{
    // todo assign errorhandler and logger to this
    // todo reuse same variable if user rejoins?
    internal class PresenceVarFactory<T>
    {
        public ILogger Logger { get; set; }

        private readonly Dictionary<long, GroupVar<T>> _varsByOpcode = new Dictionary<long, GroupVar<T>>();
        private readonly Dictionary<string, List<PresenceVar<T>>> _varsByUser = new Dictionary<string, List<PresenceVar<T>>>();

        private string _userId;

        // todo flush this value where appropriate
        private SyncMatch _syncMatch;

        private readonly int _handshakeTimeoutSec;

        public PresenceVarFactory(int handhakeTimeoutSec)
        {
            _handshakeTimeoutSec = handhakeTimeoutSec;
        }

        public Task AddGroupVar(long opcode, GroupVar<T> groupVar)
        {
            if (_varsByOpcode.ContainsKey(opcode))
            {
                throw new ArgumentException($"Already added opcode: {opcode}");
            }

            var taskList = new List<Task>();

            _varsByOpcode.Add(opcode, groupVar);

            // deferred registration
            if (_syncMatch != null)
            {
                // use the presence tracker presences rather than the sync match presence because they will be
                // up to date. the sync match may be stale at the point based on how the registry is written.
                foreach (IUserPresence presence in _syncMatch.PresenceTracker.GetSortedOthers())
                {
                    if (!_varsByUser.ContainsKey(presence.UserId))
                    {
                        _varsByUser[presence.UserId] = new List<PresenceVar<T>>();
                    }

                    var newVar = new PresenceVar<T>(opcode);
                    newVar.SetPresence(presence);

                    // newly created presence vars need to handshake, etc.
                    taskList.Add(newVar.ReceiveSyncMatch(_syncMatch, _handshakeTimeoutSec));

                    _varsByUser[presence.UserId].Add(newVar);
                    _varsByOpcode[opcode].OthersList.Add(newVar);
                    _varsByOpcode[opcode].OnPresenceAddedInternal?.Invoke(newVar);
                }
            }

            return Task.WhenAll(taskList);
        }

        public void HandleSerialized(IUserPresence source, long opcode, SerializableVar<T> serializable)
        {
            if (!_varsByOpcode.ContainsKey(opcode))
            {
                // todo I think either unnecessary or log an error
                return;
            }

            foreach (PresenceVar<T> var in _varsByOpcode[opcode].Others)
            {
                if (var.Presence.UserId == source.UserId)
                {
                    var.ReceiveSerialized(source, serializable);
                }
            }
        }

        public Task ReceiveSyncMatch(SyncMatch syncMatch)
        {
            _userId = syncMatch.Self.UserId;
            _syncMatch = syncMatch;

            // todo what if user leaves before handshake is done

            var handshakeTasks = new List<Task>();

            // use the presence tracker presences rather than the sync match presence because they will be
            // up to date. the sync match may be stale at the point based on how the registry is written.
            foreach (IUserPresence presence in syncMatch.PresenceTracker.GetSortedOthers())
            {
                handshakeTasks.AddRange(HandlePresenceAdded(presence));
            }

            return Task.WhenAll(handshakeTasks);
        }

        internal void ReceivePresenceEvent(IMatchPresenceEvent evt)
        {
            foreach (IUserPresence presence in evt.Joins)
            {
                HandlePresenceAdded(presence);
            }

            foreach (IUserPresence presence in evt.Leaves)
            {
                HandlePresenceRemoved(presence);
            }
        }

        private List<Task> HandlePresenceAdded(IUserPresence presence)
        {
            var newVarHandshakes = new List<Task>();

            if (_varsByUser.ContainsKey(presence.UserId) || presence.UserId == _userId)
            {
                // note: normal for server to send duplicate presence additions in very specific situations.
                return newVarHandshakes;
            }

            var userVars = new List<PresenceVar<T>>();
            _varsByUser.Add(presence.UserId, userVars);

            foreach (KeyValuePair<long, GroupVar<T>> var in _varsByOpcode)
            {
                var newVar = new PresenceVar<T>(var.Key);
                newVar.SetPresence(presence);
                newVarHandshakes.Add(newVar.ReceiveSyncMatch(_syncMatch, _handshakeTimeoutSec));
                userVars.Add(newVar);

                var.Value.OthersList.Add(newVar);
                var.Value.OnPresenceAddedInternal?.Invoke(newVar);
            }

            return newVarHandshakes;
        }

        private void HandlePresenceRemoved(IUserPresence presence)
        {
            if (presence.UserId != _syncMatch.Self.UserId)
            {
                return;
            }

            if (!_varsByUser.ContainsKey(presence.UserId))
            {
                // todo log error
                return;
            }

            List<PresenceVar<T>> userVars = _varsByUser[presence.UserId];

            foreach (PresenceVar<T> presenceVar in userVars)
            {
                presenceVar.Reset();
                _varsByUser.Remove(presence.UserId);
            }
        }
    }
}
