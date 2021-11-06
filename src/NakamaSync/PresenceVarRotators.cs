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
        private Dictionary<string, PresenceVarRotator<string>> _stringRotators;

        private readonly PresenceTracker _presenceTracker;

        public PresenceVarRotators(PresenceTracker presenceTracker)
        {
            _boolRotators = new Dictionary<string, PresenceVarRotator<bool>>();
            _floatRotators = new Dictionary<string, PresenceVarRotator<float>>();
            _intRotators = new Dictionary<string, PresenceVarRotator<int>>();
            _stringRotators = new Dictionary<string, PresenceVarRotator<string>>();
            _presenceTracker = presenceTracker;
        }

        public void Register(VarRegistry registry)
        {
            foreach (var kvp in registry.PresenceBools)
            {
                Register(kvp.Key, kvp.Value, _boolRotators);
            }

            foreach (var kvp in registry.PresenceFloats)
            {
                Register(kvp.Key, kvp.Value, _floatRotators);
            }

            foreach (var kvp in registry.PresenceInts)
            {
                Register(kvp.Key, kvp.Value, _intRotators);
            }

            foreach (var kvp in registry.PresenceStrings)
            {
                Register(kvp.Key, kvp.Value, _stringRotators);
            }
        }

        public void ReceiveMatch(IMatch match)
        {
            Logger?.DebugFormat($"Presence var rotators for {_presenceTracker.UserId} received match.");
            ReceiveMatch(match, _boolRotators);
            ReceiveMatch(match, _floatRotators);
            ReceiveMatch(match, _intRotators);
            ReceiveMatch(match, _stringRotators);
        }

        public PresenceVarRotator<bool> GetPresenceBoolRotator(string collectionKey)
        {
            return _boolRotators[collectionKey];
        }

        public PresenceVarRotator<float> GetPresenceFloatRotator(string collectionKey)
        {
            return _floatRotators[collectionKey];
        }

        public PresenceVarRotator<int> GetPresenceIntRotator(string collectionKey)
        {
            return _intRotators[collectionKey];
        }

        public PresenceVarRotator<string> GetPresenceStringRotator(string collectionKey)
        {
            return _stringRotators[collectionKey];
        }


        private void Register<T>(
            string key,
            IEnumerable<PresenceVar<T>> presenceVars,
            Dictionary<string, PresenceVarRotator<T>> rotators)
        {
            if (rotators.ContainsKey(key))
            {
                ErrorHandler?.Invoke(new InvalidOperationException($"Tried adding duplicate key for presence var rotation: {key}"));
                return;
            }

            var rotator = new PresenceVarRotator<T>(_presenceTracker);

            foreach (var presenceVar in presenceVars)
            {
                rotator.ErrorHandler = ErrorHandler;
                rotator.Logger = Logger;
                rotator.AddPresenceVar(presenceVar);
            }

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
