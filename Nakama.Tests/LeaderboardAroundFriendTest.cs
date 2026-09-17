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

namespace Nakama.Tests.Api
{
    public class LeaderboardAroundFriendTest : LeaderboardTest
    {
        private ISession[] _sessions = null;

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task FriendInFront()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 10, limit: 4, friendIndex: 1);
            var recordArray = records.Records.ToArray();

            Assert.Equal(4, recordArray.Length);
            Assert.Equal("109", recordArray[0].Score);
            Assert.Equal("108", recordArray[1].Score);
            Assert.Equal("107", recordArray[2].Score);
            Assert.Equal("106", recordArray[3].Score);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task FriendInBack()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 10, limit: 4, friendIndex: 9);
            var recordArray = records.Records.ToArray();

            Assert.Equal(4, recordArray.Length);
            Assert.Equal("103", recordArray[0].Score);
            Assert.Equal("102", recordArray[1].Score);
            Assert.Equal("101", recordArray[2].Score);
            Assert.Equal("100", recordArray[3].Score);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task FriendInMiddle()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 10, limit: 4, friendIndex: 5);
            var recordArray = records.Records.ToArray();

            Assert.Equal(4, recordArray.Length);
            Assert.Equal("105", recordArray[0].Score);
            Assert.Equal("104", recordArray[1].Score);
            Assert.Equal("103", recordArray[2].Score);
            Assert.Equal("102", recordArray[3].Score);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task NotEnoughRecordsForLimit()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 4, limit: 10, friendIndex: 2);
            var recordArray = records.Records.ToArray();

            Assert.Equal(4, recordArray.Length);
            Assert.Equal("103", recordArray[0].Score);
            Assert.Equal("102", recordArray[1].Score);
            Assert.Equal("101", recordArray[2].Score);
            Assert.Equal("100", recordArray[3].Score);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task CallerUsernameIsPopulated()
        {
            // The caller (session[0]) should have their username populated in returned records
            // when their own record appears in the results.
            int numRecords = 3;
            int friendIndex = 1;

            var authTasks = new List<Task<ISession>>();
            for (int i = 0; i < numRecords; i++)
            {
                authTasks.Add(_client.AuthenticateCustomAsync($"{Guid.NewGuid()}"));
            }
            ISession[] sessions = await Task.WhenAll(authTasks.ToArray());
            _sessions = sessions;

            // Establish mutual friendship between caller (sessions[0]) and friend (sessions[friendIndex])
            await _client.AddFriendsAsync(sessions[0], new[] { sessions[friendIndex].UserId });
            await _client.AddFriendsAsync(sessions[friendIndex], new[] { sessions[0].UserId });

            // Write leaderboard records
            var writeTasks = new List<Task<IApiLeaderboardRecord>>();
            for (int i = 0; i < numRecords; i++)
            {
                int score = 100 + numRecords - i - 1;
                writeTasks.Add(_client.WriteLeaderboardRecordAsync(sessions[i], _leaderboardId, score));
            }
            Task.WaitAll(writeTasks.ToArray());

            // Fetch records around the friend, called by sessions[0]
            IApiLeaderboardRecordList records = await _client.ListLeaderboardRecordsAroundFriendAsync(
                sessions[0], _leaderboardId, sessions[friendIndex].UserId, null, numRecords);

            // Find the caller's record and verify username is set
            var callerRecord = records.Records.FirstOrDefault(r => r.OwnerId == sessions[0].UserId);
            if (callerRecord != null)
            {
                Assert.Equal(sessions[0].Username, callerRecord.Username);
            }
        }

        private async Task<IApiLeaderboardRecordList> CreateAndFetchRecords(int numRecords, int limit, int friendIndex)
        {
            var authTasks = new List<Task<ISession>>();

            for (int i = 0; i < numRecords; i++)
            {
                authTasks.Add(_client.AuthenticateCustomAsync($"{Guid.NewGuid()}"));
            }

            ISession[] sessions = await Task.WhenAll(authTasks.ToArray());
            _sessions = sessions;

            // The caller is always sessions[0]. Establish mutual friendship with the target friend.
            await _client.AddFriendsAsync(sessions[0], new[] { sessions[friendIndex].UserId });
            await _client.AddFriendsAsync(sessions[friendIndex], new[] { sessions[0].UserId });

            var writeTasks = new List<Task<IApiLeaderboardRecord>>();

            for (int i = 0; i < numRecords; i++)
            {
                int score = 100 + numRecords - i - 1;
                writeTasks.Add(_client.WriteLeaderboardRecordAsync(sessions[i], _leaderboardId, score));
            }

            Task.WaitAll(writeTasks.ToArray());

            IApiLeaderboardRecordList records = await _client.ListLeaderboardRecordsAroundFriendAsync(
                sessions[0], _leaderboardId, sessions[friendIndex].UserId, null, limit);
            return records;
        }
    }
}
