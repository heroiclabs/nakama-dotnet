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
    internal class HostEgress
    {
        private readonly VarKeys _keys;
        private readonly EnvelopeBuilder _builder;
        private readonly RolePresenceTracker _presenceTracker;

        public HostEgress(VarKeys keys, EnvelopeBuilder builder, RolePresenceTracker presenceTracker)
        {
            _keys = keys;
            _builder = builder;
            _presenceTracker = presenceTracker;
        }

        public void HandleLocalSharedVarChanged<T>(string key, T newValue, SharedVarAccessor<T> accessor)
        {
            var status = _keys.GetValidationStatus(key);

            if (status == KeyValidationStatus.Pending)
            {
                throw new InvalidOperationException("Host should not have local key pending validation: " + key);
            }

            var sharedValue = new SharedValue<T>(key, newValue, _keys.GetLockVersion(key), status);
            _builder.AddSharedVar(accessor, sharedValue);
            _builder.SendEnvelope();
        }

        public void HandleLocalUserVarChanged<T>(string key, T newValue, IUserPresence target, UserVarAccessor<T> accessor)
        {
            var status = _keys.GetValidationStatus(key);

            if (status == KeyValidationStatus.Pending)
            {
                throw new InvalidOperationException("Host should not have local key pending validation: " + key);
            }

            _builder.AddUserVar(accessor, new UserValue<T>(key, newValue, _keys.GetLockVersion(key), status, target));
            _builder.SendEnvelope();
        }
    }
}
