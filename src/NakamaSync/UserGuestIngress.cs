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

using Nakama;

namespace NakamaSync
{
    internal class UserGuestIngress : ISyncService
    {
        public SyncErrorHandler ErrorHandler { get; set; }
        public ILogger Logger { get; set; }

        private VarKeys _keys;
        private PresenceTracker _presenceTracker;

        public UserGuestIngress(VarKeys keys, PresenceTracker presenceTracker)
        {
            _keys = keys;
            _presenceTracker = presenceTracker;
        }

        public void HandleValue<T>(UserVar<T> var, IUserPresence source, UserValue<T> value)
        {
            var.SetValue(value.Value, source, value.TargetId, value.ValidationStatus, var.OnRemoteValueChanged);
        }
    }
}
