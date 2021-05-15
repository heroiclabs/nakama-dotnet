// Copyright 2021 The Nakama Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Nakama
{
    /// <summary>
    /// Receive status updates for users.
    /// </summary>
    public interface IStatus
    {
        /// <summary>
        /// The status events for the users followed.
        /// </summary>
        IEnumerable<IUserPresence> Presences { get; }
    }

    /// <inheritdoc cref="IStatus"/>
    internal class Status : IStatus
    {
        public IEnumerable<IUserPresence> Presences => PresencesField ?? UserPresence.NoPresences;
        [DataMember(Name="presences"), Preserve]
        public List<UserPresence> PresencesField { get; set; }

        public override string ToString()
        {
            var presences = string.Join(", ", Presences);
            return $"Status(Presences=[{presences}])";
        }
    }
}
