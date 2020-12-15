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
    [DataContract]
    public class StreamPresenceEvent : IStreamPresenceEvent
    {
        public IEnumerable<IUserPresence> Leaves => _leaves ?? UserPresence.NoPresences;
        [DataMember(Name = "leaves", Order = 1), Preserve] public List<UserPresence> _leaves { get; set; }

        public IEnumerable<IUserPresence> Joins => _joins ?? UserPresence.NoPresences;
        [DataMember(Name = "joins", Order = 2), Preserve] public List<UserPresence> _joins { get; set; }

        public IStream Stream => _stream;
        [DataMember(Name = "stream", Order = 3), Preserve] public Stream _stream { get; set; }

        public override string ToString()
        {
            var leaves = string.Join(", ", Leaves);
            var joins = string.Join(", ", Joins);
            return $"StreamPresenceEvent(Leaves=[{leaves}], Joins=[{joins}], Stream={Stream})";
        }
    }

    /// <inheritdoc cref="IStreamState"/>
    [DataContract]
    public class StreamState : IStreamState
    {
        public IUserPresence Sender => _sender;
        [DataMember(Name = "sender", Order = 1), Preserve] public UserPresence _sender { get; set; }

        public string State => _state;
        [DataMember(Name = "data", Order = 2), Preserve] public string _state { get; set; }

        public IStream Stream => _stream;
        [DataMember(Name = "stream", Order = 3), Preserve] public Stream _stream { get; set; }

        public override string ToString()
        {
            return $"StreamState(Sender={Sender}, State='{_state}', Stream={Stream})";
        }
    }

    /// <inheritdoc cref="IStream"/>
    [DataContract]
    public class Stream : IStream
    {
        [DataMember(Name = "descriptor", Order = 1), Preserve] public string Descriptor { get; set; }

        [DataMember(Name = "label", Order = 2), Preserve] public string Label { get; set; }

        [DataMember(Name = "mode", Order = 3), Preserve] public int Mode { get; set; }

        [DataMember(Name = "subject", Order = 4), Preserve] public string Subject { get; set; }

        public override string ToString()
        {
            return $"Stream(Descriptor='{Descriptor}', Label='{Label}', Mode={Mode}, Subject='{Subject}')";
        }
    }
}

