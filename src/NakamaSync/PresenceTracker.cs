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

        private readonly SortedList<string, IUserPresence> _presences = new SortedList<string, IUserPresence>();
        private readonly string _userId;
        private readonly object _presenceLock = new object();

        public PresenceTracker(string userId)
        {
            _userId = userId;
        }

        public IUserPresence GetHost()
        {
            return _presences.FirstOrDefault().Value;
        }

        public IUserPresence GetPresence(string userId)
        {
            if (!_presences.ContainsKey(_userId))
            {
                throw new KeyNotFoundException("Could not find presence with id: " + userId);
            }

            return _presences[userId];
        }

        public IUserPresence GetSelf()
        {
            return GetPresence(_userId);
        }

        public void HandleMatch(IMatch match)
        {
            lock (_presenceLock)
            {
                HandlePresences(match.Presences, new IUserPresence[]{});
            }
        }

        public void HandlePresenceEvent(IMatchPresenceEvent presenceEvent)
        {
            lock (_presenceLock)
            {
                HandlePresences(presenceEvent.Joins, presenceEvent.Leaves);
            }
        }

        private void HandlePresences(IEnumerable<IUserPresence> joiners, IEnumerable<IUserPresence> leavers)
        {
            var oldPresences = new SortedList<string, IUserPresence>(_presences);
            ApplyNewPresences(_presences, joiners, leavers);

            var oldHost = GetHost(oldPresences.Values);
            var newHost = GetHost(_presences.Values);

            foreach (var joiner in joiners)
            {
                if (joiner.UserId != newHost?.UserId)
                {
                    OnGuestJoined?.Invoke(joiner);
                }
            }

            foreach (var leaver in leavers)
            {
                if (leaver.UserId != oldHost?.UserId)
                {
                    OnGuestLeft?.Invoke(leaver);
                }
            }

            if (oldHost != newHost)
            {
                OnHostChanged?.Invoke(new HostChangedEvent(oldHost, newHost));
            }
        }

        public bool IsSelfHost()
        {
            lock (_presenceLock)
            {
                return _userId == GetHost()?.UserId;
            }
        }

        private IUserPresence GetHost(IEnumerable<IUserPresence> presences)
        {
            lock (_presenceLock)
            {
                return presences.FirstOrDefault();
            }
        }

        private void ApplyNewPresences(
            SortedList<string, IUserPresence> presences, IEnumerable<IUserPresence> joiners, IEnumerable<IUserPresence> leavers)
        {
            lock (_presenceLock)
            {
                foreach (IUserPresence leaver in leavers)
                {
                    if (presences.ContainsKey(leaver.UserId))
                    {
                        presences.Remove(leaver.UserId);
                    }
                    else
                    {
                        throw new InvalidOperationException("Leaving presence does not exist.");
                    }
                }

                foreach (IUserPresence joiner in joiners)
                {
                    if (presences.ContainsKey(joiner.UserId))
                    {
                        throw new InvalidOperationException("Joining presence already exists.");
                    }
                    else
                    {
                        presences.Add(joiner.UserId, joiner);
                    }
                }
            }
        }
    }
}
