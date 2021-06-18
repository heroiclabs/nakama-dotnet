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

namespace Nakama.Replicated
{
    internal class ReplicatedPresenceTracker
    {
        public delegate void ReplicatedHostChangedHandler(ReplicatedHost oldHost, ReplicatedHost newHost);

        public event Action<ReplicatedGuest> OnReplicatedGuestJoined;
        public event Action<ReplicatedGuest> OnReplicatedGuestLeft;
        public event ReplicatedHostChangedHandler OnReplicatedHostChanged;

        public IReadOnlyDictionary<string, ReplicatedGuest> Guests => _guests;
        public ReplicatedHost Host => _host;

        private readonly Dictionary<string, ReplicatedGuest> _guests = new Dictionary<string, ReplicatedGuest>();
        private ReplicatedHost _host;
        private readonly PresenceTracker _presenceTracker;
        private readonly ISession _session;
        private Store _ownedStore;

        public ReplicatedPresenceTracker(ISession session, Store ownedStore)
        {
            _session = session;
            _presenceTracker = new PresenceTracker(trackHost: true, PresenceTracker.HostHeuristic.NewestMember);
            _ownedStore = ownedStore;
            _presenceTracker.OnHostChanged += HandleHostChanged;
            _presenceTracker.OnGuestJoined += HandleGuestJoined;
            _presenceTracker.OnGuestLeft += HandleGuestLeft;
        }

        private void HandleHostChanged(IUserPresence oldHost, IUserPresence newHost)
        {
            ReplicatedHost oldReplicatedHost = _host;

            if (newHost == null)
            {
                // TODO match ended
                OnReplicatedHostChanged?.Invoke(oldReplicatedHost, null);
                return;
            }

            _host = new ReplicatedHost(_presenceTracker, _ownedStore);
            OnReplicatedHostChanged(oldReplicatedHost, _host);
        }

        public void HandlePresenceEvent(IMatchPresenceEvent presenceEvent)
        {
            _presenceTracker.HandlePresenceEvent(presenceEvent);
        }

        public IReplicatedMember GetSelf()
        {
            if (_host.Presence.UserId == _session.UserId)
            {
                return _host;
            }

            if (_guests.ContainsKey(_session.UserId))
            {
                return _guests[_session.UserId];
            }

            throw new KeyNotFoundException("Could not find self.");
        }

        private void HandleGuestLeft(IUserPresence guest)
        {
            RemoveGuest(guest);
        }

        private void HandleGuestJoined(IUserPresence guest)
        {
            AddGuest(guest);
        }

        private void AddGuest(IUserPresence presence)
        {
            if (_guests.ContainsKey(presence.UserId))
            {
                throw new InvalidOperationException("Joining guest already exists.");
            }

            var newGuest = new ReplicatedGuest(presence, _presenceTracker, _ownedStore);
            _guests[presence.UserId] = newGuest;
            OnReplicatedGuestJoined?.Invoke(newGuest);
        }

        private void RemoveGuest(IUserPresence presence)
        {
            if (!_guests.ContainsKey(presence.UserId))
            {
                throw new InvalidOperationException("Leaving guest does not exist.");
            }

            var oldGuest = new ReplicatedGuest(presence, _presenceTracker, _ownedStore);
            _guests.Remove(presence.UserId);
            OnReplicatedGuestLeft?.Invoke(oldGuest);
        }
    }
}
