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
using Xunit;
using Nakama.Replicated;

namespace Nakama.Tests.Socket
{
    public class WebSocketReplicatedTest
    {
        private const int _HANDSHAKE_OPCODE = 1;
        private const int _DATA_OPCODE = 2;

        private const int _NUM_CLIENTS = 5;
        private const int _HOST_INDEX = 0;

        private readonly List<IClient> _clients = new List<IClient>();
        private readonly List<ReplicatedMatch> _matches = new List<ReplicatedMatch>();
        private readonly List<ISession> _sessions = new List<ISession>();
        private readonly List<ISocket> _sockets = new List<ISocket>();

        private readonly ReplicatedVar<bool> _testBool = new ReplicatedVar<bool>();
        private readonly ReplicatedVar<float> _testFloat = new ReplicatedVar<float>();
        private readonly ReplicatedVar<int> _testInt = new ReplicatedVar<int>();
        private readonly ReplicatedVar<string> _testString = new ReplicatedVar<string>();

        public WebSocketReplicatedTest()
        {
            _clients.AddRange(CreateClients());
            _sockets.AddRange(CreateSockets(_clients));
            _sessions.AddRange(CreateSessions(_clients));
            ConnectSockets(_sockets, _sessions);
            _matches.AddRange(CreateMatches(_sockets, _sessions));
        }

        private void RegisterMatch(ReplicatedMatch match)
        {
            match.RegisterBool(nameof(_testBool), _testBool);
            match.RegisterFloat(nameof(_testFloat), _testFloat);
            match.RegisterInt(nameof(_testInt), _testInt);
            match.RegisterString(nameof(_testString), _testString);
        }

        private IEnumerable<IClient> CreateClients()
        {
            var clients = new List<IClient>();

            for (int i = 0; i < _NUM_CLIENTS; i++)
            {
                clients.Add(TestsUtil.FromSettingsFile());
            }

            return clients;
        }

        private IEnumerable<ReplicatedMatch> CreateMatches(List<ISocket> sockets, List<ISession> sessions)
        {
            var matchTasks = new List<Task<ReplicatedMatch>>();

            matchTasks.Add(sockets[_HANDSHAKE_OPCODE].CreateReplicatedMatch(_sessions[_HOST_INDEX], new ReplicatedOpcodes(_DATA_OPCODE, _HANDSHAKE_OPCODE)));

            Task.WaitAll(matchTasks.ToArray());

            for (int i = 0; i < _NUM_CLIENTS; i++)
            {
                if (i == _HOST_INDEX)
                {
                    continue;
                }

                matchTasks.Add(_sockets[i].JoinReplicatedMatch(_sessions[i], matchTasks[0].Result.Id, new ReplicatedOpcodes(_DATA_OPCODE, _HANDSHAKE_OPCODE)));
            }

            Task.WaitAll(matchTasks.ToArray());

            IEnumerable<ReplicatedMatch> allMatches = matchTasks.Select(task => task.Result);

            foreach (var match in allMatches)
            {
                RegisterMatch(match);
            }

            return allMatches;
        }

        private IEnumerable<ISocket> CreateSockets(List<IClient> clients)
        {
            var sockets = new List<ISocket>();

            for (int i = 0; i < _NUM_CLIENTS; i++)
            {
                var newSocket = Nakama.Socket.From(clients[i]);
                sockets.Add(newSocket);
                newSocket.ReceivedError += (e) => throw e;
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

            for (int i = 0; i < _NUM_CLIENTS; i++)
            {
                ISocket socket = sockets[i];
                connectTasks.Add(socket.ConnectAsync(sessions[i]));
            }

            Task.WaitAll(connectTasks.ToArray());
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

        [Fact]
        public async Task ReplicatedShouldShareData()
        {
            IMatch match = await _sockets[_HOST_INDEX].CreateMatchAsync();

            var joinMatchTasks = new List<Task>();

            for (int i = 0; i < _NUM_CLIENTS; i++)
            {
                if (i == _HOST_INDEX)
                {
                    continue;
                }
                else
                {
                    joinMatchTasks.Add(_sockets[i].JoinMatchAsync(match.Id));
                }
            }

            Task.WaitAll(joinMatchTasks.ToArray());
        }
    }
}
