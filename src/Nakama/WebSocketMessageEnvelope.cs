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
    /// An envelope for messages received or sent on a <c>WebSocket</c>.
    /// </summary>
    internal class WebSocketMessageEnvelope
    {
        [DataMember(Name="cid")]
        public string Cid { get; set; }

        [DataMember(Name="channel")]
        public Channel Channel { get; set; }

        [DataMember(Name="channel_join")]
        public ChannelJoinMessage ChannelJoin { get; set; }

        [DataMember(Name="channel_leave")]
        public ChannelLeaveMessage ChannelLeave { get; set; }

        [DataMember(Name="channel_message")]
        public ApiChannelMessage ChannelMessage { get; set; }

        [DataMember(Name="channel_message_ack")]
        public ChannelMessageAck ChannelMessageAck { get; set; }

        [DataMember(Name="channel_message_remove")]
        public ChannelRemoveMessage ChannelMessageRemove { get; set; }

        [DataMember(Name="channel_message_send")]
        public ChannelSendMessage ChannelMessageSend { get; set; }

        [DataMember(Name="channel_message_update")]
        public ChannelUpdateMessage ChannelMessageUpdate { get; set; }

        [DataMember(Name="channel_presence_event")]
        public ChannelPresenceEvent ChannelPresenceEvent { get; set; }

        [DataMember(Name="error")]
        public WebSocketErrorMessage Error { get; set; }

        [DataMember(Name="matchmaker_add")]
        public MatchmakerAddMessage MatchmakerAdd { get; set; }

        [DataMember(Name="matchmaker_matched")]
        public MatchmakerMatched MatchmakerMatched { get; set; }

        [DataMember(Name="matchmaker_remove")]
        public MatchmakerRemoveMessage MatchmakerRemove { get; set; }

        [DataMember(Name="matchmaker_ticket")]
        public MatchmakerTicket MatchmakerTicket { get; set; }

        [DataMember(Name="match")]
        public Match Match { get; set; }

        [DataMember(Name="match_create")]
        public MatchCreateMessage MatchCreate { get; set; }

        [DataMember(Name="match_join")]
        public MatchJoinMessage MatchJoin { get; set; }

        [DataMember(Name="match_leave")]
        public MatchLeaveMessage MatchLeave { get; set; }

        [DataMember(Name="match_presence_event")]
        public MatchPresenceEvent MatchPresenceEvent { get; set; }

        [DataMember(Name="match_data")]
        public MatchState MatchState { get; set; }

        [DataMember(Name="match_data_send")]
        public MatchSendMessage MatchStateSend { get; set; }

        [DataMember(Name="notifications")]
        public ApiNotificationList NotificationList { get; set; }

        [DataMember(Name="rpc")]
        public ApiRpc Rpc { get; set; }

        [DataMember(Name="status")]
        public Status Status { get; set; }

        [DataMember(Name="status_follow")]
        public StatusFollowMessage StatusFollow { get; set; }

        [DataMember(Name="status_presence_event")]
        public StatusPresenceEvent StatusPresenceEvent { get; set; }

        [DataMember(Name="status_unfollow")]
        public StatusUnfollowMessage StatusUnfollow { get; set; }

        [DataMember(Name="status_update")]
        public StatusUpdateMessage StatusUpdate { get; set; }

        [DataMember(Name="stream_presence_event")]
        public StreamPresenceEvent StreamPresenceEvent { get; set; }

        [DataMember(Name="stream_data")]
        public StreamState StreamState { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return "WebSocketMessageEnvelope[]";
        }
    }
}
