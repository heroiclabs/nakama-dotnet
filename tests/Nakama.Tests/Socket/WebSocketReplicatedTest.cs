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
        private readonly List<ReplicatedMatch> _replicatedMatches = new List<ReplicatedMatch>();
        private readonly List<ISession> _sessions = new List<ISession>();
        private readonly List<ISocket> _sockets = new List<ISocket>();

        private readonly ReplicatedVar<bool> _testBool = new ReplicatedVar<bool>();
        private readonly ReplicatedVar<float> _testFloat = new ReplicatedVar<float>();
        private readonly ReplicatedVar<int> _testInt = new ReplicatedVar<int>();
        private readonly ReplicatedVar<string> _testString = new ReplicatedVar<string>();

        public WebSocketReplicatedTest()
        {
            Init();
        }

        private async void Init()
        {
            for (int i = 0; i < _NUM_CLIENTS; i++)
            {
                _clients.Add(TestsUtil.FromSettingsFile());
                var newSocket = Nakama.Socket.From(_clients[i]);
                newSocket.ReceivedError += (e) => throw e;
                _sockets.Add(newSocket);
            }

            var authTasks = new List<Task<ISession>>();

            foreach (var client in _clients)
            {
                authTasks.Add(client.AuthenticateCustomAsync($"{Guid.NewGuid()}"));
            }

            Task.WaitAll(authTasks.ToArray());

            _sessions.AddRange(authTasks.Select(task => task.Result));

            var connectTasks = new List<Task>();

            for (int i = 0; i < _NUM_CLIENTS; i++)
            {
                ISocket socket = _sockets[i];
                connectTasks.Add(socket.ConnectAsync(_sessions[i]));
            }

            Task.WaitAll(connectTasks.ToArray());

            var joinMatchTasks = new List<Task<IMatch>>();

            System.Console.WriteLine("awaiting");

            var match = await _sockets[0].CreateReplicatedMatch(_sessions[0], new ReplicatedOpcodes(_DATA_OPCODE, _HANDSHAKE_OPCODE));

            for (int i = 1; i < _NUM_CLIENTS; i++)
            {
                var replicatedMatch = await _sockets[i].JoinReplicatedMatch(_sessions[0], match.Id, new ReplicatedOpcodes(_DATA_OPCODE, _HANDSHAKE_OPCODE));
                replicatedMatch.RegisterBool(nameof(_testBool), _testBool);
                replicatedMatch.RegisterFloat(nameof(_testFloat), _testFloat);
                replicatedMatch.RegisterInt(nameof(_testInt), _testInt);
                replicatedMatch.RegisterString(nameof(_testString), _testString);
                _replicatedMatches.Add(replicatedMatch);
            }

            System.Console.WriteLine("done awaiting");

            for (int i = 0; i < _NUM_CLIENTS; i++)
            {
                Task<IMatch> matchTask = _sockets[i].JoinMatchAsync(match.Id);
                joinMatchTasks.Add(matchTask);
            }

            System.Console.WriteLine("about to join");

            Task.WaitAll(joinMatchTasks.ToArray());
            System.Console.WriteLine("done joining");

            System.Console.WriteLine("done initting");
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
