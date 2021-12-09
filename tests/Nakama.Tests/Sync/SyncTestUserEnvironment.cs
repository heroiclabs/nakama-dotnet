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

using NakamaSync;
using System.Threading.Tasks;

namespace Nakama.Tests.Sync
{
    /// <summary>
    // A test environment representing a single user's view of a sync match.
    /// </summary>
    public class SyncTestUserEnvironment
    {
        public IUserPresence Self => _match.Self;
        public ISession Session => _session;
        public SyncTestSharedVars SharedVars => _sharedVars;
        public SyncTestGroupVars GroupVars => _groupVars;
        public SyncTestRpcs Rpcs => _rpcs;
        public ISyncMatch Match => _match;
        public VarRegistry VarRegistry => _varRegistry;
        public ISocket Socket => _socket;

        private readonly string _userId;
        private readonly IClient _client;
        private readonly VarRegistry _varRegistry = new VarRegistry();
        private readonly RpcRegistry _rpcRegistry = new RpcRegistry(opcode: -2);
        private SyncTestSharedVars _sharedVars;
        private SyncTestGroupVars _groupVars;
        private SyncTestRpcs _rpcs;
        private readonly ILogger _logger;

        private ISyncMatch _match;
        private ISession _session;
        private ISocket _socket;

        public SyncTestUserEnvironment(string userId, bool delayRegistration = false)
        {
            _userId = userId;
            _client = TestsUtil.FromSettingsFile();
            _logger = TestsUtil.LoadConfiguration().StdOut ? new StdoutLogger() : null;
            _socket = Nakama.Socket.From(_client);
            _socket.ReceivedError += e => _logger?.ErrorFormat($"{e.Message}{e.StackTrace}");
            _sharedVars = new SyncTestSharedVars(_userId, _varRegistry, delayRegistration);
            _groupVars = new SyncTestGroupVars(_varRegistry, delayRegistration);
            _rpcs = new SyncTestRpcs(_rpcRegistry);
        }

        public async Task StartMatchViaMatchmaker(int count)
        {
            await Connect();
            await _socket.AddMatchmakerAsync("*", minCount: count, maxCount: count);

            var matchedTcs = new TaskCompletionSource<IMatchmakerMatched>();

            _socket.ReceivedMatchmakerMatched += matched =>
            {
                matchedTcs.SetResult(matched);
            };

            await matchedTcs.Task;

            _match = await _socket.JoinSyncMatch(_session,  matchedTcs.Task.Result, _varRegistry, _rpcRegistry);
            _rpcs.ReceiveMatch(_match);
        }

        public async Task<IMatch> CreateMatch()
        {
            await Connect();
            _match = await _socket.CreateSyncMatch(_session, _varRegistry, _rpcRegistry);
            _rpcs.ReceiveMatch(_match);
            return _match;
        }

        public async Task<IMatch> CreateMatch(string name)
        {
            await Connect();
            _match = await _socket.CreateSyncMatch(_session, _varRegistry, _rpcRegistry, name);
            _rpcs.ReceiveMatch(_match);
            return _match;
        }

        public async Task<IMatch> JoinMatch(string matchId)
        {
            await Connect();
            _match = await _socket.JoinSyncMatch(_session,  matchId, _varRegistry, _rpcRegistry);
            _rpcs.ReceiveMatch(_match);
            return _match;
        }

        public async Task Dispose()
        {
            await _socket.CloseAsync();
        }

        private async Task Connect()
        {
            _session = await _client.AuthenticateCustomAsync(_userId);
            await _socket.ConnectAsync(_session);
        }
    }
}
