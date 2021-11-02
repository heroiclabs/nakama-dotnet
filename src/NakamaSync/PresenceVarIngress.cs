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
    internal class OtherVarIngress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly OtherVarGuestIngress _OtherVarGuestIngress;
        private readonly OtherVarHostIngress _OtherVarHostIngress;
        private readonly VarRegistry _registry;
        private readonly OtherVarRotators _OtherVarRotators;
        private readonly string _userId;

        public OtherVarIngress(
            string userId,
            VarRegistry registry,
            OtherVarRotators OtherVarRotators,
            OtherVarGuestIngress OtherVarGuestIngress,
            OtherVarHostIngress OtherVarHostIngress)
        {
            _userId = userId;
            _registry = registry;
            _OtherVarGuestIngress = OtherVarGuestIngress;
            _OtherVarHostIngress = OtherVarHostIngress;
            _OtherVarRotators = OtherVarRotators;
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
            Logger?.DebugFormat($"OtherVarIngress received sync envelope from source: {source.UserId}");

            try
            {
                var bools = OtherVarIngressContext.FromBoolValues(_userId, envelope, _registry.OtherVarRegistry, _OtherVarRotators);
                HandleSyncEnvelope(source, bools, isHost);

                var floats = OtherVarIngressContext.FromFloatValues(_userId, envelope, _registry.OtherVarRegistry, _OtherVarRotators);
                HandleSyncEnvelope(source, floats, isHost);

                var ints = OtherVarIngressContext.FromIntValues(_userId, envelope, _registry.OtherVarRegistry, _OtherVarRotators);
                HandleSyncEnvelope(source, ints, isHost);

                var strings = OtherVarIngressContext.FromStringValues(_userId, envelope, _registry.OtherVarRegistry, _OtherVarRotators);
                HandleSyncEnvelope(source, strings, isHost);
            }
            catch (Exception e)
            {
                ErrorHandler?.Invoke(new Exception($"{_userId} could not process sync envelope: {e.Message}"));
            }
        }

        private void HandleSyncEnvelope<T>(IUserPresence source, List<OtherVarIngressContext<T>> contexts, bool isHost)
        {
            Logger?.DebugFormat($"OtherVarIngress processing num contexts: {contexts.Count}");

            foreach (OtherVarIngressContext<T> context in contexts)
            {
                Logger?.DebugFormat($"OtherVarIngress processing context: {context}");

                if (isHost)
                {
                    Logger?.InfoFormat($"Setting user value for {context.Var.Presence.UserId} as host: {context.Value}");
                    _OtherVarHostIngress.HandleValue(source, context);
                }
                else
                {
                    Logger?.InfoFormat($"Setting user value for {context.Var.Presence.UserId} as guest: {context.Value}");
                    _OtherVarGuestIngress.HandleValue(context.Var, source, context.Value);
                }
            }
        }
    }
}
