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
        public SyncTestOtherVars OtherVars => _OtherVars;
        public SyncTestRpcs Rpcs => _rpcs;
        public SyncMatch Match => _match;

        private readonly string _userId;
        private readonly IClient _client;
        private readonly VarRegistry _varRegistry = new VarRegistry();
        private readonly RpcTargetRegistry _rpcTargetRegistry = new RpcTargetRegistry();
        private readonly SyncOpcodes _opcodes;
        private SyncTestSharedVars _sharedVars;
        private SyncTestOtherVars _OtherVars;
        private SyncTestRpcs _rpcs;
        private readonly ILogger _logger;
        private readonly VarIdGenerator _varIdGenerator;

        private SyncMatch _match;
        private ISession _session;
        private ISocket _socket;

        public SyncTestUserEnvironment(string userId, SyncOpcodes opcodes, VarIdGenerator varIdGenerator, int numOtherVarCollections, int numOtherVarsPerCollection) : this(userId, opcodes)
        {
            _varIdGenerator = varIdGenerator;
            _OtherVars = new SyncTestOtherVars(_varRegistry, numOtherVarCollections, numOtherVarsPerCollection);
        }

        public SyncTestUserEnvironment(string userId, SyncOpcodes opcodes, VarIdGenerator varIdGenerator, int numSharedVars) : this(userId, opcodes)
        {
            _varIdGenerator = varIdGenerator;
            _sharedVars = new SyncTestSharedVars(_userId, _varRegistry, numSharedVars, _varIdGenerator);
        }

        public SyncTestUserEnvironment(string userId, SyncOpcodes opcodes)
        {
            _userId = userId;
            _opcodes = opcodes;
            _client = TestsUtil.FromSettingsFile();
            _logger = TestsUtil.LoadConfiguration().StdOut ? new StdoutLogger() : null;
            _socket = Nakama.Socket.From(_client);
            _rpcs = new SyncTestRpcs(_rpcTargetRegistry);
        }

        public async Task StartMatchViaMatchmaker(int count, SyncErrorHandler errorHandler)
        {
            await Connect();
            await _socket.AddMatchmakerAsync("*", minCount: count, maxCount: count);

            var matchedTcs = new TaskCompletionSource<IMatchmakerMatched>();

            _socket.ReceivedMatchmakerMatched += matched =>
            {
                matchedTcs.SetResult(matched);
            };

            await matchedTcs.Task;

            _match = await _socket.JoinSyncMatch(_session, _opcodes, matchedTcs.Task.Result, _varRegistry, _rpcTargetRegistry, errorHandler, _logger);
            _rpcs.ReceiveMatch(_match);
        }

        public async Task<IMatch> CreateMatch(SyncErrorHandler errorHandler)
        {
            await Connect();
            _match = await _socket.CreateSyncMatch(_session, _varRegistry, _rpcTargetRegistry, _opcodes, errorHandler, _logger);
            _rpcs.ReceiveMatch(_match);
            return _match;
        }

        public async Task<IMatch> CreateMatch(string name, SyncErrorHandler errorHandler)
        {
            await Connect();
            _match = await _socket.CreateSyncMatch(_session, _varRegistry, _rpcTargetRegistry, _opcodes, errorHandler, _logger, name);
            _rpcs.ReceiveMatch(_match);
            return _match;
        }

        public async Task<IMatch> JoinMatch(string matchId, SyncErrorHandler errorHandler)
        {
            await Connect();
            _match = await _socket.JoinSyncMatch(_session, _opcodes, matchId, _varRegistry, _rpcTargetRegistry, errorHandler, _logger);
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
