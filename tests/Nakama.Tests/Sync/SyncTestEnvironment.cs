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
    public class SyncTestEnvironment
    {
        private const int _RAND_GUEST_SEED = 1;

        public int HostIndex { get; }
        public List<IMatch> Matches => _matches;
        public int NumSessions { get; }
        public int NumTestVars { get; }
        public SyncOpcodes Opcodes { get; }

        private readonly IClient _client;
        private readonly List<IMatch> _matches = new List<IMatch>();
        private readonly List<VarRegistry> _registrations = new List<VarRegistry>();
        private readonly List<ISession> _sessions = new List<ISession>();
        private readonly List<ISocket> _sockets = new List<ISocket>();
        private Dictionary<string, SyncTestUserEnvironment> _userEnvs;
        private readonly Random _randomGuest = new Random(_RAND_GUEST_SEED);

        public SyncTestEnvironment(
            SyncOpcodes opcodes,
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
            _sessions.AddRange(CreateSessions(_client));
            _registrations.AddRange(CreateRegistrations(_sessions));
            _sockets.AddRange(CreateSockets(_client));
            ConnectSockets(_sockets, _sessions);
            _userEnvs = CreateUserEnvs(_sessions, _registrations, idGenerator ?? SyncTestUserEnvironment.DefaultVarIdGenerator);
        }

        private Dictionary<string, SyncTestUserEnvironment> CreateUserEnvs(List<ISession> sessions, List<VarRegistry> registrations, VarIdGenerator generator)
        {
            var envs = new Dictionary<string, SyncTestUserEnvironment>();

            for (int i = 0; i < sessions.Count; i++)
            {
                ISession session = sessions[i];
                envs[session.UserId] = new SyncTestUserEnvironment(session, _registrations[i], NumTestVars, generator);
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

        public SyncTestUserEnvironment GetHostEnv()
        {
            return _userEnvs[GetHostPresence().UserId];
        }

        public IUserPresence GetHostPresence()
        {
            return _matches[HostIndex].Self;
        }

        public SyncTestUserEnvironment GetRandomGuestEnv()
        {
            List<IUserPresence> guests = GetGuests();
            int randGuestIndex = _randomGuest.Next(guests.Count);
            return _userEnvs[guests[randGuestIndex].UserId];
        }

        public SyncTestUserEnvironment GetUserEnv(IUserPresence clientPresence)
        {
            return _userEnvs[clientPresence.UserId];
        }

        public async Task StartMatch()
        {
            if (_matches.Any())
            {
                throw new InvalidOperationException("Already started matches.");
            }

            var opcodes = new SyncOpcodes(Opcodes.HandshakeRequestOpcode, Opcodes.HandshakeResponseOpcode, Opcodes.DataOpcode);

            var createTask = _sockets[HostIndex].CreateSyncMatch(_sessions[HostIndex], opcodes, _registrations[HostIndex]);
            await createTask;

            var joinTasks = new List<Task<IMatch>>();

            for (int i = 0; i < NumSessions; i++)
            {
                if (i == HostIndex)
                {
                    continue;
                }

                var registration = _registrations[i];
                var socket = _sockets[i];
                var matchTask = socket.JoinSyncMatch(_sessions[i], opcodes, createTask.Result.Id, registration);
                joinTasks.Add(matchTask);
            }

            await Task.WhenAll(joinTasks.ToArray());
            _matches.Add(createTask.Result);
            _matches.AddRange(joinTasks.Select(task => task.Result));
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

        private IEnumerable<VarRegistry> CreateRegistrations(List<ISession> sessions)
        {
            var registries = new List<VarRegistry>();

            for (int i = 0; i < sessions.Count; i++)
            {
                var registry = new VarRegistry();
                registries.Insert(i, registry);
            }

            return registries;
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
