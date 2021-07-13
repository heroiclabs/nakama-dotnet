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

namespace NakamaSync
{
    internal class SyncServices
    {
        public VarKeys VarKeys { get; }
        public VarRegistry VarRegistry { get; }
        public EnvelopeBuilder EnvelopeBuilder { get; }
        public SyncSocket SyncSocket { get; }

        public PresenceTracker PresenceTracker { get; }
        public RoleTracker RoleTracker { get; }

        public SharedRoleIngress SharedRoleIngress { get; }
        public UserRoleIngress UserRoleIngress { get; }

        public GuestEgress GuestEgress { get; }
        public HostEgress HostEgress { get; }
        public RoleEgress RoleEgress { get; }

        public HandshakeRequester HandshakeRequester { get; }
        public HandshakeResponder HandshakeResponder { get; }

        private ISocket _socket;
        private HostMigrator _migrator;

        public SyncServices(ISocket socket, ISession session, VarRegistry registry, SyncOpcodes opcodes)
        {
            _socket = socket;


            VarKeys = new VarKeys();
            VarRegistry = registry;
            PresenceTracker = new PresenceTracker(session.UserId);
            RoleTracker = new RoleTracker(PresenceTracker);
            SyncSocket = new SyncSocket(socket, opcodes, RoleTracker);
            EnvelopeBuilder = new EnvelopeBuilder(SyncSocket);

            GuestEgress = new GuestEgress(VarKeys, EnvelopeBuilder);
            HostEgress = new HostEgress(VarKeys, EnvelopeBuilder, RoleTracker);
            RoleEgress = new RoleEgress(GuestEgress, HostEgress, RoleTracker);

            _migrator = new HostMigrator(VarRegistry, EnvelopeBuilder);
            var guestIngress = new GuestIngress(VarKeys, PresenceTracker);
            var sharedHostIngress = new SharedHostIngress(VarKeys, EnvelopeBuilder);
            var userHostIngress = new UserHostIngress(VarKeys, EnvelopeBuilder);

            SharedRoleIngress = new SharedRoleIngress(guestIngress, sharedHostIngress, registry);
            UserRoleIngress = new UserRoleIngress(guestIngress, userHostIngress, registry);

            HandshakeRequester = new HandshakeRequester(VarKeys, SharedRoleIngress, UserRoleIngress);
            HandshakeResponder = new HandshakeResponder(VarKeys, VarRegistry, PresenceTracker);
        }

        public void Initialize(bool isMatchCreator)
        {
            PresenceTracker.Subscribe(_socket);
            _migrator.Subscribe(PresenceTracker, RoleTracker);
            SharedRoleIngress.Subscribe(SyncSocket, RoleTracker);
            UserRoleIngress.Subscribe(SyncSocket, RoleTracker);
            RoleEgress.Subscribe(VarRegistry);

            if (!isMatchCreator)
            {
                HandshakeRequester.Subscribe(SyncSocket, RoleTracker, PresenceTracker);
            }

            HandshakeResponder.Subscribe(SyncSocket);
        }
    }
}