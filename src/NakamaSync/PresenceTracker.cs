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
    internal class PresenceTracker : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        public event Action<IUserPresence> OnPresenceAdded;
        public event Action<IUserPresence> OnPresenceRemoved;

        private readonly SortedList<string, IUserPresence> _presences = new SortedList<string, IUserPresence>();
        private readonly string _userId;

        public PresenceTracker(string userId)
        {
            _userId = userId;
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
            if (!_presences.ContainsKey(_userId))
            {
                ErrorHandler?.Invoke(new KeyNotFoundException("Could not find presence with id: " + userId));
            }

            return _presences[userId];
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
                    removedPresences.Add(leaver);
                    _presences.Remove(leaver.UserId);
                }
                else
                {
                    ErrorHandler?.Invoke(new InvalidOperationException("Leaving presence does not exist: " + leaver.UserId));
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
                    throw new InvalidOperationException("Joining presence already exists: " + joiner.UserId);
                }
                else
                {
                    addedPresences.Add(joiner);
                    _presences.Add(joiner.UserId, joiner);
                }
            }

            foreach (IUserPresence addedPresence in addedPresences)
            {
                OnPresenceAdded?.Invoke(addedPresence);
            }
        }
    }
}
