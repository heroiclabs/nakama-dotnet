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

namespace Nakama.Tests
{
    public class SyncTestUserEnvironment
    {
        public IUserPresence Self => _match.Self;
        public ISession Session => _session;
        public SyncTestSharedVars SharedVars => _sharedVars;
        public SyncTestPresenceVars PresenceVars => _presenceVars;

        private readonly string _userId;
        private readonly IClient _client;
        private readonly VarRegistry _varRegistry = new VarRegistry();
        private readonly SyncOpcodes _opcodes;
        private SyncTestPresenceVars _presenceVars;
        private SyncTestSharedVars _sharedVars;
        private readonly int _numSharedVars;
        private readonly SyncErrorHandler _errorHandler;
        private readonly ILogger _logger;
        private readonly VarIdGenerator _varIdGenerator;

        private IMatch _match;
        private ISession _session;
        private ISocket _socket;

        public SyncTestUserEnvironment(string userId, SyncOpcodes opcodes, int numSharedVars, VarIdGenerator varIdGenerator)
        {
            _userId = userId;
            _opcodes = opcodes;
            _numSharedVars = numSharedVars;
            _client = TestsUtil.FromSettingsFile();
            _logger = TestsUtil.LoadConfiguration().StdOut ? null : new StdoutLogger();
            _varIdGenerator = varIdGenerator;
            _sharedVars = new SyncTestSharedVars(_userId, _varRegistry, _numSharedVars, _varIdGenerator);
            _socket = new Nakama.Socket();
        }

        public async Task StartMatchViaMatchmaker(int count, SyncErrorHandler errorHandler)
        {
            await Connect();
            await _socket.AddMatchmakerAsync("*", minCount: count, maxCount: count);

            var matchedTcs = new TaskCompletionSource<IMatchmakerMatched>();

            _socket.ReceivedMatchmakerMatched += (matched) =>
            {
                matchedTcs.SetResult(matched);
            };

            await matchedTcs.Task;

            _match = await _socket.JoinSyncMatch(_session, _opcodes, matchedTcs.Task.Result, _varRegistry, errorHandler, new StdoutLogger());
        }

        public async Task<IMatch> CreateMatch()
        {
            await Connect();
            _match = await _socket.CreateSyncMatch(_session, _varRegistry, _opcodes, _errorHandler, _logger);
            return _match;
        }

        public async Task JoinMatch(string matchId)
        {
            await Connect();
            await _socket.JoinSyncMatch(_session, _opcodes, matchId, _varRegistry, _errorHandler, _logger);
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
