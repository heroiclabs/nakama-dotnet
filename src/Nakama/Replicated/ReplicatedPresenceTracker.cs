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
    // todo dynamic host choosing, and populate guests
    internal class ReplicatedPresenceTracker
    {
        public delegate void ReplicatedHostChangedHandler(ReplicatedHost oldHost, ReplicatedHost newHost);

        public event Action<ReplicatedGuest> OnReplicatedGuestJoined;
        public event Action<ReplicatedGuest> OnReplicatedGuestLeft;
        public event ReplicatedHostChangedHandler OnReplicatedHostChanged;

        public IEnumerable<ReplicatedGuest> Guests => _guests.Values;
        public ReplicatedHost Host => _host;
        public IReplicatedMember Self => _self;

        private readonly Dictionary<string, ReplicatedGuest> _guests = new Dictionary<string, ReplicatedGuest>();
        private ReplicatedHost _host;
        private readonly Dictionary<string, IReplicatedMember> _others = new Dictionary<string, IReplicatedMember>();
        private readonly PresenceTracker _presenceTracker;
        private IReplicatedMember _self;
        private ReplicatedValueStore _valStore;
        private ReplicatedVarStore _varStore;

        public ReplicatedPresenceTracker(IUserPresence self, ReplicatedValueStore valStore, ReplicatedVarStore varStore)
        {
            _presenceTracker = new PresenceTracker(self, trackHost: true, PresenceTracker.HostHeuristic.NewestMember);
            _valStore = valStore;
            _varStore = varStore;
            _presenceTracker.OnHostChanged += HandleHostChanged;
            _presenceTracker.OnGuestJoined += HandleGuestJoined;
            _presenceTracker.OnGuestLeft += HandleGuestLeft;
        }

        public void HandlePresenceEvent(IMatchPresenceEvent presenceEvent)
        {
            _presenceTracker.HandlePresenceEvent(presenceEvent);
        }

        public bool SelfIsHost()
        {
            return Self.Presence.UserId == _host.Presence.UserId;
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

            _host = new ReplicatedHost(newHost, this, _varStore);

            if (_host.Presence.UserId == _presenceTracker.Self.UserId)
            {
                _self = _host;
            }
            else
            {
                AddGuest(_presenceTracker.Self);
            }

            OnReplicatedHostChanged(oldReplicatedHost, _host);
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

            var newGuest = new ReplicatedGuest(presence, this, _varStore);
            _guests[presence.UserId] = newGuest;
            OnReplicatedGuestJoined?.Invoke(newGuest);
        }

        private void RemoveGuest(IUserPresence presence)
        {
            if (!_guests.ContainsKey(presence.UserId))
            {
                throw new InvalidOperationException("Leaving guest does not exist.");
            }

            var oldGuest = new ReplicatedGuest(presence, this, _varStore);
            _guests.Remove(presence.UserId);
            OnReplicatedGuestLeft?.Invoke(oldGuest);
        }
    }
}
