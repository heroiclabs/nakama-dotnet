// Copyright 2018 The Nakama Authors
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
using static System.String;

namespace Nakama
{
    /// <summary>
    /// A batch of joins and leaves on the low level stream.
    /// </summary>
    /// <remarks>
    /// Streams are built on to provide abstractions for matches, chat channels, etc. In most cases you'll never need to
    /// interact with the low level stream itself.
    /// </remarks>
    public interface IStreamPresenceEvent
    {
        /// <summary>
        /// Presences of users who joined the stream.
        /// </summary>
        IEnumerable<IUserPresence> Leaves { get; }

        /// <summary>
        /// Presences of users who left the stream.
        /// </summary>
        IEnumerable<IUserPresence> Joins { get; }

        /// <summary>
        /// The identifier for the stream.
        /// </summary>
        IStream Stream { get; }
    }

    /// <summary>
    /// A state change received from a stream.
    /// </summary>
    public interface IStreamState
    {
        /// <summary>
        /// The user who sent the state change. May be <c>null</c>.
        /// </summary>
        IUserPresence Sender { get; }

        /// <summary>
        /// The contents of the state change.
        /// </summary>
        string State { get; }

        /// <summary>
        /// The identifier for the stream.
        /// </summary>
        IStream Stream { get; }
    }

    /// <summary>
    /// A realtime socket stream on the server.
    /// </summary>
    public interface IStream
    {
        /// <summary>
        /// The descriptor of the stream. Used with direct chat messages and contains a second user id.
        /// </summary>
        string Descriptor { get; }

        /// <summary>
        /// Identifies streams which have a context across users like a chat channel room.
        /// </summary>
        string Label { get; }

        /// <summary>
        /// The mode of the stream.
        /// </summary>
        int Mode { get; }

        /// <summary>
        /// The subject of the stream. This is usually a user id.
        /// </summary>
        string Subject { get; }
    }

    /// <inheritdoc cref="IStreamPresenceEvent"/>
    internal class StreamPresenceEvent : IStreamPresenceEvent
    {
        public IEnumerable<IUserPresence> Leaves => LeavesField ?? UserPresence.NoPresences;
        [DataMember(Name = "leaves"), Preserve] public List<UserPresence> LeavesField { get; set; }

        public IEnumerable<IUserPresence> Joins => JoinsField ?? UserPresence.NoPresences;
        [DataMember(Name = "joins"), Preserve] public List<UserPresence> JoinsField { get; set; }

        public IStream Stream => StreamField;
        [DataMember(Name = "stream"), Preserve] public Stream StreamField { get; set; }

        public override string ToString() =>
            $"StreamPresenceEvent(Leaves=[{Join(", ", Leaves)}], Joins=[{Join(", ", Joins)}], Stream={Stream})";
    }

    /// <inheritdoc cref="IStreamState"/>
    internal class StreamState : IStreamState
    {
        public IUserPresence Sender => SenderField;
        [DataMember(Name = "sender"), Preserve] public UserPresence SenderField { get; set; }

        public string State => StateField;
        [DataMember(Name = "data"), Preserve] public string StateField { get; set; }

        public IStream Stream => StreamField;
        [DataMember(Name = "stream"), Preserve] public Stream StreamField { get; set; }

        public override string ToString() => $"StreamState(Sender={Sender}, State='{StateField}', Stream={Stream})";
    }

    /// <inheritdoc cref="IStream"/>
    internal class Stream : IStream
    {
        [DataMember(Name = "descriptor"), Preserve] public string Descriptor { get; set; }

        [DataMember(Name = "label"), Preserve] public string Label { get; set; }

        [DataMember(Name = "mode"), Preserve] public int Mode { get; set; }

        [DataMember(Name = "subject"), Preserve] public string Subject { get; set; }

        public override string ToString() =>
            $"Stream(Descriptor='{Descriptor}', Label='{Label}', Mode={Mode}, Subject='{Subject}')";
    }
}
