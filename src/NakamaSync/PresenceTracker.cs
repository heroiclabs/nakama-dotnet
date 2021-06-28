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

        public IUserPresence GetSelf()
        {
            lock (_presenceLock)
            {
                return GetPresence(_userId);
            }
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
            HandlePresences(presenceEvent.Joins, presenceEvent.Leaves);
        }

        private void HandlePresences(IEnumerable<IUserPresence> joiners, IEnumerable<IUserPresence> leavers)
        {
            lock (_presenceLock)
            {
                var oldPresences = _presences;
                var newPresences = GetNewPresences(_presences, joiners, leavers);
                _presences = newPresences;

                var oldHost = GetHost();
                var newHost = GetHost(newPresences);

                HandleJoiners(joiners, newHost);
                HandleLeavers(leavers, oldHost);

                System.Console.WriteLine("user is " + _userId + "...");

                System.Console.WriteLine("new host is " + _userId);


                System.Console.WriteLine("dispatching on host changed");

                if (oldHost != newHost)
                {
                    OnHostChanged?.Invoke(new HostChangedEvent(oldHost, newHost));
                }
            }
        }

        private void HandleJoiners(IEnumerable<IUserPresence> joiners, IUserPresence newHost)
        {
            foreach (var joiner in joiners)
            {
                System.Console.WriteLine("joiner is " + joiner.UserId + " , " + newHost.UserId);

                if (joiner.UserId != newHost?.UserId)
                {
                    System.Console.WriteLine("dispatching on guest joined");
                    OnGuestJoined?.Invoke(joiner);
                }
            }
        }

        private void HandleLeavers(IEnumerable<IUserPresence> leavers, IUserPresence oldhost)
        {
            foreach (var leaver in leavers)
            {
                if (leaver.UserId != oldhost?.UserId)
                {
                    OnGuestLeft?.Invoke(leaver);
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
            SortedList<string, IUserPresence> oldPresences, IEnumerable<IUserPresence> joiners, IEnumerable<IUserPresence> leavers)
        {
            lock (_presenceLock)
            {
                var newPresences = new SortedList<string, IUserPresence>(oldPresences);

                foreach (IUserPresence leaver in leavers)
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

                foreach (IUserPresence joiner in joiners)
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
