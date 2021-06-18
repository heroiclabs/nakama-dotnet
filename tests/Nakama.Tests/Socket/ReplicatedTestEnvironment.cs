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

        private readonly Dictionary<string, Owned<bool>> _ownedBools = new Dictionary<string, Owned<bool>>();
        private readonly Dictionary<string, Owned<float>> _ownedFloats = new Dictionary<string, Owned<float>>();
        private readonly Dictionary<string, Owned<int>> _ownedInts = new Dictionary<string, Owned<int>>();
        private readonly Dictionary<string, Owned<string>> _ownedStrings = new Dictionary<string, Owned<string>>();

        private readonly Dictionary<string, Shared<bool>> _sharedBools = new Dictionary<string, Shared<bool>>();
        private readonly Dictionary<string, Shared<float>> _sharedFloats = new Dictionary<string, Shared<float>>();
        private readonly Dictionary<string, Shared<int>> _sharedInts = new Dictionary<string, Shared<int>>();
        private readonly Dictionary<string, Shared<string>> _sharedStrings = new Dictionary<string, Shared<string>>();

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

        public void SetOwnedBool(IUserPresence clientPresence, IUserPresence targetPresence, bool value)
        {
            _ownedBools[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
        }

        public void SetOwnedFloat(IUserPresence clientPresence, IUserPresence targetPresence, float value)
        {
            _ownedFloats[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
        }

        public void SetOwnedInt(IUserPresence clientPresence, IUserPresence targetPresence, int value)
        {
            _ownedInts[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
        }

        public void SetOwnedString(IUserPresence clientPresence, IUserPresence targetPresence, string value)
        {
            _ownedStrings[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
        }

        public void SetSharedBool(IUserPresence clientPresence, bool value)
        {
            _sharedBools[clientPresence.UserId].SetValue(value);
        }

        public void SetSharedFloat(IUserPresence clientPresence, float value)
        {
            _sharedFloats[clientPresence.UserId].SetValue(value);
        }

        public void SetSharedInt(IUserPresence clientPresence, int value)
        {
            _sharedInts[clientPresence.UserId].SetValue(value);
        }

        public void SetSharedString(IUserPresence clientPresence, string value)
        {
            _sharedStrings[clientPresence.UserId].SetValue(value);
        }

        public Owned<bool> GetOwnedBool(IUserPresence clientPresence)
        {
            return _ownedBools[clientPresence.UserId];
        }

        public Owned<float> GetOwnedFloat(IUserPresence clientPresence, IUserPresence targetPresence, float value)
        {
            return _ownedFloats[clientPresence.UserId];
        }

        public Owned<int> GetOwnedInt(IUserPresence clientPresence, IUserPresence targetPresence, int value)
        {
            return _ownedInts[clientPresence.UserId];
        }

        public Owned<string> GetOwnedString(IUserPresence clientPresence, IUserPresence targetPresence, string value)
        {
            return _ownedStrings[clientPresence.UserId];
        }

        public Shared<bool> GetSharedBool(IUserPresence clientPresence)
        {
            return _sharedBools[clientPresence.UserId];
        }

        public Shared<float> GetSharedFloat(IUserPresence clientPresence)
        {
            return _sharedFloats[clientPresence.UserId];
        }

        public Shared<int> GetSharedInt(IUserPresence clientPresence)
        {
            return _sharedInts[clientPresence.UserId];
        }

        public Shared<string> GetSharedString(IUserPresence clientPresence)
        {
            return _sharedStrings[clientPresence.UserId];
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
                    matches[i].RegisterBool(presence.UserId + nameof(_ownedBools), new Owned<bool>());
                    matches[i].RegisterFloat(presence.UserId + nameof(_ownedFloats), new Owned<float>());
                    matches[i].RegisterInt(presence.UserId + nameof(_ownedInts), new Owned<int>());
                    matches[i].RegisterString(presence.UserId + nameof(_ownedStrings), new Owned<string>());
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