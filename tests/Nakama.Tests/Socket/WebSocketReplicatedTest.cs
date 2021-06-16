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

namespace Nakama.Tests.Socket
{
    public class WebSocketReplicatedTest
    {
        private const int _NUM_CLIENTS = 5;
        private const int _HOST_INDEX = 0;

        private readonly List<IClient> _clients = new List<IClient>();
        private readonly List<ISocket> _sockets = new List<ISocket>();
        private readonly List<ISession> _sessions = new List<ISession>();
        private IMatch match = null;

        public WebSocketReplicatedTest()
        {
            for (int i = 0; i < _NUM_CLIENTS; i++)
            {
                _clients.Add(TestsUtil.FromSettingsFile());
                _sockets.Add(Nakama.Socket.From(_clients[i]));
            }

            var authTasks = new List<Task<ISession>>();

            foreach (var client in _clients)
            {
                client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            }

            Task.WaitAll(authTasks.ToArray());

            _sessions.AddRange(authTasks.Select(task => task.Result));

            var connectTasks = new List<Task>();

            for (int i = 0; i < _sockets.Count; i++)
            {
                ISocket socket = _sockets[i];
                connectTasks.Add(socket.ConnectAsync(_sessions[i]));
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
