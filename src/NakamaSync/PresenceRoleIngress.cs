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

using System.Collections.Generic;
using Nakama;

namespace NakamaSync
{
    internal class PresenceRoleIngress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly PresenceGuestIngress _presenceGuestIngress;
        private readonly PresenceHostIngress _presenceHostIngress;
        private readonly VarRegistry _registry;
        private LockVersionGuard _lockVersionGuard;

        public PresenceRoleIngress(
            PresenceGuestIngress presenceGuestIngress,
            PresenceHostIngress presenceHostIngress,
            VarRegistry registry,
            LockVersionGuard lockVersionGuard)
        {
            _presenceGuestIngress = presenceGuestIngress;
            _presenceHostIngress = presenceHostIngress;
            _registry = registry;
            _lockVersionGuard = lockVersionGuard;
        }

        public void Subscribe(SyncSocket socket, RoleTracker roleTracker, HandshakeRequester requester)
        {
            requester.OnInitialStoreLoaded += () =>
            {
                Subscribe(socket, roleTracker);
            };
        }

        public void Subscribe(SyncSocket socket, RoleTracker roleTracker)
        {
            socket.OnSyncEnvelope += (source, envelope) =>
            {
                ReceiveSyncEnvelope(source, envelope, roleTracker.IsSelfHost());
            };
        }

        public void ReceiveSyncEnvelope(IUserPresence source, Envelope envelope, bool isHost)
        {
            Logger?.DebugFormat($"User role ingress received sync envelope.");

            var bools = UserIngressContext.FromBoolValues(envelope, _registry);
            HandleSyncEnvelope(source, bools, isHost);

            var floats = UserIngressContext.FromFloatValues(envelope, _registry);
            HandleSyncEnvelope(source, floats, isHost);

            var ints = UserIngressContext.FromIntValues(envelope, _registry);
            HandleSyncEnvelope(source, ints, isHost);

            var strings = UserIngressContext.FromStringValues(envelope, _registry);
            HandleSyncEnvelope(source, strings, isHost);
        }

        private void HandleSyncEnvelope<T>(IUserPresence source, List<PresenceIngressContext<T>> contexts, bool isHost)
        {
            Logger?.DebugFormat($"User role ingress processing num contexts: {contexts.Count}");

            foreach (PresenceIngressContext<T> context in contexts)
            {
                Logger?.DebugFormat($"User role ingress processing context: {context}");

                if (!_lockVersionGuard.IsValidLockVersion(context.Value.Key, context.Value.LockVersion))
                {
                    Logger?.DebugFormat($"User role ingress received invalid lock version: {context.Value.LockVersion}");
                    continue;
                }

                if (isHost)
                {
                    Logger?.InfoFormat($"Setting user value for self as host: {context.Value}");
                    _presenceHostIngress.HandleValue(source, context);
                }
                else
                {
                    Logger?.InfoFormat($"Setting user value for self as guest: {context.Value}");
                    _presenceGuestIngress.HandleValue(context.Var, source, context.Value);
                }
            }
        }
    }
}
