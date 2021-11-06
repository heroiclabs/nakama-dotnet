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
    internal delegate List<ValidationAck> AckAccessor(Envelope envelope);
    internal delegate List<VarValue<T>> VarValueAccessor<T>(Envelope envelope);

    // todo this can be used to batch socket envelope sends
    // todo make this more like the rpcenvelopebuilder?
    internal class EnvelopeBuilder : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private Envelope _envelope = new Envelope();

        private SyncSocket _socket;

        public EnvelopeBuilder(SyncSocket socket)
        {
            _socket = socket;
        }

        public void AddVar<T>(VarValueAccessor<T> accessor, VarValue<T> value)
        {
            accessor(_envelope).Add(value);
        }

        public void AddAck(AckAccessor accessor, string key)
        {
            accessor(_envelope).Add(new ValidationAck(key));
        }

        public void SendEnvelope()
        {
            _socket.SendSyncDataToAll(_envelope);
            _envelope = new Envelope();
        }
    }
}
