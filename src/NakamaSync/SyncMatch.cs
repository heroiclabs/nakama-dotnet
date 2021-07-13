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

using Nakama;
using System.Threading.Tasks;

namespace NakamaSync
{
    // todo entire concurrency pass on all this
    // enforce key ids should be unique across types
    // TODO what if someone changes the type collection that the key is in, will try to send to the incorrect type
    // between clients and may pass handshake.
    // if user leaves and then rejoins do their values come back? they are still in collection but
    // are they received by that user on initial sync? I think so.
    // catch all exceptions and route them through the OnError event?
    // todo protobuf support when that is merged.
    // todo think about fact that user can still attach to presence handler without being able to sequence presence events as they see fit.
    // todo synced composite object?
    // todo synced list
    // todo potential race when creating and joining a match between the construction of this object
    // and the dispatching of presence objects off the socket.
    // TODO restore the default getvalue call with self
    // ~destructor definitely doesn't work, think about end match flow.
    // fix OnHostValdiate so that you have a better way of signalling intent that you want a var to be validated.
    // to string calls
    // expose interfaces, not concrete classes.
    // todo rename this class?
    // todo handle host changed
    // todo handle guest left
    // todo migrate pending values when host chanes
    // override tostring
    // todo remove any event subscriptions in constructors.
    // todo parameter ordering
    // todo internalize the registry so that users can't change it from the outside?
    // otherwise find some other way to prevent users from messing with it.
    // todo error handling checks particularly on dictionary accessing etc.
    // todo clean this class up and use explicit returns values where needed for some of these private methods
    // should user vars have acks on a uesr by user basis?
    public class SyncMatch
    {
        private readonly Handshaker _handshaker;
        private readonly SyncSocket _socket;
        private readonly RolePresenceTracker _rolePresenceTracker;
        private readonly PresenceTracker _presenceTracker;
        private SyncVarRegistry _registry;
        private VarKeys _keys;
        private EnvelopeBuilder _builder;
        private SharedRoleIngress _sharedIngress;
        private UserRoleIngress _userIngress;

        internal SyncMatch(ISession session, SyncSocket socket, SyncVarRegistry registry, PresenceTracker presenceTracker, RolePresenceTracker rolePresenceTracker)
        {
            _presenceTracker = presenceTracker;
            _rolePresenceTracker = rolePresenceTracker;
            _socket = socket;
            _registry = registry;
            _keys = new VarKeys();
            _builder = new EnvelopeBuilder(socket);

            registry.Register(_keys, presenceTracker.GetSelf());
            CreateIngresses();
            CreateEgresses();

            _rolePresenceTracker.OnHostChanged += HandleHostChanged;

            _handshaker = CreateHandshaker(_sharedIngress, _userIngress);
        }

        public Task WaitForHandshake()
        {
            return _handshaker.WaitForHandshake();
        }

        private void HandleHostChanged(HostChangedEvent evt)
        {
            if (evt.NewHost == _presenceTracker.GetSelf())
            {
                // pick up where the old host left off in terms of validating values.
                new HostMigration(_builder).Migrate(_registry);
            }
        }

        private void CreateIngresses()
        {
            var guestIngress = new GuestIngress(_keys, _rolePresenceTracker);
            var sharedHostIngress = new SharedHostIngress(_builder, _keys);
            var userHostIngress = new UserHostIngress(_builder, _keys);

            var sharedRoleIngress = new SharedRoleIngress(guestIngress, sharedHostIngress, _registry);
            sharedRoleIngress.Subscribe(_socket, _rolePresenceTracker);

            var userRoleIngress = new UserRoleIngress(guestIngress, userHostIngress, _registry);
            userRoleIngress.Subscribe(_socket, _rolePresenceTracker);
        }

        private void CreateEgresses()
        {
            var guestEgress = new GuestEgress(_keys, _builder);
            var hostEgress = new HostEgress(_builder, _keys, _presenceTracker);

            var egress = new RoleEgress(guestEgress, hostEgress, _rolePresenceTracker);
            egress.Subscribe(_registry);
        }

        private Handshaker CreateHandshaker(SharedRoleIngress sharedIngress, UserRoleIngress userIngress)
        {
            var hostHandshaker = new HostHandshaker(_keys, _registry, _presenceTracker);
            hostHandshaker.ListenForHandshakes(_socket);

            var guestHandshaker = new GuestHandshaker(_keys, sharedIngress, userIngress, _rolePresenceTracker, _socket);

            return new Handshaker(guestHandshaker, hostHandshaker, _rolePresenceTracker);
        }
    }
}
