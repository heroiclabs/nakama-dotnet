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
    using System.Runtime.Serialization;

    /// <summary>
    /// An acknowledgement from the server when a chat message is delivered to a channel.
    /// </summary>
    public interface IChannelMessageAck
    {
        /// <summary>
        /// The server-assigned channel ID.
        /// </summary>
        string ChannelId { get; }

        /// <summary>
        /// A user-defined code for the chat message.
        /// </summary>
        int Code { get; }

        /// <summary>
        /// The UNIX time when the message was created.
        /// </summary>
        string CreateTime { get; }

        /// <summary>
        /// A unique ID for the chat message.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// True if the chat message has been stored in history.
        /// </summary>
        bool Persistent { get; }

        /// <summary>
        /// The UNIX time when the message was updated.
        /// </summary>
        string UpdateTime { get; }

        /// <summary>
        /// The username of the sender of the message.
        /// </summary>
        string Username { get; }
    }

    /// <inheritdoc />
    internal class ChannelMessageAck : IChannelMessageAck
    {
        [DataMember(Name="channel_id")]
        public string ChannelId { get; set; }

        [DataMember(Name="code")]
        public int Code { get; set; }

        [DataMember(Name="create_time")]
        public string CreateTime { get; set; }

        [DataMember(Name="message_id")]
        public string MessageId { get; set; }

        [DataMember(Name="persistent")]
        public bool Persistent { get; set; }

        [DataMember(Name="update_time")]
        public string UpdateTime { get; set; }

        [DataMember(Name="username")]
        public string Username { get; set; }

        public override string ToString()
        {
            return $"ChannelMessageAck[ChannelId={ChannelId}, Code={Code}, CreateTime={CreateTime}, MessageId={MessageId}, Persistent={Persistent}, UpdateTime={UpdateTime}, Username={Username}]";
        }
    }
}
