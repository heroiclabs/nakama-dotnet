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

using System.Collections.Generic;

namespace Nakama.Examples
{
    public class PartyExamples
    {
        private ISocket socket;
        private IUserPresence partyMember = default(IUserPresence);

        private async void CreateOpenParty()
        {
            IParty party = await socket.CreatePartyAsync(open: true, maxSize: 2);
            System.Console.WriteLine(party);
        }

        private async void CreateClosedParty()
        {
            IParty party = await socket.CreatePartyAsync(open: false, maxSize: 2);

            socket.ReceivedPartyJoinRequest += async request =>
            {
                foreach (IUserPresence presence in request.Presences)
                {
                    if (presence.UserId == "<acceptid>")
                    {
                        await socket.AcceptPartyMemberAsync(party.Id, presence);
                    }
                }
            };
        }

        private async void JoinParty()
        {
            string partyId = "<partyid>";
            await socket.JoinPartyAsync(partyId);

            socket.ReceivedParty += party =>
            {
                System.Console.WriteLine("Joined party: " + party);
            };
        }

        private async void ListPartyJoinRequests()
        {
            string partyId = "<partyid>";

            IPartyJoinRequest requests = await socket.ListPartyJoinRequestsAsync(partyId);

            foreach (IUserPresence presence in requests.Presences)
            {
                await socket.AcceptPartyMemberAsync(partyId, presence);
            }
        }

        private async void CloseParty()
        {
            string partyId = "<partyid>";
            await socket.ClosePartyAsync(partyId);
        }

        private async void RemovedFromParty()
        {
            socket.ReceivedPartyClose += close =>
            {
                // user removed.
            };
        }

        private async void TrackMembers()
        {
            var partyMembers = new Dictionary<string, IUserPresence>();

            socket.ReceivedPartyPresence += presence =>
            {
                foreach (IUserPresence joiningUser in presence.Joins)
                {
                    partyMembers.Add(joiningUser.UserId, joiningUser);
                }

                foreach (IUserPresence leavingUser in presence.Leaves)
                {
                    partyMembers.Remove(leavingUser.UserId);
                }
            };
        }

        private async void PromoteToLeader()
        {
            socket.ReceivedPartyLeader += newLeader =>
            {
                System.Console.WriteLine("new party leader " + newLeader);
            };

            await socket.PromotePartyMemberAsync("<partyid>", partyMember);
        }

        private async void RemovePartyMember()
        {
            socket.ReceivedPartyPresence += presence =>
            {
                foreach (IUserPresence leavingUser in presence.Leaves)
                {
                    System.Console.WriteLine("removed party member: " + presence);
                }
            };

            await socket.RemovePartyMemberAsync("<partyid>", partyMember);
        }

        private async void SendPartyData()
        {
            await socket.SendPartyDataAsync(partyId: "<partyid>", opCode: 1, data: System.Text.Encoding.UTF8.GetBytes("{\"hello\": \"world\"}"));
        }

        private async void ReceivePartyData()
        {
            socket.ReceivedPartyData += data =>
            {
                System.Console.WriteLine("received data " + data.Data);
            };
        }

        private async void MatchmakeAsParty()
        {
            IMatchmakerMatched matched;

            socket.ReceivedMatchmakerMatched += matchedAsParty =>
            {
                matched = matchedAsParty;
                socket.JoinMatchAsync(matched);
            };

            await socket.AddMatchmakerPartyAsync(partyId: "<partyid>", query: "*", minCount: 2, maxCount: 6);
        }

        private async void RemoveFromMatchmakeAsParty()
        {
            IPartyMatchmakerTicket ticket = await socket.AddMatchmakerPartyAsync(partyId: "<partyid>", query: "*", minCount: 2, maxCount: 6);
            await socket.RemoveMatchmakerPartyAsync(ticket.PartyId, ticket.Ticket);
        }
    }
}
