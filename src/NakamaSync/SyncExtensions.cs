/**
* Copyright 2021 The Nakama Authors
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

// todo think about fact that user can still attach to presence handler without being able to sequence presence events as they see fit.
// todo potential race when creating and joining a match between the construction of this object
// and the dispatching of presence objects off the socket.

using System.Threading.Tasks;
using Nakama;

namespace NakamaSync
{
    public static class SyncExtensions
    {
        // todo maybe don't require session as a parameter here since we pass it to socket.
        public static async Task<ISyncMatch> CreateSyncMatch(this ISocket socket, ISession session, VarRegistry varRegistry, RpcRegistry rpcRegistry, string name = null)
        {
            // TODO think about how to properly deregister, if at all.
            socket.ReceivedMatchState += varRegistry.HandleReceivedMatchState;
            var trackers = new SyncTrackers(session, varRegistry, socket);
            IMatch match = await socket.CreateMatchAsync(name);
            trackers.PresenceTracker.ReceiveMatch(match);
            var syncMatch = new SyncMatch(socket, session, match, varRegistry, rpcRegistry, trackers);
            await varRegistry.ReceiveMatch(syncMatch);
            trackers.HostTracker.ReceiveMatch(match);
            return syncMatch;
        }

        public static async Task<ISyncMatch> JoinSyncMatch(this ISocket socket, ISession session, IMatchmakerMatched matched, VarRegistry varRegistry, RpcRegistry rpcRegistry)
        {
            // TODO think about how to properly deregister, if at all.
            socket.ReceivedMatchState += varRegistry.HandleReceivedMatchState;
            var trackers = new SyncTrackers(session, varRegistry, socket);
            IMatch match = await socket.JoinMatchAsync(matched);
            trackers.PresenceTracker.ReceiveMatch(match);
            var syncMatch = new SyncMatch(socket, session, match, varRegistry, rpcRegistry, trackers);
            await varRegistry.ReceiveMatch(syncMatch);
            trackers.HostTracker.ReceiveMatch(match);
            return syncMatch;
        }

        public static async Task<ISyncMatch> JoinSyncMatch(this ISocket socket, ISession session, string matchId, VarRegistry varRegistry, RpcRegistry rpcRegistry)
        {
            // TODO think about how to properly deregister, if at all.
            socket.ReceivedMatchState += varRegistry.HandleReceivedMatchState;
            var trackers = new SyncTrackers(session, varRegistry, socket);
            IMatch match = await socket.JoinMatchAsync(matchId);
            trackers.PresenceTracker.ReceiveMatch(match);
            var syncMatch = new SyncMatch(socket, session, match, varRegistry, rpcRegistry, trackers);
            await varRegistry.ReceiveMatch(syncMatch);
            return syncMatch;
        }
    }
}
