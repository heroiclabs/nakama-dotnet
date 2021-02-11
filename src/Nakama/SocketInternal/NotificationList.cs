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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Nakama.SocketInternal
{
    /// <inheritdoc />
    [DataContract]
    public class ApiNotificationList : IApiNotificationList
    {

        /// <inheritdoc />
        [DataMember(Name="cacheable_cursor", Order = 2), Preserve]
        public string CacheableCursor { get; set; }

        /// <inheritdoc />
        public IEnumerable<IApiNotification> Notifications => _notifications ?? new List<ApiNotification>(0);

        [DataMember(Name="notifications", Order = 1), Preserve]
        private List<ApiNotification> _notifications;

        public override string ToString()
        {
            var output = "";
            output = string.Concat(output, "CacheableCursor: ", CacheableCursor, ", ");
            output = string.Concat(output, "Notifications: [", string.Join(", ", Notifications), "], ");
            return output;
        }
    }
}

