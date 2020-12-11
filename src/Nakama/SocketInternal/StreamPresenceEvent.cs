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
using System.Collections.Generic;

namespace Nakama.SocketInternal
{
    /// <inheritdoc cref="IStreamPresenceEvent"/>
    public class StreamPresenceEvent : IStreamPresenceEvent
    {
        public IEnumerable<IUserPresence> Leaves => _leaves ?? UserPresence.NoPresences;
        [DataMember(Name = "leaves"), Preserve] public List<UserPresence> _leaves { get; set; }

        public IEnumerable<IUserPresence> Joins => _joins ?? UserPresence.NoPresences;
        [DataMember(Name = "joins"), Preserve] public List<UserPresence> _joins { get; set; }

        public IStream Stream => _stream;
        [DataMember(Name = "stream"), Preserve] public Stream _stream { get; set; }

        public override string ToString()
        {
            var leaves = string.Join(", ", Leaves);
            var joins = string.Join(", ", Joins);
            return $"StreamPresenceEvent(Leaves=[{leaves}], Joins=[{joins}], Stream={Stream})";
        }
    }

    /// <inheritdoc cref="IStreamState"/>
    public class StreamState : IStreamState
    {
        public IUserPresence Sender => _sender;
        [DataMember(Name = "sender"), Preserve] public UserPresence _sender { get; set; }

        public string State => _state;
        [DataMember(Name = "data"), Preserve] public string _state { get; set; }

        public IStream Stream => _stream;
        [DataMember(Name = "stream"), Preserve] public Stream _stream { get; set; }

        public override string ToString()
        {
            return $"StreamState(Sender={Sender}, State='{_state}', Stream={Stream})";
        }
    }

    /// <inheritdoc cref="IStream"/>
    public class Stream : IStream
    {
        [DataMember(Name = "descriptor"), Preserve] public string Descriptor { get; set; }

        [DataMember(Name = "label"), Preserve] public string Label { get; set; }

        [DataMember(Name = "mode"), Preserve] public int Mode { get; set; }

        [DataMember(Name = "subject"), Preserve] public string Subject { get; set; }

        public override string ToString()
        {
            return $"Stream(Descriptor='{Descriptor}', Label='{Label}', Mode={Mode}, Subject='{Subject}')";
        }
    }
}

