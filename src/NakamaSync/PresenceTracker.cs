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
using System.Linq;
using System.Collections.Generic;
using Nakama;

namespace NakamaSync
{
    internal class PresenceTracker : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }


        public event Action<IUserPresence> OnPresenceAdded;
        public event Action<IUserPresence> OnPresenceRemoved;

        public string UserId => _userId;

        private readonly SortedList<string, IUserPresence> _presences = new SortedList<string, IUserPresence>();
        private readonly string _userId;

        public PresenceTracker(string userId)
        {
            _userId = userId;
        }

        public void ReceiveMatch(IMatch match)
        {
            Logger?.DebugFormat($"Presence tracker for {_userId} received match.");
            HandlePresences(match.Presences, new IUserPresence[]{});
        }

        public int GetPresenceCount()
        {
            return _presences.Count;
        }

        public List<string> GetSortedUserIds()
        {
            return new List<string>(_presences.Keys);
        }

        public IUserPresence GetPresence(string userId)
        {
            if (!_presences.ContainsKey(userId))
            {
                ErrorHandler?.Invoke(new KeyNotFoundException($"User{_userId} could not find presence with id {userId}."));
            }

            return _presences[userId];
        }

        public bool HasPresence(string userId)
        {
            return _presences.ContainsKey(userId);
        }

        public IUserPresence GetPresence(int index)
        {
            if (GetPresenceCount() > index)
            {
                return _presences.Values[index];
            }

            return null;
        }

        public List<IUserPresence> GetPresences(IEnumerable<string> userIds)
        {
            var presences = new List<IUserPresence>();

            foreach (string userId in userIds)
            {
                presences.Add(GetPresence(userId));
            }

            return presences;
        }

        public IEnumerable<IUserPresence> GetOthers()
        {
            var otherIds = GetSortedUserIds().Except(new string[]{_userId});
            return GetPresences(otherIds);
        }

        public IUserPresence GetSelf()
        {
            return GetPresence(_userId);
        }

        public void Subscribe(ISocket socket)
        {
            socket.ReceivedMatchPresence += HandlePresenceEvent;
        }

        private void HandlePresenceEvent(IMatchPresenceEvent presenceEvent)
        {
            HandlePresences(presenceEvent.Joins, presenceEvent.Leaves);
        }

        private void HandlePresences(IEnumerable<IUserPresence> joiners, IEnumerable<IUserPresence> leavers)
        {
            var oldPresences = new SortedList<string, IUserPresence>(_presences, StringComparer.Create(System.Globalization.CultureInfo.InvariantCulture, ignoreCase: false));

            var removedPresences = new List<IUserPresence>();

            foreach (IUserPresence leaver in leavers)
            {
                if (_presences.ContainsKey(leaver.UserId))
                {
                    Logger?.DebugFormat($"User {_userId} is removing leaving presence {leaver}");
                    removedPresences.Add(leaver);
                    _presences.Remove(leaver.UserId);
                    Logger?.DebugFormat($"Num presences after removal: {_presences.Count}");
                }
                else
                {
                    ErrorHandler?.Invoke(new InvalidOperationException($"For user {_userId} leaving presence does not exist: " + leaver.UserId));
                }
            }

            foreach (var removedPresence in removedPresences)
            {
                OnPresenceRemoved?.Invoke(removedPresence);
            }

            var addedPresences = new List<IUserPresence>();

            foreach (IUserPresence joiner in joiners)
            {
                if (_presences.ContainsKey(joiner.UserId))
                {
                    // sometimes duplicate presences can be returned from the server depending the timing
                    // of when players simultaneously join.
                    continue;
                }
                else
                {
                    Logger?.DebugFormat($"User {_userId} is adding joining presence {joiner.UserId}");
                    addedPresences.Add(joiner);
                    _presences.Add(joiner.UserId, joiner);
                    Logger?.DebugFormat($"Num presences after add: {_presences.Count}");
                }
            }

            foreach (IUserPresence addedPresence in addedPresences)
            {
                OnPresenceAdded?.Invoke(addedPresence);
            }
        }
    }
}
