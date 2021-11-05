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

        private readonly IncomingVarIngress _incomingVarGuestIngress;

        private readonly SharedVarEgress _sharedVarEgress;
        private readonly SelfVarEgress _selfVarEgress;

        private readonly HandshakeRequester _handshakeRequester;
        private readonly HandshakeResponder _handshakeResponder;
        private readonly HandshakeResponseHandler _handshakeResponseHandler;
        private readonly ISocket _socket;
        private readonly HostMigrator _migrator;

        private readonly OtherVarRotators _otherVarRotators;

        private RpcIngress _rpcIngress;

        private readonly SyncCleanup _syncCleanup;

        private bool _initialized;

        private readonly List<ISyncService> _services = new List<ISyncService>();

        public SyncServices(ISocket socket, ISession session, VarRegistry varRegistry, RpcTargetRegistry rpcTargetRegistry, SyncOpcodes opcodes)
        {
            var presenceTracker = new PresenceTracker(session.UserId);
            _services.Add(presenceTracker);

            var OtherVarRotators = new OtherVarRotators(presenceTracker);
            _services.Add(OtherVarRotators);

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

            var sharedHostIngress = new IncomingVarHostIngress(lockVersionGuard, envelopeBuilder);
            _services.Add(sharedHostIngress);

            var selfVarGuestEgress = new SelfVarGuestEgress(envelopeBuilder);
            _services.Add(selfVarGuestEgress);

            var selfVarHostEgress = new SelfVarHostEgress(envelopeBuilder);
            _services.Add(selfVarHostEgress);

            var selfVarEgress = new SelfVarEgress(selfVarGuestEgress, selfVarHostEgress, presenceTracker, hostTracker);
            _services.Add(selfVarEgress);

            var incomingVarGuestIngress = new IncomingVarIngress(sharedGuestIngress, sharedHostIngress, varRegistry, lockVersionGuard);
            _services.Add(incomingVarGuestIngress);

            var handshakeRequester = new HandshakeRequester(varRegistry, presenceTracker, syncSocket, incomingVarGuestIngress, session.UserId);
            _services.Add(handshakeRequester);

            var handshakeResponder = new HandshakeResponder(lockVersionGuard, varRegistry, presenceTracker);
            _services.Add(handshakeResponder);

            var handshakeResponseHandler = new HandshakeResponseHandler(incomingVarGuestIngress);
            _services.Add(handshakeResponseHandler);

            var sharedVarGuestEgress = new SharedVarGuestEgress(lockVersionGuard, envelopeBuilder);
            _services.Add(incomingVarGuestIngress);

            var sharedHostEgress = new SharedVarHostEgress(lockVersionGuard, envelopeBuilder);
            _services.Add(sharedHostEgress);

            var sharedVarHostEgress = new SharedVarEgress(sharedVarGuestEgress, sharedHostEgress, presenceTracker, hostTracker);
            _services.Add(sharedVarHostEgress);

            var migrator = new HostMigrator(varRegistry, envelopeBuilder);
            _services.Add(migrator);

            _services.Add(rpcTargetRegistry);

            var rpcIngress = new RpcIngress(rpcTargetRegistry);
            _services.Add(rpcIngress);

            var syncCleanup = new SyncCleanup(session.UserId, varRegistry, rpcTargetRegistry);
            _syncCleanup = syncCleanup;
            _services.Add(syncCleanup);

            _varRegistry = varRegistry;
            _socket = socket;
            _syncSocket = syncSocket;
            _presenceTracker = presenceTracker;
            _hostTracker = hostTracker;
            _lockVersionGuard = lockVersionGuard;

            _incomingVarGuestIngress = incomingVarGuestIngress;

            _handshakeRequester = handshakeRequester;
            _handshakeResponder = handshakeResponder;
            _handshakeResponseHandler = handshakeResponseHandler;

            _sharedVarEgress = sharedVarHostEgress;
            _selfVarEgress = selfVarEgress;

            _socket = socket;
            _migrator = migrator;
            _otherVarRotators = OtherVarRotators;
            _rpcIngress = rpcIngress;
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

            foreach (IVar var in _varRegistry.RegisteredVars)
            {
                var.Logger = logger;
            }

            _incomingVarGuestIngress.Subscribe(_syncSocket, _hostTracker);
            _sharedVarEgress.Subscribe(_varRegistry.SharedVarRegistry);

            // no self var ingress because only self can set a presence var
            _selfVarEgress.Subscribe(_varRegistry.OtherVarRegistry);

            _handshakeRequester.Subscribe(_hostTracker);
            _handshakeResponder.Subscribe(_syncSocket);
            _handshakeResponseHandler.Subscribe(_handshakeRequester, _syncSocket, _hostTracker);

            _otherVarRotators.Register(_varRegistry.OtherVarRegistry);
            _rpcIngress.Subscribe(_syncSocket);
            _syncCleanup.Subscribe(_presenceTracker);


            _initialized = true;
            _logger?.DebugFormat("Sync services initialized.");
        }

        public SyncMatch ReceiveMatch(IMatch match)
        {
            _logger?.DebugFormat("Sync services are receiving match...");

            _varRegistry.ReceiveMatch(match);

            _logger?.DebugFormat("Sync socket is receiving match...");

            _syncSocket.ReceiveMatch(match);

            _logger?.DebugFormat("Presence tracker is receiving match...");

            _presenceTracker.ReceiveMatch(match);

            _logger?.DebugFormat("Rotators are receiving match...");

            _otherVarRotators.ReceiveMatch(match);

            _logger?.DebugFormat("Handshake requester is receiving match...");

            _handshakeRequester.ReceiveMatch(match);

            _logger?.DebugFormat("Sync services received match.");

            return new SyncMatch(match, _hostTracker, _presenceTracker, _syncSocket);
        }
    }
}
