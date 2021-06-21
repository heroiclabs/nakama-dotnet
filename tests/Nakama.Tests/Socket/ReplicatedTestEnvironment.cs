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
    public class ReplicatedTestEnvironment
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

        private readonly Dictionary<string, UserVar<bool>> _userBools = new Dictionary<string, UserVar<bool>>();
        private readonly Dictionary<string, UserVar<float>> _userFloats = new Dictionary<string, UserVar<float>>();
        private readonly Dictionary<string, UserVar<int>> _userInts = new Dictionary<string, UserVar<int>>();
        private readonly Dictionary<string, UserVar<string>> _userStrings = new Dictionary<string, UserVar<string>>();

        private readonly Dictionary<string, SharedVar<bool>> _sharedBools = new Dictionary<string, SharedVar<bool>>();
        private readonly Dictionary<string, SharedVar<float>> _sharedFloats = new Dictionary<string, SharedVar<float>>();
        private readonly Dictionary<string, SharedVar<int>> _sharedInts = new Dictionary<string, SharedVar<int>>();
        private readonly Dictionary<string, SharedVar<string>> _sharedStrings = new Dictionary<string, SharedVar<string>>();

        public ReplicatedTestEnvironment(SyncedOpcodes opcodes, int numClients, int numTestVars, int hostIndex)
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
            RegsterUserVars(_matches);
        }

        public void SetUserBool(IUserPresence clientPresence, IUserPresence targetPresence, bool value)
        {
            _userBools[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
        }

        public void SetUserFloat(IUserPresence clientPresence, IUserPresence targetPresence, float value)
        {
            _userFloats[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
        }

        public void SetUserInt(IUserPresence clientPresence, IUserPresence targetPresence, int value)
        {
            _userInts[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
        }

        public void SetUserString(IUserPresence clientPresence, IUserPresence targetPresence, string value)
        {
            _userStrings[clientPresence.UserId].SetValue(value, clientPresence, targetPresence);
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

        public UserVar<bool> GetUserBool(IUserPresence clientPresence)
        {
            return _userBools[clientPresence.UserId];
        }

        public UserVar<float> GetUserFloat(IUserPresence clientPresence, IUserPresence targetPresence, float value)
        {
            return _userFloats[clientPresence.UserId];
        }

        public UserVar<int> GetUserInt(IUserPresence clientPresence, IUserPresence targetPresence, int value)
        {
            return _userInts[clientPresence.UserId];
        }

        public UserVar<string> GetUserString(IUserPresence clientPresence, IUserPresence targetPresence, string value)
        {
            return _userStrings[clientPresence.UserId];
        }

        public SharedVar<bool> GetSharedBool(IUserPresence clientPresence)
        {
            return _sharedBools[clientPresence.UserId];
        }

        public SharedVar<float> GetSharedFloat(IUserPresence clientPresence)
        {
            return _sharedFloats[clientPresence.UserId];
        }

        public SharedVar<int> GetSharedInt(IUserPresence clientPresence)
        {
            return _sharedInts[clientPresence.UserId];
        }

        public SharedVar<string> GetSharedString(IUserPresence clientPresence)
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

        private void RegsterUserVars(List<SyncedMatch> matches)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                IUserPresence presence = matches[i].Self;

                for (int j = 0; j < NumTestVars; j++)
                {
                    matches[i].RegisterBool(presence.UserId + nameof(_userBools), new UserVar<bool>());
                    matches[i].RegisterFloat(presence.UserId + nameof(_userFloats), new UserVar<float>());
                    matches[i].RegisterInt(presence.UserId + nameof(_userInts), new UserVar<int>());
                    matches[i].RegisterString(presence.UserId + nameof(_userStrings), new UserVar<string>());
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