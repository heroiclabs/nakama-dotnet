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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nakama.Replicated;

namespace Nakama.Tests
{
    public class ReplicatedTestEnvironment
    {
        public IUserPresence Host => _matches[HostIndex].Self;
        public int HostIndex { get; }
        public int NumClients { get; }
        public int NumTestVars { get; }
        public ReplicatedOpcodes Opcodes { get; }

        private readonly List<IClient> _clients = new List<IClient>();
        private readonly List<ReplicatedMatch> _matches = new List<ReplicatedMatch>();
        private readonly List<ISession> _sessions = new List<ISession>();
        private readonly List<ISocket> _sockets = new List<ISocket>();

        private readonly Dictionary<string, Owned<bool>> _testBools = new Dictionary<string, Owned<bool>>();
        private readonly Dictionary<string, Owned<float>> _testFloats = new Dictionary<string, Owned<float>>();
        private readonly Dictionary<string, Owned<int>> _testInts = new Dictionary<string, Owned<int>>();
        private readonly Dictionary<string, Owned<string>> _testStrings = new Dictionary<string, Owned<string>>();

        public ReplicatedTestEnvironment(ReplicatedOpcodes opcodes, int numClients, int numTestVars, int hostIndex)
        {
            Opcodes = opcodes;
            NumClients = numClients;
            NumTestVars = numTestVars;
            HostIndex = hostIndex;

            _clients.AddRange(CreateClients());
            _sockets.AddRange(CreateSockets(_clients));
            _sessions.AddRange(CreateSessions(_clients));
            ConnectSockets(_sockets, _sessions);
            _matches.AddRange(CreateMatches(_sockets, _sessions));
            RegsterOwnedVars(_matches);
        }

        public void SetValue(IUserPresence clientPresence, IUserPresence targetPresence, bool value)
        {
            _testBools[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
        }

        public void SetValue(IUserPresence clientPresence, IUserPresence targetPresence, float value)
        {
            _testFloats[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
        }

        public void SetValue(IUserPresence clientPresence, IUserPresence targetPresence, int value)
        {
            _testInts[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
        }

        public void SetValue(IUserPresence clientPresence, IUserPresence targetPresence, string value)
        {
            _testStrings[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
        }

        private IEnumerable<IClient> CreateClients()
        {
            var clients = new List<IClient>();

            for (int i = 0; i < NumClients; i++)
            {
                clients.Add(TestsUtil.FromSettingsFile());
            }

            return clients;
        }

        private IEnumerable<ReplicatedMatch> CreateMatches(List<ISocket> sockets, List<ISession> sessions)
        {
            var matchTasks = new List<Task<ReplicatedMatch>>();
            matchTasks.Add(sockets[HostIndex].CreateReplicatedMatch(_sessions[HostIndex], new ReplicatedOpcodes(Opcodes.HandshakeOpcode, Opcodes.DataOpcode)));
            Task.WaitAll(matchTasks.ToArray());

            for (int i = 0; i < NumClients; i++)
            {
                if (i == HostIndex)
                {
                    continue;
                }

                matchTasks.Add(_sockets[i].JoinReplicatedMatch(_sessions[i], matchTasks[0].Result.Id, new ReplicatedOpcodes(Opcodes.DataOpcode, Opcodes.HandshakeOpcode)));
            }

            Task.WaitAll(matchTasks.ToArray());

            return matchTasks.Select(task => task.Result);
        }

        private IEnumerable<ISocket> CreateSockets(List<IClient> clients)
        {
            var sockets = new List<ISocket>();

            for (int i = 0; i < NumClients; i++)
            {
                var newSocket = Nakama.Socket.From(clients[i]);
                sockets.Add(newSocket);
            }

            return sockets;
        }

        private IEnumerable<ISession> CreateSessions(IEnumerable<IClient> clients)
        {
            var authTasks = new List<Task<ISession>>();

            foreach (var client in _clients)
            {
                authTasks.Add(client.AuthenticateCustomAsync($"{Guid.NewGuid()}"));
            }

            Task.WaitAll(authTasks.ToArray());

            return authTasks.Select(task => task.Result);
        }

        private void ConnectSockets(List<ISocket> sockets, List<ISession> sessions)
        {
            var connectTasks = new List<Task>();

            for (int i = 0; i < NumClients; i++)
            {
                ISocket socket = sockets[i];
                connectTasks.Add(socket.ConnectAsync(sessions[i]));
            }

            Task.WaitAll(connectTasks.ToArray());
        }

        private void RegsterOwnedVars(List<ReplicatedMatch> matches)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                IUserPresence presence = matches[i].Self;

                for (int j = 0; j < NumTestVars; j++)
                {
                    System.Console.WriteLine(presence.UserId + nameof(_testBools));
                    matches[i].RegisterBool(presence.UserId + nameof(_testBools), new Owned<bool>());
                    matches[i].RegisterFloat(presence.UserId + nameof(_testFloats), new Owned<float>());
                    matches[i].RegisterInt(presence.UserId + nameof(_testInts), new Owned<int>());
                    matches[i].RegisterString(presence.UserId + nameof(_testStrings), new Owned<string>());
                }
            }
        }

        public void Dispose()
        {
            var closeTasks = new List<Task>();

            foreach (ISocket socket in _sockets)
            {
                closeTasks.Add(socket.CloseAsync());
            }

            Task.WaitAll(closeTasks.ToArray());
        }
    }
}