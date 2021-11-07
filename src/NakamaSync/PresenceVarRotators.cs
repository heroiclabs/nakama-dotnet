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
    internal delegate PresenceVarRotator<T> PresenceVarRotatorAccessor<T>(string collectionKey);

    internal class PresenceVarRotators : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private SyncErrorHandler _errorHandler;

        private Dictionary<string, PresenceVarRotator<bool>> _boolRotators;
        private Dictionary<string, PresenceVarRotator<float>> _floatRotators;
        private Dictionary<string, PresenceVarRotator<int>> _intRotators;
        private Dictionary<string, PresenceVarRotator<object>> _objectRotators;
        private Dictionary<string, PresenceVarRotator<string>> _stringRotators;
        private PresenceTracker _presenceTracker;
        private ISession _session;

        public PresenceVarRotators(PresenceTracker presenceTracker, ISession session)
        {
            _presenceTracker = presenceTracker;
            _session = session;
            _boolRotators = new Dictionary<string, PresenceVarRotator<bool>>();
            _floatRotators = new Dictionary<string, PresenceVarRotator<float>>();
            _intRotators = new Dictionary<string, PresenceVarRotator<int>>();
            _objectRotators = new Dictionary<string, PresenceVarRotator<object>>();
            _stringRotators = new Dictionary<string, PresenceVarRotator<string>>();
        }

        public void Subscribe(VarRegistry registry)
        {
            System.Console.WriteLine("SUBSCRIBING");

            foreach (var rotator in registry.PresenceBools)
            {
                Subscribe(rotator.Key, rotator.Value, _boolRotators);
            }

            foreach (var kvp in registry.PresenceFloats)
            {
                Subscribe(kvp.Key, kvp.Value, _floatRotators);
            }

            foreach (var kvp in registry.PresenceInts)
            {
                Subscribe(kvp.Key, kvp.Value, _intRotators);
            }

            foreach (var kvp in registry.PresenceObjects)
            {
                Subscribe(kvp.Key, kvp.Value, _objectRotators);
            }

            foreach (var kvp in registry.PresenceStrings)
            {
                Subscribe(kvp.Key, kvp.Value, _stringRotators);
            }
        }

        public void ReceiveMatch(IMatch match)
        {
            ReceiveMatch(match, _boolRotators);
            ReceiveMatch(match, _floatRotators);
            ReceiveMatch(match, _intRotators);
            ReceiveMatch(match, _objectRotators);
            ReceiveMatch(match, _stringRotators);
        }

        private void Subscribe<T>(
            string key,
            IEnumerable<PresenceVar<T>> presenceVars,
            Dictionary<string, PresenceVarRotator<T>> rotators)
        {
            if (rotators.ContainsKey(key))
            {
                ErrorHandler?.Invoke(new InvalidOperationException($"Tried adding duplicate key for presence var rotation: {key}"));
                return;
            }

            var rotator = new PresenceVarRotator<T>(_session);

            foreach (var presenceVar in presenceVars)
            {
                rotator.ErrorHandler = ErrorHandler;
                rotator.Logger = Logger;
                rotator.AddPresenceVar(presenceVar);
            }

            rotator.Subscribe(_presenceTracker);
            rotators.Add(key, rotator);
        }

        private void ReceiveMatch<T>(IMatch match, Dictionary<string, PresenceVarRotator<T>> rotators)
        {
            foreach (PresenceVarRotator<T> rotator in rotators.Values)
            {
                rotator.ReceiveMatch(match);
            }
        }
    }
}
