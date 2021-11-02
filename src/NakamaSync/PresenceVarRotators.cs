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
    internal delegate OtherVarRotator<T> OtherVarRotatorAccessor<T>(string collectionKey);

    internal class OtherVarRotators : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private SyncErrorHandler _errorHandler;

        private Dictionary<string, OtherVarRotator<bool>> _boolRotators;
        private Dictionary<string, OtherVarRotator<float>> _floatRotators;
        private Dictionary<string, OtherVarRotator<int>> _intRotators;
        private Dictionary<string, OtherVarRotator<string>> _stringRotators;

        private readonly PresenceTracker _presenceTracker;

        public OtherVarRotators(PresenceTracker presenceTracker)
        {
            _boolRotators = new Dictionary<string, OtherVarRotator<bool>>();
            _floatRotators = new Dictionary<string, OtherVarRotator<float>>();
            _intRotators = new Dictionary<string, OtherVarRotator<int>>();
            _stringRotators = new Dictionary<string, OtherVarRotator<string>>();
            _presenceTracker = presenceTracker;
        }

        public void Register(OtherVarRegistry registry)
        {
            foreach (var kvp in registry.PresenceBools)
            {
                AddBool(kvp.Key, kvp.Value.SelfVar, kvp.Value.OtherVars);
            }

            foreach (var kvp in registry.PresenceFloats)
            {
                AddFloat(kvp.Key, kvp.Value.SelfVar, kvp.Value.OtherVars);
            }

            foreach (var kvp in registry.PresenceInts)
            {
                AddInt(kvp.Key, kvp.Value.SelfVar, kvp.Value.OtherVars);
            }

            foreach (var kvp in registry.PresenceStrings)
            {
                AddString(kvp.Key, kvp.Value.SelfVar, kvp.Value.OtherVars);
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

        public OtherVarRotator<bool> GetPresenceBoolRotator(string collectionKey)
        {
            return _boolRotators[collectionKey];
        }

        public OtherVarRotator<float> GetPresenceFloatRotator(string collectionKey)
        {
            return _floatRotators[collectionKey];
        }

        public OtherVarRotator<int> GetPresenceIntRotator(string collectionKey)
        {
            return _intRotators[collectionKey];
        }

        public OtherVarRotator<string> GetPresenceStringRotator(string collectionKey)
        {
            return _stringRotators[collectionKey];
        }

        private void AddBool(string key, SelfVar<bool> selfVar, IEnumerable<OtherVar<bool>> OtherVars)
        {
            Add(key, selfVar, OtherVars, _boolRotators);
        }

        private void AddFloat(string key, SelfVar<float> selfVar, IEnumerable<OtherVar<float>> OtherVars)
        {
            Add(key, selfVar, OtherVars, _floatRotators);
        }

        private void AddInt(string key, SelfVar<int> selfVar, IEnumerable<OtherVar<int>> OtherVars)
        {
            Add(key, selfVar, OtherVars, _intRotators);
        }

        private void AddString(string key, SelfVar<string> selfVar, IEnumerable<OtherVar<string>> OtherVars)
        {
            Add(key, selfVar, OtherVars, _stringRotators);
        }

        private void Add<T>(
            string key,
            SelfVar<T> selfVar,
            IEnumerable<OtherVar<T>> OtherVars,
            Dictionary<string, OtherVarRotator<T>> rotators)
        {
            if (rotators.ContainsKey(key))
            {
                ErrorHandler?.Invoke(new InvalidOperationException($"Tried adding duplicate key for presence var rotation: {key}"));
                return;
            }

            var rotator = new OtherVarRotator<T>(_presenceTracker);

            foreach (var OtherVar in OtherVars)
            {
                rotator.ErrorHandler = ErrorHandler;
                rotator.Logger = Logger;
                rotator.AddOtherVar(OtherVar);
            }

            rotators.Add(key, rotator);
        }

        private void ReceiveMatch<T>(IMatch match, Dictionary<string, OtherVarRotator<T>> rotators)
        {
            foreach (OtherVarRotator<T> rotator in rotators.Values)
            {
                rotator.ReceiveMatch(match);
            }
        }
    }
}
