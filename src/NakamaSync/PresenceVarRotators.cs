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
    internal delegate PresenceVarRotator<T> PresenceVarRotatorAccessor<T>(string collectionKey);

    internal class PresenceVarRotators<T>
    {
        List<PresenceVarRotator<T>> _rotators;

        private PresenceTracker _presenceTracker;
        private ISession _session;

        public PresenceVarRotators(PresenceTracker presenceTracker, ISession session)
        {
            _presenceTracker = presenceTracker;
            _session = session;
        }

        public void ReceiveMatch(IMatch match)
        {
            foreach (var rotator in _rotators)
            {
                rotator.ReceiveMatch(match);
            }
        }

        public void Subscribe(VarRegistry<T> registry)
        {
            var rotator = new PresenceVarRotator<T>(_session);

            string key = null;

            foreach (var presenceVar in registry.GetPresenceVars(key))
            {
                rotator.AddPresenceVar(presenceVar);
                key = key ?? presenceVar.Key;
            }

            rotator.Subscribe(_presenceTracker);
            _rotators.Add(rotator);
        }

        private void ReceiveMatch(IMatch match, Dictionary<string, PresenceVarRotator<T>> rotators)
        {
            foreach (PresenceVarRotator<T> rotator in rotators.Values)
            {
                rotator.ReceiveMatch(match);
            }
        }
    }
}
