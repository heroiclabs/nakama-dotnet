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
        private const int _RAND_GUEST_SEED = 1;

        public int HostIndex { get; }
        public List<SyncedMatch> Matches => _matches;
        public int NumSessions { get; }
        public int NumTestVars { get; }
        public SyncedOpcodes Opcodes { get; }

        private readonly IClient _client;
        private readonly List<SyncedMatch> _matches = new List<SyncedMatch>();
        private readonly List<ISession> _sessions = new List<ISession>();
        private readonly List<ISocket> _sockets = new List<ISocket>();
        private Dictionary<string, SyncedTestUserEnvironment> _userEnvs;
        private readonly Random _randomGuest = new Random(_RAND_GUEST_SEED);

        public SyncedTestEnvironment(
            SyncedOpcodes opcodes,
            int numClients,
            int numTestVars,
            int hostIndex,
            VarIdGenerator idGenerator = null)
        {
            Opcodes = opcodes;
            NumSessions = numClients;
            NumTestVars = numTestVars;
            HostIndex = hostIndex;

            _client = TestsUtil.FromSettingsFile();
            _sockets.AddRange(CreateSockets(_client));
            _sessions.AddRange(CreateSessions(_client));
            ConnectSockets(_sockets, _sessions);
            _userEnvs = CreateUserEnvs(_sessions, idGenerator ?? SyncedTestUserEnvironment.DefaultVarIdGenerator);
        }

        private Dictionary<string, SyncedTestUserEnvironment> CreateUserEnvs(List<ISession> sessions, VarIdGenerator generator)
        {
            var envs = new Dictionary<string, SyncedTestUserEnvironment>();

            for (int i = 0; i < sessions.Count; i++)
            {
                ISession session = sessions[i];
                envs[session.UserId] = new SyncedTestUserEnvironment(session, NumTestVars, generator);
            }

            return envs;
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

        public SyncedTestUserEnvironment GetHostEnv()
        {
            return _userEnvs[GetHostPresence().UserId];
        }

        public IUserPresence GetHostPresence()
        {
            return _matches[HostIndex].Self;
        }

        public SyncedTestUserEnvironment GetRandomGuestEnv()
        {
            List<IUserPresence> guests = GetGuests();
            int randGuestIndex = _randomGuest.Next(guests.Count);
            return _userEnvs[guests[randGuestIndex].UserId];
        }

        public SyncedTestUserEnvironment GetUserEnv(IUserPresence clientPresence)
        {
            return _userEnvs[clientPresence.UserId];
        }

        public async Task StartMatch()
        {
            if (_matches.Any())
            {
                throw new InvalidOperationException("Already started matches.");
            }

            var matchTasks = new List<Task<SyncedMatch>>();
            var opcodes = new SyncedOpcodes(Opcodes.HandshakeOpcode, Opcodes.DataOpcode);
            var registration = new SyncedVarRegistration(_sessions[HostIndex]);
            matchTasks.Add(_sockets[HostIndex].CreateSyncedMatch(opcodes, registration));

            Task.WaitAll(matchTasks.ToArray());

            for (int i = 0; i < NumSessions; i++)
            {
                if (i == HostIndex)
                {
                    continue;
                }

                matchTasks.Add(_sockets[i].JoinSyncedMatch(
                    matchTasks[0].Result.Id,
                    opcodes, new SyncedVarRegistration(_sessions[i])));
            }

            await Task.WhenAll(matchTasks.ToArray());
            _matches.AddRange(matchTasks.Select(task => task.Result));
        }

        private IEnumerable<IClient> CreateClients()
        {
            var clients = new List<IClient>();

            for (int i = 0; i < NumSessions; i++)
            {
                clients.Add(TestsUtil.FromSettingsFile());
            }

            return clients;
        }

        private IEnumerable<ISocket> CreateSockets(IClient client)
        {
            var sockets = new List<ISocket>();

            for (int i = 0; i < NumSessions; i++)
            {
                var newSocket = Nakama.Socket.From(client);
                sockets.Add(newSocket);
            }

            return sockets;
        }

        private void ConnectSockets(List<ISocket> sockets, List<ISession> sessions)
        {
            var connectTasks = new List<Task>();

            for (int i = 0; i < NumSessions; i++)
            {
                ISocket socket = sockets[i];
                connectTasks.Add(socket.ConnectAsync(sessions[i]));
            }

            Task.WaitAll(connectTasks.ToArray());
        }

        private IEnumerable<ISession> CreateSessions(IClient client)
        {
            var authTasks = new List<Task<ISession>>();

            for (int i = 0; i < NumSessions; i++)
            {
                authTasks.Add(client.AuthenticateCustomAsync($"{Guid.NewGuid()}"));
            }

            Task.WaitAll(authTasks.ToArray());

            return authTasks.Select(task => {
                return task.Result;
            });
        }

        private List<IUserPresence> GetGuests()
        {
            var guests = new List<IUserPresence>();

            for (int i = 0; i < _matches.Count; i++)
            {
                if (i == HostIndex)
                {
                    continue;
                }

                guests.Add(_matches[i].Self);
            }

            return guests;
        }
    }
}
