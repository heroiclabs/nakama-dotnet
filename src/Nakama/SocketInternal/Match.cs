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
    /// <inheritdoc cref="IMatch"/>
    [DataContract]
    public class Match : IMatch
    {
        [DataMember(Name = "authoritative", Order = 2), Preserve] public bool Authoritative { get; set; }

        [DataMember(Name = "match_id", Order = 1), Preserve] public string Id { get; set; }

        [DataMember(Name = "label", Order = 3), Preserve] public string Label { get; set; }

        public IEnumerable<IUserPresence> Presences => _presences ?? UserPresence.NoPresences;
        [DataMember(Name = "presences", Order = 5), Preserve] public List<UserPresence> _presences { get; set; }

        [DataMember(Name = "size", Order = 4), Preserve] public int Size { get; set; }

        public IUserPresence Self => _self;
        [DataMember(Name = "self", Order = 6), Preserve] public UserPresence _self { get; set; }

        public override string ToString()
        {
            var presences = string.Join(", ", Presences);
            return
                $"Match(Authoritative={Authoritative}, Id='{Id}', Label='{Label}', Presences=[{presences}], Size={Size}, Self={Self})";
        }
    }
}
