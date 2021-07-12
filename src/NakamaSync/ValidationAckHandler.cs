
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
    internal class ValidationAckHandler
    {
        private SyncVarRegistry _registry;

        public ValidationAckHandler(SyncVarRegistry registry)
        {
            _registry = registry;
        }

        public void HandleSyncEnvelope(IUserPresence source, Envelope envelope)
        {

            HandleAcks(envelope.SharedBoolAcks, _registry.SharedBools);
            HandleAcks(envelope.SharedFloatAcks, _registry.SharedFloats);
            HandleAcks(envelope.SharedIntAcks, _registry.SharedInts);
            HandleAcks(envelope.SharedStringAcks, _registry.SharedStrings);
        }

        private void HandleAcks<TVar>(List<ValidationAck> acks, Dictionary<string, TVar> vars) where TVar : IVar
        {
            foreach (ValidationAck ack in acks)
            {
                // todo handle no key.
                vars[ack.Key].SetValidationStatus(KeyValidationStatus.Validated);
            }
        }
    }
}