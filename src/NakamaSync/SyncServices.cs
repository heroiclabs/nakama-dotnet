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
using System.Threading.Tasks;
using Nakama;

namespace NakamaSync
{
    public delegate void SyncErrorHandler(Exception e);

    internal class SyncServices
    {
        private ILogger _logger;

        private readonly VarRegistry _varRegistry;
        private readonly SyncSocket _syncSocket;
        private readonly PresenceTracker _presenceTracker;
        private readonly HostTracker _hostTracker;

        private readonly LockVersionGuard _lockVersionGuard;

        private readonly SharedVarIngress _sharedVarIngress;
        private readonly PresenceVarIngress _presenceVarIngress;

        private readonly SharedVarEgress _sharedVarEgress;
        private readonly SelfVarEgress _selfVarEgress;

        private readonly HandshakeRequester _handshakeRequester;
        private readonly HandshakeResponder _handshakeResponder;
        private readonly HandshakeResponseHandler _handshakeResponseHandler;
        private readonly ISocket _socket;
        private readonly HostMigrator _migrator;

        private readonly PresenceVarRotators _presenceVarRotators;

        private readonly SyncCleanup _syncCleanup;

        private bool _initialized;

        private readonly List<ISyncService> _services = new List<ISyncService>();

        public SyncServices(ISocket socket, ISession session, VarRegistry varRegistry, SyncOpcodes opcodes)
        {
            var presenceTracker = new PresenceTracker(session.UserId);
            _services.Add(presenceTracker);

            var presenceVarRotators = new PresenceVarRotators(presenceTracker);
            _services.Add(presenceVarRotators);

            var hostTracker = new HostTracker(presenceTracker);
            _services.Add(hostTracker);

            var lockVersionGuard = new LockVersionGuard(varRegistry);
            _services.Add(lockVersionGuard);

            var syncSocket = new SyncSocket(socket, opcodes, presenceTracker);
            _services.Add(syncSocket);

            var envelopeBuilder = new EnvelopeBuilder(syncSocket);
            _services.Add(envelopeBuilder);

            var sharedGuestIngress = new SharedVarGuestIngress(presenceTracker);
            _services.Add(sharedGuestIngress);

            var sharedHostIngress = new SharedVarHostIngress(lockVersionGuard, envelopeBuilder);
            _services.Add(sharedHostIngress);

            var selfVarGuestEgress = new SelfVarGuestEgress(envelopeBuilder);
            _services.Add(selfVarGuestEgress);

            var selfVarHostEgress = new SelfVarHostEgress(envelopeBuilder);
            _services.Add(selfVarHostEgress);

            var selfVarEgress = new SelfVarEgress(selfVarGuestEgress, selfVarHostEgress, presenceTracker, hostTracker);
            _services.Add(selfVarEgress);

            var presenceVarGuestIngress = new PresenceVarGuestIngress(presenceTracker);
            _services.Add(presenceVarGuestIngress);

            var presenceVarHostIngress = new PresenceVarHostIngress(lockVersionGuard, envelopeBuilder);
            _services.Add(presenceVarHostIngress);

            var sharedVarGuestIngress = new SharedVarIngress(sharedGuestIngress, sharedHostIngress, varRegistry, lockVersionGuard);
            _services.Add(sharedVarGuestIngress);

            var presenceRoleIngress = new PresenceVarIngress(session.UserId, varRegistry, presenceVarRotators, presenceVarGuestIngress, presenceVarHostIngress);
            _services.Add(presenceRoleIngress);

            var handshakeRequester = new HandshakeRequester(varRegistry, presenceTracker, syncSocket, sharedVarGuestIngress, presenceRoleIngress, session.UserId);
            _services.Add(handshakeRequester);

            var handshakeResponder = new HandshakeResponder(lockVersionGuard, varRegistry, presenceTracker);
            _services.Add(handshakeResponder);

            var handshakeResponseHandler = new HandshakeResponseHandler(sharedVarGuestIngress, presenceRoleIngress);
            _services.Add(handshakeResponseHandler);

            var sharedVarGuestEgress = new SharedVarGuestEgress(lockVersionGuard, envelopeBuilder);
            _services.Add(sharedVarGuestIngress);

            var sharedHostEgress = new SharedVarHostEgress(lockVersionGuard, envelopeBuilder);
            _services.Add(sharedHostEgress);

            var sharedVarHostEgress = new SharedVarEgress(sharedVarGuestEgress, sharedHostEgress, presenceTracker, hostTracker);
            _services.Add(sharedVarHostEgress);

            var migrator = new HostMigrator(varRegistry, envelopeBuilder);
            _services.Add(migrator);

            var syncCleanup = new SyncCleanup(session.UserId, varRegistry);
            _syncCleanup = syncCleanup;
            _services.Add(syncCleanup);

            _varRegistry = varRegistry;
            _socket = socket;
            _syncSocket = syncSocket;
            _presenceTracker = presenceTracker;
            _hostTracker = hostTracker;
            _lockVersionGuard = lockVersionGuard;

            _sharedVarIngress = sharedVarGuestIngress;
            _presenceVarIngress = presenceRoleIngress;

            _handshakeRequester = handshakeRequester;
            _handshakeResponder = handshakeResponder;
            _handshakeResponseHandler = handshakeResponseHandler;

            _sharedVarEgress = sharedVarHostEgress;
            _selfVarEgress = selfVarEgress;

            _socket = socket;
            _migrator = migrator;
            _presenceVarRotators = presenceVarRotators;
        }

        public Task GetHandshakeTask()
        {
             // todo handshake timeout? put it in the handshake requester.
            return _handshakeResponseHandler.GetHandshakeTask();
        }

        public void Initialize(bool isMatchCreator, SyncErrorHandler errorHandler, ILogger logger)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("Sync services have already been initialized.");
            }

            _logger = logger;

            foreach (ISyncService service in _services)
            {
                service.ErrorHandler = errorHandler;
                service.Logger = logger;
            }

            _logger?.DebugFormat("Sync services initializing...");

            _presenceTracker.Subscribe(_socket);
            _hostTracker.Subscribe(_socket);
            _migrator.Subscribe(_presenceTracker, _hostTracker);
            if (isMatchCreator)
            {
                _sharedVarEgress.Subscribe(_varRegistry.SharedVarRegistry);
                _sharedVarIngress.Subscribe(_syncSocket, _hostTracker);
                _presenceVarIngress.Subscribe(_syncSocket, _hostTracker);
                _selfVarEgress.Subscribe(_varRegistry.PresenceVarRegistry);
            }
            else
            {
                // delay receiving and sending new values until initial store is synced
                // todo just expose the anonymous lambdas outside here, no need to hide it in
                // another subscribe call
                _handshakeRequester.Subscribe(_hostTracker);
                _sharedVarEgress.Subscribe(_varRegistry.SharedVarRegistry, _handshakeRequester);
                _selfVarEgress.Subscribe(_varRegistry.PresenceVarRegistry, _handshakeRequester);
            }

            _sharedVarEgress.Subscribe(_varRegistry.SharedVarRegistry, _handshakeRequester);
            _handshakeResponder.Subscribe(_syncSocket);
            _handshakeResponseHandler.Subscribe(_handshakeRequester, _syncSocket, _hostTracker);

            _presenceVarRotators.Register(_varRegistry.PresenceVarRegistry);
            _syncCleanup.Subscribe(_presenceTracker);

            _initialized = true;
            _logger?.DebugFormat("Sync services initialized.");
        }

        public SyncMatch ReceiveMatch(IMatch match)
        {
            _logger?.DebugFormat("Sync services are receiving match...");

            _varRegistry.ReceiveMatch(match);
            _syncSocket.ReceiveMatch(match);
            _presenceTracker.ReceiveMatch(match);
            _presenceVarRotators.ReceiveMatch(match);
            _handshakeRequester.ReceiveMatch(match);
            _logger?.DebugFormat("Sync services received match.");

            return new SyncMatch(match, _hostTracker);
        }
    }
}
