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
        private readonly object _presenceLock = new object();

        public PresenceTracker(string userId)
        {
            _userId = userId;
        }

        public IUserPresence GetHost()
        {
            lock (_presenceLock)
            {
                return _presences.FirstOrDefault().Value;
            }
        }

        public IUserPresence GetSelf()
        {
            lock (_presenceLock)
            {
                return GetPresence(_userId);
            }
        }

        public IUserPresence GetPresence(string userId)
        {
            lock (_presenceLock)
            {
                if (!_presences.ContainsKey(_userId))
                {
                    throw new KeyNotFoundException("Could not find presence with id: " + userId);
                }

                return _presences[userId];
            }
        }

        public void HandlePresenceEvent(IMatchPresenceEvent presenceEvent)
        {
            lock (_presenceLock)
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
        }

        public bool IsSelfHost()
        {
            lock (_presenceLock)
            {
                return _userId == GetHost()?.UserId;
            }
        }

        private IUserPresence GetHost(SortedList<string, IUserPresence> presences)
        {
            lock (_presenceLock)
            {
                return _presences.FirstOrDefault().Value;
            }
        }

        private SortedList<string, IUserPresence> GetNewPresences(
            SortedList<string, IUserPresence> oldPresences, IMatchPresenceEvent presenceEvent)
        {
            lock (_presenceLock)
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
}
