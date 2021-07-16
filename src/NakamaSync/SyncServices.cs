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

        private readonly SharedRoleEgress _sharedRoleEgress;
        private readonly UserRoleEgress _userRoleEgress;

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

            var syncSocket = new SyncSocket(socket, opcodes, presenceTracker);
            _services.Add(syncSocket);

            var envelopeBuilder = new EnvelopeBuilder(syncSocket);
            _services.Add(envelopeBuilder);

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

            var handshakeRequester = new HandshakeRequester(varKeys, sharedRoleIngress, userRoleIngress, session.UserId);
            _services.Add(handshakeRequester);

            var handshakeResponder = new HandshakeResponder(varKeys, varRegistry, presenceTracker);
            _services.Add(handshakeResponder);

            var sharedGuestEgress = new SharedGuestEgress(varKeys, envelopeBuilder);
            _services.Add(sharedGuestEgress);

            var sharedHostEgress = new SharedHostEgress(varKeys, envelopeBuilder);
            _services.Add(sharedHostEgress);

            var userGuestEgress = new UserGuestEgress(varKeys, envelopeBuilder);
            _services.Add(userGuestEgress);

            var userHostEgress = new UserHostEgress(varKeys, envelopeBuilder);
            _services.Add(userHostEgress);

            var sharedRoleEgress = new SharedRoleEgress(sharedGuestEgress, sharedHostEgress, roleTracker);
            _services.Add(sharedRoleEgress);

            var userRoleEgress = new UserRoleEgress(userGuestEgress, userHostEgress, roleTracker);
            _services.Add(userRoleEgress);

            var migrator = new HostMigrator(varRegistry, envelopeBuilder);
            _services.Add(migrator);

            _varKeys = varKeys;
            _varRegistry = varRegistry;
            _socket = socket;
            _syncSocket = syncSocket;
            _presenceTracker = presenceTracker;
            _roleTracker = roleTracker;
            _lockVersionGuard = lockVersionGuard;

            _sharedRoleIngress = sharedRoleIngress;
            _userRoleIngress = userRoleIngress;

            _handshakeRequester = handshakeRequester;
            _handshakeResponder = handshakeResponder;

            _sharedRoleEgress = sharedRoleEgress;
            _userRoleEgress = userRoleEgress;

            _socket = socket;
            _migrator = migrator;
        }

        public void ReceiveMatch(IMatch match)
        {
            _varRegistry.ReceiveMatch(_varKeys, match);
            _syncSocket.ReceiveMatch(match);
            _presenceTracker.ReceiveMatch(match);
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

            if (isMatchCreator)
            {
                _sharedRoleEgress.Subscribe(_varRegistry);
                _sharedRoleIngress.Subscribe(_syncSocket, _roleTracker);
                _userRoleIngress.Subscribe(_syncSocket, _roleTracker);
            }
            else
            {
                // delay receiving and sending new values until initial store is synced
                // todo just expose the anonymous lambdas outside here, no need to hide it in
                // another subscribe call
                _handshakeRequester.Subscribe(_syncSocket, _roleTracker, _presenceTracker);

                _sharedRoleEgress.Subscribe(_varRegistry, _handshakeRequester);
                _userRoleEgress.Subscribe(_varRegistry, _handshakeRequester);

                _sharedRoleIngress.Subscribe(_syncSocket, _roleTracker, _handshakeRequester);
                _userRoleIngress.Subscribe(_syncSocket, _roleTracker, _handshakeRequester);
            }

            _userRoleEgress.Subscribe(_varRegistry, _handshakeRequester);
            _sharedRoleEgress.Subscribe(_varRegistry, _handshakeRequester);

            _handshakeResponder.Subscribe(_syncSocket);

            _initialized = true;
        }
    }
}
