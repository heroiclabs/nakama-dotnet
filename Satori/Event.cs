// Copyright 2022 The Satori Authors
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

using System;
using System.Collections.Generic;
using System.Xml;

namespace Satori
{
    /// <summary>
    /// An event to be published to the server.
    /// </summary>
    public class Event
    {
        /// <summary>
        /// The name of the event.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The time when the event was triggered.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Optional value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Event metadata, if any.
        /// </summary>
        public Dictionary<string, string> Metadata { get; }

        /// <summary>
        /// Optional event ID assigned by the client, used to de-duplicate in retransmission scenarios.
        /// If not supplied the server will assign a randomly generated unique event identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Optional identity id associated with the event. Ignored if the event is published as part of a session.
        /// </summary>
        public string IdentityId { get; }

        /// <summary>
        /// Optional session id associated with the event. Ignored if the event is published as part of a session.
        /// </summary>
        public string SessionId { get;  }

        /// <summary>
        /// Optional Unix epoch session issued at associated with the event. Ignored if the event is published as part of a session.
        /// </summary>
        public string SessionIssuedAt { get; }

        /// <summary>
        /// Optional Unix epoch session expires at associated with the event. Ignored if the event is published as part of a session.
        /// </summary>
        public string SessionExpiresAt { get; }

        /// <summary>
        /// The event constructor.
        /// </summary>
        /// <param name="name">The <see cref="Event.Name"/></param>
        /// <param name="timestamp">The <see cref="Event.Timestamp"/></param>
        /// <param name="value">The <see cref="Event.Value"/></param>
        /// <param name="metadata">The <see cref="Event.Metadata"/></param>
        /// <param name="id">The <see cref="Event.Id"/></param>
        /// <param name="identityId"> <see cref="Event.IdentityId"/></param>
        /// <param name="sessionId"> <see cref="Event.SessionId"/></param>
        /// <param name="sessionIssuedAt"> <see cref="Event.SessionIssuedAt"/></param>
        /// <param name="sessionExpiresAt"> <see cref="Event.SessionExpiresAt"/></param>
        public Event(string name, DateTime timestamp, string value = null, Dictionary<string, string> metadata = null,
            string id = null, string sessionId = null, string identityId = null,
            string sessionIssuedAt = null, string sessionExpiresAt = null)
        {
            Name = name;
            Timestamp = timestamp;
            Value = value;
            Metadata = metadata;
            Id = id;
            SessionId = sessionId;
            IdentityId = identityId;
            SessionIssuedAt = sessionIssuedAt;
            SessionExpiresAt = sessionExpiresAt;
        }

        internal ApiEvent ToApiEvent()
        {
            return new ApiEvent()
            {
                Id = this.Id,
                Name = this.Name,
                // Protobuf requires a DateTime string formatted as per RFC 3339
                Timestamp = XmlConvert.ToString(this.Timestamp, XmlDateTimeSerializationMode.Utc),
                Value = this.Value,
                _metadata = this.Metadata,
                IdentityId = this.IdentityId,
                SessionId = this.SessionId,
                SessionIssuedAt = this.SessionIssuedAt,
                SessionExpiresAt = this.SessionExpiresAt,
            };
        }
    }
}
