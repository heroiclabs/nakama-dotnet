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
using Nakama;

namespace NakamaSync
{
    internal class UserHostEgress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private readonly VarKeys _keys;
        private readonly EnvelopeBuilder _builder;

        public UserHostEgress(VarKeys keys, EnvelopeBuilder builder)
        {
            _keys = keys;
            _builder = builder;
        }

        public void HandleLocalUserVarChanged<T>(string key, T newValue, string targetId, UserVarAccessor<T> accessor)
        {
            var status = _keys.GetValidationStatus(key);

            if (status == ValidationStatus.Pending)
            {
                ErrorHandler?.Invoke(new InvalidOperationException("Host should not have local key pending validation: " + key));
                return;
            }

            _keys.IncrementLockVersion(key);
            _builder.AddUserVar(accessor, new UserValue<T>(key, newValue, _keys.GetLockVersion(key), status, targetId));
            _builder.SendEnvelope();
        }
    }
}
