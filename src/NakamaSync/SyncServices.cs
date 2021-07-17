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
        private readonly VarKeys _varKeys;
        private readonly VarRegistry _varRegistry;
        private readonly SyncSocket _syncSocket;
        private readonly PresenceTracker _presenceTracker;
        private readonly HostTracker _hostTracker;

        private readonly LockVersionGuard _lockVersionGuard;

        private readonly SharedVarIngress _sharedVarGuestIngress;
        private readonly PresenceVarIngress _presenceRoleIngress;

        private readonly SharedVarEgress _sharedVarHostEgress;

        private readonly HandshakeRequester _handshakeRequester;
        private readonly HandshakeResponder _handshakeResponder;
        private readonly HandshakeResponseHandler _handshakeResponseHandler;
        private readonly ISocket _socket;
        private readonly HostMigrator _migrator;
        private bool _initialized;

        private readonly List<ISyncService> _services = new List<ISyncService>();

        public SyncServices(ISocket socket, ISession session, VarRegistry varRegistry, SyncOpcodes opcodes)
        {
            // todo odd/annoying that these are coupled together in this manner.
            var varKeys = varRegistry.VarKeys;
            _services.Add(varKeys);

            var presenceTracker = new PresenceTracker(session.UserId);
            _services.Add(presenceTracker);

            var hostTracker = new HostTracker(presenceTracker);
            _services.Add(hostTracker);

            var lockVersionGuard = new LockVersionGuard(varKeys);
            _services.Add(lockVersionGuard);

            var syncSocket = new SyncSocket(socket, opcodes, presenceTracker);
            _services.Add(syncSocket);

            var envelopeBuilder = new EnvelopeBuilder(syncSocket);
            _services.Add(envelopeBuilder);

            var sharedGuestIngress = new SharedVarGuestIngress(varKeys, presenceTracker);
            _services.Add(sharedGuestIngress);

            var sharedHostIngress = new SharedVarHostIngress(varKeys, envelopeBuilder);
            _services.Add(sharedHostIngress);

            var presenceVarGuestIngress = new PresenceVarGuestIngress(varKeys, presenceTracker);
            _services.Add(presenceVarGuestIngress);

            var presenceVarHostIngress = new PresenceVarHostIngress(varKeys, envelopeBuilder);
            _services.Add(presenceVarHostIngress);

            var sharedVarGuestIngress = new SharedVarIngress(sharedGuestIngress, sharedHostIngress, varRegistry, lockVersionGuard);
            _services.Add(sharedVarGuestIngress);

            var presenceRoleIngress = new PresenceVarIngress(presenceVarGuestIngress, presenceVarHostIngress, varRegistry, lockVersionGuard);
            _services.Add(presenceRoleIngress);

            var handshakeRequester = new HandshakeRequester(varKeys, sharedVarGuestIngress, presenceRoleIngress, session.UserId);
            _services.Add(handshakeRequester);

            var handshakeResponder = new HandshakeResponder(varKeys, varRegistry, presenceTracker);
            _services.Add(handshakeResponder);

            var handshakeResponseHandler = new HandshakeResponseHandler(sharedVarGuestIngress, presenceRoleIngress);
            _services.Add(handshakeResponseHandler);

            var sharedVarGuestEgress = new SharedVarGuestEgress(varKeys, envelopeBuilder);
            _services.Add(sharedVarGuestIngress);

            var sharedHostEgress = new SharedVarHostEgress(varKeys, envelopeBuilder);
            _services.Add(sharedHostEgress);

            var sharedVarHostEgress = new SharedVarEgress(sharedVarGuestEgress, sharedHostEgress, hostTracker);
            _services.Add(sharedVarHostEgress);

            var migrator = new HostMigrator(varRegistry, envelopeBuilder);
            _services.Add(migrator);

            _varKeys = varKeys;
            _varRegistry = varRegistry;
            _socket = socket;
            _syncSocket = syncSocket;
            _presenceTracker = presenceTracker;
            _hostTracker = hostTracker;
            _lockVersionGuard = lockVersionGuard;

            _sharedVarGuestIngress = sharedVarGuestIngress;
            _presenceRoleIngress = presenceRoleIngress;

            _handshakeRequester = handshakeRequester;
            _handshakeResponder = handshakeResponder;
            _handshakeResponseHandler = handshakeResponseHandler;

            _sharedVarHostEgress = sharedVarHostEgress;

            _socket = socket;
            _migrator = migrator;
        }

        public Task GetHandshakeTask()
        {
             // todo handshake timeout? put it in the handshake requester.
            return _handshakeResponseHandler.GetHandshakeTask();
        }

        public void ReceiveMatch(IMatch match)
        {
            _varRegistry.ReceiveMatch(match);
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
            _migrator.Subscribe(_presenceTracker, _hostTracker);

            if (isMatchCreator)
            {
                _sharedVarHostEgress.Subscribe(_varRegistry);
                _sharedVarGuestIngress.Subscribe(_syncSocket, _hostTracker);
                _presenceRoleIngress.Subscribe(_syncSocket, _hostTracker);
            }
            else
            {
                // delay receiving and sending new values until initial store is synced
                // todo just expose the anonymous lambdas outside here, no need to hide it in
                // another subscribe call
                _handshakeRequester.Subscribe(_syncSocket, _hostTracker, _presenceTracker);

                _sharedVarHostEgress.Subscribe(_varRegistry, _handshakeRequester);
            }

            _sharedVarHostEgress.Subscribe(_varRegistry, _handshakeRequester);

            _handshakeResponder.Subscribe(_syncSocket);

            _handshakeResponseHandler.Subscribe(_handshakeRequester, _syncSocket, _hostTracker);
            _initialized = true;
        }
    }
}
