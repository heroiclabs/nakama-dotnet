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
            _stickyHostId.OnValueChanged += HandleStickyHostChanged;
        }

        private void HandleStickyHostChanged(IVarEvent<string> evt)
        {
            string oldSticky = evt.ValueChange.OldValue;
            string newSticky = evt.ValueChange.OldValue;
            var sortedPresences = _presenceTracker.GetSortedPresenceIds();
            var oldHost = GetHost(sortedPresences, oldSticky);
            var newHost = GetHost(sortedPresences, newSticky);
            OnHostChanged?.Invoke(new HostChangedEvent(oldHost, newHost));
        }

        public void SetHost(string userId)
        {
            // TODO MAKE SURE EVENT FIRES APPROPRIATELY IN ALL TESTS
            _stickyHostId.SetValue(userId);
        }

        public void ReceiveMatch(IMatch match)
        {
            if (!match.Presences.Any())
            {
                _stickyHostId.SetValue(match.Self.UserId);
            }

            _presenceTracker.OnPresenceAdded += HandlePresenceAdded;
            _presenceTracker.OnPresenceRemoved += HandlePresenceRemoved;
        }

        private void HandlePresenceRemoved(IUserPresence leaver)
        {
            var oldIds = _presenceTracker.GetSortedPresenceIds().Except(new string[]{leaver.UserId});
            var oldHost = GetHost(oldIds, _stickyHostId.GetValue());
            var newHost = GetHost(_presenceTracker.GetSortedPresenceIds(), _stickyHostId.GetValue());

            // old host cannot have been null
            if (leaver.UserId == oldHost.UserId)
            {
                _stickyHostId.SetValue(null);
                OnHostChanged?.Invoke(new HostChangedEvent(oldHost, newHost));
            }
            else
            {
                OnGuestLeft?.Invoke(leaver);
            }
        }

        private void HandlePresenceAdded(IUserPresence joiner)
        {
            Logger?.DebugFormat($"Host tracker for {_presenceTracker.UserId} saw joiner added: {joiner.UserId}");

            var oldIds = _presenceTracker.GetSortedPresenceIds().Except(new string[]{joiner.UserId});
            var oldHost = GetHost(oldIds, _stickyHostId.GetValue());
            var newHost = GetHost(_presenceTracker.GetSortedPresenceIds(), _stickyHostId.GetValue());

            if (oldHost.UserId != newHost.UserId)
            {
                OnHostChanged?.Invoke(new HostChangedEvent(oldHost, newHost));
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

            var ids = _presenceTracker.GetSortedPresenceIds();
            ids.Reverse();
            var guestIds = ids.Take(presenceCount);

            return _presenceTracker.GetPresences(guestIds);
        }

        public IUserPresence GetHost()
        {
            return GetHost(_presenceTracker.GetSortedPresenceIds(), _stickyHostId.GetValue());
        }

        private IUserPresence GetHost(IEnumerable<string> sortedPresenceIds, string stickyHostId)
        {
            if (!sortedPresenceIds.Any())
            {
                return null;
            }

            if (!string.IsNullOrEmpty(stickyHostId) && _presenceTracker.HasPresence(stickyHostId))
            {
                return _presenceTracker.GetPresence(stickyHostId);
            }

            string hostId = _presenceTracker.GetSortedPresenceIds().First();
            return _presenceTracker.GetPresence(hostId);
        }

        public bool IsSelfHost()
        {
            return _presenceTracker.GetSelf().UserId == GetHost(_presenceTracker.GetSortedPresenceIds(), _stickyHostId.GetValue())?.UserId;
        }
    }
}
