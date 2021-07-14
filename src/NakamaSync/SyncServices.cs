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
        private readonly VarKeys _varKeys;
        private readonly VarRegistry _varRegistry;
        private readonly SyncSocket _syncSocket;
        private readonly PresenceTracker _presenceTracker;
        private readonly RoleTracker _roleTracker;

        private readonly LockVersionGuard _lockVersionGuard;
        private readonly SharedRoleIngress _sharedRoleIngress;
        private readonly UserRoleIngress _userRoleIngress;
        private readonly RoleEgress _roleEgress;

        private readonly HandshakeRequester _handshakeRequester;
        private readonly HandshakeResponder _handshakeResponder;
        private readonly ISocket _socket;
        private readonly HostMigrator _migrator;
        private bool _initialized;

        private readonly List<ISyncService> _services = new List<ISyncService>();

        public SyncServices(ISocket socket, ISession session, VarRegistry varRegistry, SyncOpcodes opcodes)
        {
            var varKeys = new VarKeys();
            _services.Add(varKeys);

            var presenceTracker = new PresenceTracker(session.UserId);
            _services.Add(presenceTracker);

            var roleTracker = new RoleTracker(presenceTracker);
            _services.Add(roleTracker);

            var lockVersionGuard = new LockVersionGuard(varKeys);
            _services.Add(lockVersionGuard);

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

            var sharedGuestIngress = new SharedGuestIngress(varKeys, presenceTracker);
            _services.Add(sharedGuestIngress);

            var sharedHostIngress = new SharedHostIngress(varKeys, envelopeBuilder);
            _services.Add(sharedHostIngress);

            var userGuestIngress = new UserGuestIngress(varKeys, presenceTracker);
            _services.Add(userGuestIngress);

            var userHostIngress = new UserHostIngress(varKeys, envelopeBuilder);
            _services.Add(userHostIngress);

            var sharedRoleIngress = new SharedRoleIngress(sharedGuestIngress, sharedHostIngress, varRegistry, lockVersionGuard);
            _services.Add(sharedRoleIngress);

            var userRoleIngress = new UserRoleIngress(userGuestIngress, userHostIngress, varRegistry, lockVersionGuard);
            _services.Add(userRoleIngress);

            var handshakeRequester = new HandshakeRequester(varKeys, sharedRoleIngress, userRoleIngress);
            _services.Add(handshakeRequester);

            var handshakeResponder = new HandshakeResponder(varKeys, varRegistry, presenceTracker);
            _services.Add(handshakeResponder);

            _varKeys = varKeys;
            _varRegistry = varRegistry;
            _socket = socket;
            _syncSocket = syncSocket;
            _presenceTracker = presenceTracker;
            _roleTracker = roleTracker;
            _lockVersionGuard = lockVersionGuard;
            _sharedRoleIngress = sharedRoleIngress;
            _userRoleIngress = userRoleIngress;
            _roleEgress = roleEgress;
            _handshakeRequester = handshakeRequester;
            _handshakeResponder = handshakeResponder;
            _socket = socket;
            _migrator = migrator;
        }

        public void ReceiveMatch(IMatch match)
        {
            _varRegistry.ReceiveMatch(_varKeys, match);
            _syncSocket.ReceiveMatch(match);
        }

        public void Initialize(bool isMatchCreator, SyncErrorHandler errorHandler, ILogger logger)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("Sync services have already been initialized.");
            }

            foreach (ISyncService service in _services)
            {
                service.ErrorHandler = errorHandler;
                service.Logger = logger;
            }

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

            _initialized = true;
        }
    }
}
