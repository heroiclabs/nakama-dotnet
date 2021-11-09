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

namespace NakamaSync
{
    internal class VarSharedMatchState<T>
    {
        public VarEgress<T> Egress { get; }
        public VarIngress<T> Ingress { get; }
        public HandshakeRequester<T> HandshakeRequester { get; }
        public HandshakeResponder<T> HandshakeResponder { get; }
        public HandshakeResponseHandler<T> HandshakeResponseHandler { get; }
        public LockVersionGuard LockVersionGuard { get; }
        public VarRegistry<T> Registry { get; }
        public PresenceVarRotators<T> Rotators { get; }
        public SyncSocket<T> SyncSocket { get; }

        public VarSharedMatchState(VarRegistry<T> registry, SyncOpcodes opcodes, VarSessionState sessionState, VarMatchState matchState)
        {
            SyncSocket = new SyncSocket<T>(sessionState.Socket, opcodes, matchState.PresenceTracker);
            LockVersionGuard = new LockVersionGuard(registry.GetAllKeys());
            Ingress = new VarIngress<T>(registry, LockVersionGuard, matchState.HostTracker);
            HandshakeResponseHandler = new HandshakeResponseHandler<T>(Ingress);
            HandshakeResponder = new HandshakeResponder<T>(LockVersionGuard, registry, matchState.PresenceTracker);
            HandshakeRequester = new HandshakeRequester<T>(registry.GetAllKeys(), Ingress, SyncSocket);
            Egress = new VarEgress<T>(LockVersionGuard, matchState.PresenceTracker, matchState.HostTracker, SyncSocket);
            Registry = registry;
            Rotators = new PresenceVarRotators<T>(matchState.PresenceTracker, sessionState.Session);

            HandshakeRequester.Subscribe();
            HandshakeResponder.Subscribe(SyncSocket);
            HandshakeResponseHandler.Subscribe(HandshakeRequester, SyncSocket, matchState.HostTracker);
            Ingress.Subscribe(SyncSocket);
            HandshakeResponseHandler.Subscribe(HandshakeRequester, SyncSocket, matchState.HostTracker);
            HandshakeResponder.Subscribe(SyncSocket);
            HandshakeRequester.Subscribe();
            Egress.Subscribe(registry);
            Rotators.Subscribe(registry);

            HandshakeRequester.ReceiveMatch(matchState.Match);
            Rotators.ReceiveMatch(matchState.Match);
        }
    }
}
