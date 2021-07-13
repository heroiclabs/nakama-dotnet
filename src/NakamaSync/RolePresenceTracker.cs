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
    internal class RolePresenceTracker
    {
        public event Action<HostChangedEvent> OnHostChanged;
        public event Action<IUserPresence> OnGuestLeft;
        public event Action<IUserPresence> OnGuestJoined;

        private readonly PresenceTracker _presenceTracker;

        public RolePresenceTracker(ISession session)
        {
            _presenceTracker = new PresenceTracker(session.UserId);
        }

        public void Subscribe(ISocket socket)
        {
            socket.ReceivedMatchPresence += _presenceTracker.HandlePresenceEvent;
            _presenceTracker.OnPresenceAdded += HandlePresenceAdded;
            _presenceTracker.OnPresenceRemoved += HandlePresenceRemoved;
        }

        public IUserPresence GetPresence(string userId)
        {
            return _presenceTracker.GetPresence(userId);
        }

        private void HandlePresenceRemoved(IUserPresence leaver)
        {
            var host = GetHost();

            bool leaverWasHost = string.Compare(leaver.UserId, host.UserId, StringComparison.InvariantCulture) < 0;

            if (leaverWasHost)
            {
                OnHostChanged?.Invoke(new HostChangedEvent(leaver, host));
            }
            else
            {
                OnGuestLeft?.Invoke(leaver);
            }
        }

        private void HandlePresenceAdded(IUserPresence joiner)
        {
            IUserPresence host = GetHost();

            if (joiner.UserId == host.UserId)
            {
                // get the next presence in the alphanumeric list
                IUserPresence oldHost = _presenceTracker.GetPresence(1);
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

            string hostId = _presenceTracker.GetSortedUserIds().First();
            return _presenceTracker.GetPresence(hostId);
        }

        public IUserPresence GetSelf()
        {
            return _presenceTracker.GetSelf();
        }

        public bool IsSelfHost()
        {
            return _presenceTracker.GetSelf().UserId == GetHost()?.UserId;
        }

        public void ReceiveMatch(IMatch match)
        {
            _presenceTracker.ReceiveMatch(match);
        }

        public int GetPresenceCount()
        {
            return _presenceTracker.GetPresenceCount();
        }
    }
}
