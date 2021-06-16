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

namespace Nakama.Replicated
{
    // TODO catch presence tracker exceptions.
    internal class PresenceTracker
    {
        internal enum HostHeuristic
        {
            None,
            NewestMember
        }

        public IUserPresence Self => _self;

        public delegate void HostChangedHandler(IUserPresence oldHost, IUserPresence newHost);

        public event HostChangedHandler OnHostChanged;
        public event Action<IUserPresence> OnGuestLeft;
        public event Action<IUserPresence> OnGuestJoined;

        private IUserPresence _host;
        private readonly HostHeuristic _hostHeuristic;
        private readonly List<string> _joinOrder = new List<string>();
        private readonly Dictionary<string, IUserPresence> _presences = new Dictionary<string, IUserPresence>();
        private readonly IUserPresence _self;
        private readonly bool _trackHost;

        public PresenceTracker(IUserPresence self, bool trackHost, HostHeuristic hostHeuristic)
        {
            _self = self;
            _trackHost = trackHost;
            _hostHeuristic = hostHeuristic;
        }

        public IEnumerable<IUserPresence> GetGuests()
        {
            return _presences.Where(presence => presence.Value.UserId != _host.UserId).Select(presence => presence.Value);
        }

        public IUserPresence GetHost()
        {
            return _host;
        }

        public IEnumerable<IUserPresence> GetPresences()
        {
            return _presences.Values;
        }

        public void HandlePresenceEvent(IMatchPresenceEvent presenceEvent)
        {
            foreach (IUserPresence joiner in presenceEvent.Joins)
            {
                if (_presences.ContainsKey(joiner.UserId))
                {
                    throw new InvalidOperationException("Joining presence already exists.");
                }
                else
                {
                    _presences[joiner.UserId] = joiner;
                    _joinOrder.Add(joiner.UserId);

                    if (_hostHeuristic == HostHeuristic.NewestMember && _presences.Count == 0)
                    {
                        SetHost(joiner);
                    }
                    else
                    {
                        OnGuestJoined(joiner);
                    }
                }
            }

            foreach (IUserPresence leaver in presenceEvent.Leaves)
            {
                if (_presences.ContainsKey(leaver.UserId))
                {
                    _presences.Remove(leaver.UserId);
                    _joinOrder.Remove(leaver.UserId);

                    if (_presences.Count == 0)
                    {
                        continue;
                    }

                    if (_hostHeuristic == HostHeuristic.NewestMember && _host.UserId == leaver.UserId)
                    {
                        SetHost(_presences[_joinOrder[0]]);
                    }
                    else
                    {
                        OnGuestLeft(leaver);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Leaving presence does not exist.");
                }
            }
        }

        private void SetHost(IUserPresence newHost)
        {
            IUserPresence oldHost = _host;
            _host = newHost;

            if (OnHostChanged != null)
            {
                OnHostChanged(oldHost, newHost);
            }
        }
    }
}
