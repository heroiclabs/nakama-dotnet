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
    internal class PresenceVarRotators : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private SyncErrorHandler _errorHandler;
        private ILogger _logger;

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
        }

        public void Register(PresenceVarRegistry registry)
        {
            foreach (var kvp in registry.PresenceBools)
            {
                AddBool(kvp.Key, kvp.Value.SelfPresenceVar, kvp.Value.PresenceVars);
            }

            foreach (var kvp in registry.PresenceFloats)
            {
                AddFloat(kvp.Key, kvp.Value.SelfPresenceVar, kvp.Value.PresenceVars);
            }

            foreach (var kvp in registry.PresenceInts)
            {
                AddInt(kvp.Key, kvp.Value.SelfPresenceVar, kvp.Value.PresenceVars);
            }

            foreach (var kvp in registry.PresenceStrings)
            {
                AddString(kvp.Key, kvp.Value.SelfPresenceVar, kvp.Value.PresenceVars);
            }
        }

        private void AddBool(string key, SelfPresenceVar<bool> selfPresenceVar, IEnumerable<PresenceVar<bool>> presenceVars)
        {
            Add(key, selfPresenceVar, presenceVars, _boolRotators);
        }

        private void AddFloat(string key, SelfPresenceVar<float> selfPresenceVar, IEnumerable<PresenceVar<float>> presenceVars)
        {
            Add(key, selfPresenceVar, presenceVars, _floatRotators);
        }

        private void AddInt(string key, SelfPresenceVar<int> selfPresenceVar, IEnumerable<PresenceVar<int>> presenceVars)
        {
            Add(key, selfPresenceVar, presenceVars, _intRotators);
        }

        private void AddString(string key, SelfPresenceVar<string> selfPresenceVar, IEnumerable<PresenceVar<string>> presenceVars)
        {
            Add(key, selfPresenceVar, presenceVars, _stringRotators);
        }

        public void ReceiveMatch(IMatch match)
        {
            ReceiveMatch(match, _boolRotators);
            ReceiveMatch(match, _floatRotators);
            ReceiveMatch(match, _intRotators);
            ReceiveMatch(match, _stringRotators);
        }

        private void ReceiveMatch<T>(IMatch match, Dictionary<string, PresenceVarRotator<T>> rotators)
        {
            foreach (PresenceVarRotator<T> rotator in rotators.Values)
            {
                rotator.ReceiveMatch(match);
            }
        }

        private void Add<T>(
            string key,
            SelfPresenceVar<T> selfPresenceVar,
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
                rotator.AddPresenceVar(presenceVar);
                rotator.ErrorHandler = ErrorHandler;
                rotator.Logger = Logger;
            }

            rotators.Add(key, rotator);
        }
    }
}
