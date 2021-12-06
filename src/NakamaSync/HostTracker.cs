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
    // TODO catch presence tracker exceptions. and think about how to handle exceptions for the sync system in general
    internal class HostTracker
    {
        public event Action<IHostChangedEvent> OnHostChanged;
        public event Action<IUserPresence> OnGuestLeft;
        public event Action<IUserPresence> OnGuestJoined;

        public ILogger Logger { get; set; }

        private readonly PresenceTracker _presenceTracker;

        // the first host of the match is the first player to join. after that, we use alphanumeric sort on the userids.
        // we do this because if we used alphanumeric sort the whole time, there would be a lot of host changes
        // right as users enter the match.
        private readonly SharedVar<string> _stickyHostId;

        public HostTracker(PresenceTracker presenceTracker, SharedVar<string> stickyHostId)
        {
            _presenceTracker = presenceTracker;
            _stickyHostId = stickyHostId;
        }

        public void Subscribe(ISocket socket)
        {
            _presenceTracker.OnPresenceAdded += HandlePresenceAdded;
            _presenceTracker.OnPresenceRemoved += HandlePresenceRemoved;
        }

        private void HandlePresenceRemoved(IUserPresence leaver)
        {
            Logger?.DebugFormat($"Host tracker for {_presenceTracker.UserId} saw leaver: {leaver.UserId}");

            var newHost = GetHost();

            bool leaverWasHost = string.Compare(leaver.UserId, newHost.UserId, StringComparison.InvariantCulture) < 0;

            if (leaverWasHost)
            {
                _stickyHostId.SetValue(null);
                OnHostChanged?.Invoke(new HostChangedEvent(leaver, newHost));
            }
            else
            {
                OnGuestLeft?.Invoke(leaver);
            }
        }

        private void HandlePresenceAdded(IUserPresence joiner)
        {
            if (joiner.UserId == _presenceTracker.GetSelf().UserId && _presenceTracker.GetPresenceCount() == 1)
            {
                // first to match is host
                _stickyHostId.SetValue(joiner.UserId);
            }

            Logger?.DebugFormat($"Host tracker for {_presenceTracker.UserId} saw joiner added: {joiner.UserId}");

            IUserPresence host = GetHost();

            System.Console.WriteLine("HANDLING PRESENCE ADDED");

            if (joiner.UserId != host.UserId)
            {
                System.Console.WriteLine("DISPATCHING ON HOST CHANGED");

                // get the next presence in the alphanumeric list
                IUserPresence oldHost = _presenceTracker.GetPresence(index: 1);
                Logger?.DebugFormat($"Host tracker changing host from {oldHost?.UserId} to {host.UserId}");
                OnHostChanged?.Invoke(new HostChangedEvent(oldHost, host));
            }
            else
            {
                OnGuestJoined?.Invoke(joiner);
            }
        }

        public IEnumerable<IUserPresence> GetGuests()
        {
            int presenceCount = _presenceTracker.GetPresenceCount();

            if (presenceCount <= 1)
            {
                return new IUserPresence[]{};
            }

            var ids = _presenceTracker.GetSortedUserIds();
            ids.Reverse();
            var guestIds = ids.Take(presenceCount);

            return _presenceTracker.GetPresences(guestIds);
        }

        public IUserPresence GetHost()
        {
            if (_presenceTracker.GetPresenceCount() == 0)
            {
                return null;
            }

            if (_stickyHostId.GetValue() != null && _presenceTracker.HasPresence(_stickyHostId.GetValue()))
            {
                return _presenceTracker.GetPresence(_stickyHostId.GetValue());
            }

            string hostId = _presenceTracker.GetSortedUserIds().First();
            return _presenceTracker.GetPresence(hostId);
        }

        public bool IsSelfHost()
        {
            return _presenceTracker.GetSelf().UserId == GetHost()?.UserId;
        }
    }
}
