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

using System.Threading.Tasks;

namespace Nakama.Replicated
{
    public static class ReplicatedExtensions
    {
        // todo don't require session as a parameter here since we pass it to socket.
        public async static Task<ReplicatedMatch> CreateReplicatedMatch(this ISocket socket, ISession session, ReplicatedOpcodes opcodes)
        {
            var varStore = new ReplicatedVarStore();
            var presenceTracker = new ReplicatedPresenceTracker(session, varStore);
            socket.ReceivedMatchPresence += presenceTracker.HandlePresenceEvent;

            IMatch match = await socket.CreateMatchAsync();

            var replicatedSocket = new ReplicatedSocket(match.Id, opcodes, presenceTracker, socket, varStore);

            presenceTracker.OnReplicatedGuestJoined += replicatedSocket.HandleGuestJoined;
            presenceTracker.OnReplicatedGuestLeft += replicatedSocket.HandleGuestLeft;
            presenceTracker.OnReplicatedHostChanged += replicatedSocket.HandleHostChanged;

            return new ReplicatedMatch(match, varStore, presenceTracker);
        }

        public async static Task<ReplicatedMatch> JoinReplicatedMatch(this ISocket socket, ISession session, string matchId, ReplicatedOpcodes opcodes)
        {
            var varStore = new ReplicatedVarStore();
            var presenceTracker = new ReplicatedPresenceTracker(session, varStore);
            socket.ReceivedMatchPresence += presenceTracker.HandlePresenceEvent;

            var replicatedSocket = new ReplicatedSocket(matchId, opcodes, presenceTracker, socket, varStore);

            presenceTracker.OnReplicatedGuestJoined += replicatedSocket.HandleGuestJoined;
            presenceTracker.OnReplicatedGuestLeft += replicatedSocket.HandleGuestLeft;
            presenceTracker.OnReplicatedHostChanged += replicatedSocket.HandleHostChanged;

            IMatch match = await socket.JoinMatchAsync(matchId);
            return new ReplicatedMatch(match, varStore, presenceTracker);
        }
    }
}