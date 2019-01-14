/**
 * Copyright 2018 The Nakama Authors
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

namespace Nakama
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

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

    /// <inheritdoc />
    internal class StreamPresenceEvent : IStreamPresenceEvent
    {
        public IEnumerable<IUserPresence> Leaves => _leaves ?? new List<UserPresence>(0);
        [DataMember(Name="leaves")]
        public List<UserPresence> _leaves { get; set; }

        public IEnumerable<IUserPresence> Joins => _joins ?? new List<UserPresence>(0);
        [DataMember(Name="joins")]
        public List<UserPresence> _joins { get; set; }

        public IStream Stream => _stream;
        [DataMember(Name="stream")]
        public Stream _stream { get; set; }

        public override string ToString()
        {
            var leaves = string.Join(", ", Leaves);
            var joins = string.Join(", ", Joins);
            return $"StreamPresenceEvent(Leaves={leaves}, Joins={joins}, Stream={Stream})";
        }
    }

    /// <inheritdoc />
    internal class StreamState : IStreamState
    {
        public IUserPresence Sender => _sender;
        [DataMember(Name="sender")]
        public UserPresence _sender { get; set; }

        public string State => _state;
        [DataMember(Name="data")]
        public string _state { get; set; }

        public IStream Stream => _stream;
        [DataMember(Name="stream")]
        public Stream _stream { get; set; }

        public override string ToString()
        {
            //var state = System.Text.Encoding.UTF8.GetString(State);
            return $"StreamState(Sender={Sender}, State={State}, Stream={Stream})";
        }
    }

    /// <inheritdoc />
    internal class Stream : IStream
    {
        [DataMember(Name="descriptor")]
        public string Descriptor { get; set; }

        [DataMember(Name="label")]
        public string Label { get; set; }

        [DataMember(Name="mode")]
        public int Mode { get; set; }

        [DataMember(Name="subject")]
        public string Subject { get; set; }

        public override string ToString()
        {
            return $"Stream(Descriptor={Descriptor}, Label={Label}, Mode={Mode}, Subject={Subject})";
        }
    }
}
