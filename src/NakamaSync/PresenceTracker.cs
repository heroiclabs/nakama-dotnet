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
    // TODO catch presence tracker exceptions.
    internal class PresenceTracker
    {
        public event Action<HostChangedEvent> OnHostChanged;

        public event Action<IUserPresence> OnGuestLeft;
        public event Action<IUserPresence> OnGuestJoined;

        private SortedList<string, IUserPresence> _presences = new SortedList<string, IUserPresence>();
        private readonly string _userId;

        public PresenceTracker(string userId)
        {
            _userId = userId;
        }

        public bool IsSelfHost()
        {
            return _userId == GetHost()?.UserId;
        }

        public IUserPresence GetHost()
        {
            return _presences.FirstOrDefault().Value;
        }

        public IUserPresence GetSelf()
        {
            return GetPresence(_userId);
        }

        public IUserPresence GetPresence(string userId)
        {
            if (!_presences.ContainsKey(_userId))
            {
                throw new KeyNotFoundException("Could not find presnce with id: " + userId);
            }

            return _presences[userId];
        }

        public void HandlePresenceEvent(IMatchPresenceEvent presenceEvent)
        {
            var oldPresences = _presences;
            var newPresences = GetNewPresences(_presences, presenceEvent);
            _presences = newPresences;

            var oldHost = GetHost();
            var newHost = GetHost(newPresences);

            foreach (var leaver in presenceEvent.Leaves)
            {
                if (leaver.UserId != oldHost?.UserId)
                {
                    OnGuestLeft?.Invoke(leaver);
                }
            }

            foreach (var joiner in presenceEvent.Joins)
            {
                if (joiner.UserId != newHost?.UserId)
                {
                    OnGuestJoined?.Invoke(joiner);
                }
            }

            if (oldHost != newHost)
            {
                OnHostChanged?.Invoke(new HostChangedEvent(oldHost, newHost));
            }
        }

        private IUserPresence GetHost(SortedList<string, IUserPresence> presences)
        {
            return _presences.FirstOrDefault().Value;
        }

        private SortedList<string, IUserPresence> GetNewPresences(
            SortedList<string, IUserPresence> oldPresences, IMatchPresenceEvent presenceEvent)
        {
            var newPresences = new SortedList<string, IUserPresence>(oldPresences);

            foreach (IUserPresence leaver in presenceEvent.Leaves)
            {
                if (newPresences.ContainsKey(leaver.UserId))
                {
                    newPresences.Remove(leaver.UserId);
                }
                else
                {
                    throw new InvalidOperationException("Leaving presence does not exist.");
                }
            }

            foreach (IUserPresence joiner in presenceEvent.Joins)
            {
                if (newPresences.ContainsKey(joiner.UserId))
                {
                    throw new InvalidOperationException("Joining presence already exists.");
                }
                else
                {
                    newPresences.Add(joiner.UserId, joiner);
                }
            }

            return newPresences;
        }
    }
}
