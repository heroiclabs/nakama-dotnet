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
    internal class PresenceVarIngress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly PresenceVarGuestIngress _presenceVarGuestIngress;
        private readonly PresenceVarHostIngress _presenceVarHostIngress;
        private readonly VarRegistry _registry;
        private readonly PresenceVarRotators _presenceVarRotators;

        public PresenceVarIngress(
            VarRegistry registry,
            PresenceVarRotators presenceVarRotators,
            PresenceVarGuestIngress presenceVarGuestIngress,
            PresenceVarHostIngress presenceVarHostIngress)
        {
            _registry = registry;
            _presenceVarGuestIngress = presenceVarGuestIngress;
            _presenceVarHostIngress = presenceVarHostIngress;
            _presenceVarRotators = presenceVarRotators;
        }

        public void Subscribe(SyncSocket socket, HostTracker hostTracker)
        {
            socket.OnSyncEnvelope += (source, envelope) =>
            {
                ReceiveSyncEnvelope(source, envelope, hostTracker.IsSelfHost());
            };
        }

        public void ReceiveSyncEnvelope(IUserPresence source, Envelope envelope, bool isHost)
        {
            Logger?.DebugFormat($"PresenceVarIngress received sync envelope from source: {source.UserId}");

            try
            {
                var bools = PresenceVarIngressContext.FromBoolValues(envelope, _registry.PresenceVarRegistry, _presenceVarRotators);
                HandleSyncEnvelope(source, bools, isHost);

                var floats = PresenceVarIngressContext.FromFloatValues(envelope, _registry.PresenceVarRegistry, _presenceVarRotators);
                HandleSyncEnvelope(source, floats, isHost);

                var ints = PresenceVarIngressContext.FromIntValues(envelope, _registry.PresenceVarRegistry, _presenceVarRotators);
                HandleSyncEnvelope(source, ints, isHost);

                var strings = PresenceVarIngressContext.FromStringValues(envelope, _registry.PresenceVarRegistry, _presenceVarRotators);
                HandleSyncEnvelope(source, strings, isHost);
            }
            catch (Exception e)
            {
                ErrorHandler?.Invoke(e);
            }
        }

        private void HandleSyncEnvelope<T>(IUserPresence source, List<PresenceVarIngressContext<T>> contexts, bool isHost)
        {
            Logger?.DebugFormat($"PresenceVarIngress processing num contexts: {contexts.Count}");

            foreach (PresenceVarIngressContext<T> context in contexts)
            {
                Logger?.DebugFormat($"PresenceVarIngress processing context: {context}");

                if (isHost)
                {
                    Logger?.InfoFormat($"Setting user value for {context.Var.Presence.UserId} as host: {context.Value}");
                    _presenceVarHostIngress.HandleValue(source, context);
                }
                else
                {
                    Logger?.InfoFormat($"Setting user value for {context.Var.Presence.UserId} as guest: {context.Value}");
                    _presenceVarGuestIngress.HandleValue(context.Var, source, context.Value);
                }
            }
        }
    }
}
