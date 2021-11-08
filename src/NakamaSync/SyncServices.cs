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

        private readonly VarIngress _varGuestIngress;

        private readonly VarEgress _varEgress;

        private readonly HandshakeRequester _handshakeRequester;
        private readonly HandshakeResponder _handshakeResponder;
        private readonly HandshakeResponseHandler _handshakeResponseHandler;
        private readonly ISocket _socket;

        private readonly PresenceVarRotators _presenceVarRotators;

        private RpcIngress _rpcIngress;

        private readonly SyncCleanup _syncCleanup;

        private bool _initialized;

        private readonly List<ISyncService> _services = new List<ISyncService>();

        public SyncServices(ISocket socket, ISession session, VarRegistry varRegistry, RpcTargetRegistry rpcTargetRegistry, SyncOpcodes opcodes)
        {
            var presenceTracker = new PresenceTracker(session.UserId);
            _services.Add(presenceTracker);

            var PresenceVarRotators = new PresenceVarRotators(presenceTracker, session);
            _services.Add(PresenceVarRotators);

            var hostTracker = new HostTracker(presenceTracker);
            _services.Add(hostTracker);

            var lockVersionGuard = new LockVersionGuard(varRegistry);
            _services.Add(lockVersionGuard);

            var syncSocket = new SyncSocket(socket, opcodes, presenceTracker);
            _services.Add(syncSocket);

            var varGuestIngress = new VarIngress(varRegistry, lockVersionGuard, hostTracker);
            _services.Add(varGuestIngress);

            var handshakeRequester = new HandshakeRequester(varRegistry, varGuestIngress, syncSocket);
            _services.Add(handshakeRequester);

            var handshakeResponder = new HandshakeResponder(lockVersionGuard, varRegistry, presenceTracker);
            _services.Add(handshakeResponder);

            var handshakeResponseHandler = new HandshakeResponseHandler(varGuestIngress);
            _services.Add(handshakeResponseHandler);

            var varHostEgress = new VarEgress(lockVersionGuard, presenceTracker, hostTracker, syncSocket);
            _services.Add(varHostEgress);

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

            _varGuestIngress = varGuestIngress;

            _handshakeRequester = handshakeRequester;
            _handshakeResponder = handshakeResponder;
            _handshakeResponseHandler = handshakeResponseHandler;

            _varEgress = varHostEgress;

            _socket = socket;
            _presenceVarRotators = PresenceVarRotators;
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

            foreach (IVar var in _varRegistry.GetAllVars())
            {
                var.Logger = logger;
                var.HostTracker = _hostTracker;
            }

            _varGuestIngress.Subscribe(_syncSocket);
            _varEgress.Subscribe(_varRegistry);

            _handshakeRequester.Subscribe();
            _handshakeResponder.Subscribe(_syncSocket);
            _handshakeResponseHandler.Subscribe(_handshakeRequester, _syncSocket, _hostTracker);

            _presenceVarRotators.Subscribe(_varRegistry);
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

            _presenceVarRotators.ReceiveMatch(match);

            _logger?.DebugFormat("Handshake requester is receiving match...");

            _handshakeRequester.ReceiveMatch(match);
            _logger?.DebugFormat("Sync services received match.");

            return new SyncMatch(match, _hostTracker, _presenceTracker, _syncSocket);
        }
    }
}
