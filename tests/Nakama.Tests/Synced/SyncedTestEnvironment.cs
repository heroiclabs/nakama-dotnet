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
using NakamaSync;

namespace Nakama.Tests
{
    public class SyncedTestEnvironment
    {
        public IUserPresence Host => _matches[HostIndex].Self;
        public int HostIndex { get; }
        public int NumClients { get; }
        public int NumTestVars { get; }
        public SyncedOpcodes Opcodes { get; }

        private readonly List<IClient> _clients = new List<IClient>();
        private readonly List<SyncedMatch> _matches = new List<SyncedMatch>();
        private readonly List<ISession> _sessions = new List<ISession>();
        private readonly List<ISocket> _sockets = new List<ISocket>();
        private readonly Dictionary<string, SyncedTestUserEnvironment> _usersEnvs = new Dictionary<string, SyncedTestUserEnvironment>();

        public SyncedTestEnvironment(SyncedOpcodes opcodes, int numClients, int numTestVars, int hostIndex)
        {
            Opcodes = opcodes;
            NumClients = numClients;
            NumTestVars = numTestVars;
            HostIndex = hostIndex;

            _clients.AddRange(CreateClients());
            _sockets.AddRange(CreateSockets(_clients));
            _sessions.AddRange(CreateSessions(_clients));
            _usersEnvs = CreateTestEnvs(_matches, _sessions, numTestVars);
            ConnectSockets(_sockets, _sessions);
            _matches.AddRange(CreateMatches(_sockets, _sessions));
        }

        public SyncedTestUserEnvironment GetUserEnv(IUserPresence clientPresence)
        {
            return _usersEnvs[clientPresence.UserId];
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

        private IEnumerable<SyncedMatch> CreateMatches(List<ISocket> sockets, List<ISession> sessions)
        {
            var matchTasks = new List<Task<SyncedMatch>>();
            matchTasks.Add(sockets[HostIndex].CreateSyncedMatch(_sessions[HostIndex], new SyncedOpcodes(Opcodes.HandshakeOpcode, Opcodes.DataOpcode)));
            Task.WaitAll(matchTasks.ToArray());

            for (int i = 0; i < NumClients; i++)
            {
                if (i == HostIndex)
                {
                    continue;
                }

                matchTasks.Add(_sockets[i].JoinSyncedMatch(_sessions[i], matchTasks[0].Result.Id, new SyncedOpcodes(Opcodes.DataOpcode, Opcodes.HandshakeOpcode)));
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

        private Dictionary<string, SyncedTestUserEnvironment> CreateTestEnvs(List<SyncedMatch> matches, List<ISession> sessions, int numTestVars)
        {
            var testEnvs = new Dictionary<string, SyncedTestUserEnvironment>();

            for (int i = 0; i < matches.Count; i++)
            {
                IUserPresence presence = matches[i].Self;
                var newEnv = new SyncedTestUserEnvironment(matches[i], numTestVars);
                testEnvs.Add(presence.UserId, newEnv);
            }

            return testEnvs;
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
