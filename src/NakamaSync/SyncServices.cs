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
using Nakama;

namespace NakamaSync
{
    public delegate void SyncErrorHandler(Exception e);

    internal class SyncServices
    {
        private VarKeys _varKeys;
        private VarRegistry _varRegistry;
        private SyncSocket _syncSocket;
        private PresenceTracker _presenceTracker;
        private RoleTracker _roleTracker;
        private SharedRoleIngress _sharedRoleIngress;
        private UserRoleIngress _userRoleIngress;
        private RoleEgress _roleEgress;

        private HandshakeRequester _handshakeRequester;
        private HandshakeResponder _handshakeResponder;
        private ISocket _socket;
        private HostMigrator _migrator;

        private readonly List<ISyncService> _services = new List<ISyncService>();

        public SyncServices(ISocket socket, ISession session, VarRegistry varRegistry, SyncOpcodes opcodes)
        {
            var varKeys = new VarKeys();
            _services.Add(varKeys);

            var presenceTracker = new PresenceTracker(session.UserId);
            _services.Add(presenceTracker);

            var roleTracker = new RoleTracker(presenceTracker);
            _services.Add(roleTracker);

            var syncSocket = new SyncSocket(socket, opcodes, roleTracker);
            _services.Add(syncSocket);

            var envelopeBuilder = new EnvelopeBuilder(syncSocket);
            _services.Add(envelopeBuilder);

            var guestEgress = new GuestEgress(varKeys, envelopeBuilder);
            _services.Add(guestEgress);

            var hostEgress = new HostEgress(varKeys, envelopeBuilder, roleTracker);
            _services.Add(hostEgress);

            var roleEgress = new RoleEgress(guestEgress, hostEgress, roleTracker);
            _services.Add(roleEgress);

            var migrator = new HostMigrator(varRegistry, envelopeBuilder);
            _services.Add(migrator);

            var guestIngress = new GuestIngress(varKeys, presenceTracker);
            _services.Add(guestIngress);

            var sharedHostIngress = new SharedHostIngress(varKeys, envelopeBuilder);
            _services.Add(sharedHostIngress);

            var userHostIngress = new UserHostIngress(varKeys, envelopeBuilder);
            _services.Add(userHostIngress);

            var sharedRoleIngress = new SharedRoleIngress(guestIngress, sharedHostIngress, varRegistry);
            _services.Add(sharedHostIngress);

            var userRoleIngress = new UserRoleIngress(guestIngress, userHostIngress, varRegistry);
            _services.Add(userRoleIngress);

            var handshakeRequester = new HandshakeRequester(varKeys, sharedRoleIngress, userRoleIngress);
            _services.Add(handshakeRequester);

            var handshakeResponder = new HandshakeResponder(varKeys, varRegistry, presenceTracker);
            _services.Add(handshakeResponder);

            _varKeys = varKeys;
            _varRegistry = varRegistry;
            _socket = socket;
            _presenceTracker = presenceTracker;
            _roleTracker = roleTracker;
            _sharedRoleIngress = sharedRoleIngress;
            _userRoleIngress = userRoleIngress;
            _roleEgress = roleEgress;
            _handshakeRequester = handshakeRequester;
            _handshakeResponder = handshakeResponder;
            _socket = socket;
            _migrator = migrator;
        }

        public void SetLogger(ILogger logger)
        {
            foreach (ISyncService service in _services)
            {
                service.Logger = logger;
            }
        }

        public void SetErrorHandler(SyncErrorHandler errorHandler)
        {
            foreach (ISyncService service in _services)
            {
                service.ErrorHandler = errorHandler;
            }
        }

        internal void ReceiveMatch(IMatch match)
        {
            _varRegistry.ReceiveMatch(_varKeys, match);
            _syncSocket.ReceiveMatch(match);
        }

        internal void Initialize(bool isMatchCreator)
        {
            _presenceTracker.Subscribe(_socket);
            _migrator.Subscribe(_presenceTracker, _roleTracker);
            _sharedRoleIngress.Subscribe(_syncSocket, _roleTracker);
            _userRoleIngress.Subscribe(_syncSocket, _roleTracker);
            _roleEgress.Subscribe(_varRegistry);

            if (!isMatchCreator)
            {
                _handshakeRequester.Subscribe(_syncSocket, _roleTracker, _presenceTracker);
            }

            _handshakeResponder.Subscribe(_syncSocket);
        }
    }
}
