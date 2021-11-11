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
    internal delegate PresenceVarRotator PresenceVarRotatorAccessor(string collectionKey);

    internal class PresenceVarRotators
    {
        private readonly List<PresenceVarRotator> _rotators = new List<PresenceVarRotator>();

        private PresenceTracker _presenceTracker;
        private ISession _session;
        private VarRegistry _varRegistry;

        public PresenceVarRotators(PresenceTracker presenceTracker, ISession session, VarRegistry registry)
        {
            _presenceTracker = presenceTracker;
            _session = session;
            _varRegistry = registry;
        }

        public void HandlePresencesAdded(IEnumerable<IUserPresence> presences)
        {
            foreach (var rotator in _rotators)
            {
                rotator.HandlePresencesAdded(presences);
            }
        }

        public void Subscribe()
        {
            var rotator = new PresenceVarRotator(_session);

            string key = null;

            foreach (var presenceVar in _varRegistry.GetAllRotatables())
            {
                rotator.AddPresenceVar(presenceVar);
                key = key ?? presenceVar.Key;
            }

            rotator.Subscribe(_presenceTracker);
            _rotators.Add(rotator);
        }
    }
}
