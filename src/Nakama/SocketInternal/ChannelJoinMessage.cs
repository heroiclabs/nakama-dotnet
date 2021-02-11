/**
 * Copyright 2020 The Nakama Authors
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

using System.Runtime.Serialization;

namespace Nakama.SocketInternal
{
    /// <summary>
    /// Send a channel join message to the server.
    /// </summary>
    [DataContract]
    public class ChannelJoinMessage
    {
        public bool Hidden
        {
            get => _hiddenValue.HasValue ? _hiddenValue.Value : _hidden;
            set
            {
                _hidden = value;
                _hiddenValue.Value = value;
            }
        }

        public bool Persistence
        {
            get => _persistenceValue.HasValue ? _persistenceValue.Value : _persistence;
            set
            {
                _persistenceValue.Value = value;
                _persistence = value;
            }
        }

        [DataMember(Name="target", Order = 1), Preserve]
        public string Target { get; set; }

        [DataMember(Name="type", Order = 2), Preserve]
        public int Type { get; set; }

        [DataMember(Order = 4), Preserve]
        private BoolValue _hiddenValue;

        [DataMember(Name="hidden"), Preserve]
        private bool _hidden;

        [DataMember(Name="persistence"), Preserve]
        private bool _persistence;

        [DataMember(Order = 3), Preserve]
        private BoolValue _persistenceValue;

        public override string ToString()
        {
            return $"ChannelJoinMessage(Hidden={Hidden}, Persistence={Persistence}, Target='{Target}', Type={Type})";
        }
    }
}
