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
    public class LeaderboardAroundOwnerTest : LeaderboardTest
    {
        [Fact]
        public async Task OwnerInFront()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 10, limit: 4, ownerIndex: 0);
            var recordArray = records.Records.ToArray();

            Assert.Equal(4, recordArray.Length);
            Assert.Equal("109", recordArray[0].Score);
            Assert.Equal("108", recordArray[1].Score);
            Assert.Equal("107", recordArray[2].Score);
            Assert.Equal("106", recordArray[3].Score);
        }

        [Fact]
        public async Task OwnerInBack()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 10, limit: 4, ownerIndex: 9);
            var recordArray = records.Records.ToArray();

            Assert.Equal(4, recordArray.Length);
            Assert.Equal("103", recordArray[0].Score);
            Assert.Equal("102", recordArray[1].Score);
            Assert.Equal("101", recordArray[2].Score);
            Assert.Equal("100", recordArray[3].Score);
        }

        [Fact]
        public async Task OwnerNearFront()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 10, limit: 4, ownerIndex: 1);
            var recordArray = records.Records.ToArray();

            Assert.Equal(4, recordArray.Length);
            Assert.Equal("109", recordArray[0].Score);
            Assert.Equal("108", recordArray[1].Score);
            Assert.Equal("107", recordArray[2].Score);
            Assert.Equal("106", recordArray[3].Score);
        }

        [Fact]
        public async Task OwnerNearBack()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 10, limit: 4, ownerIndex: 8);
            var recordArray = records.Records.ToArray();

            Assert.Equal(4, recordArray.Length);
            Assert.Equal("103", recordArray[0].Score);
            Assert.Equal("102", recordArray[1].Score);
            Assert.Equal("101", recordArray[2].Score);
            Assert.Equal("100", recordArray[3].Score);
        }

        [Fact]
        public async Task OwnerInMiddle()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 10, limit: 4, ownerIndex: 5);
            var recordArray = records.Records.ToArray();

            Assert.Equal(recordArray.Length, 4);
            // owner score is 104
            Assert.Equal("105", recordArray[0].Score);
            Assert.Equal("104", recordArray[1].Score);
            Assert.Equal("103", recordArray[2].Score);
            Assert.Equal("102", recordArray[3].Score);
        }

        [Fact]
        public async Task NotEnoughRecordsForLimit()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 4, limit: 10, ownerIndex: 2);
            var recordArray = records.Records.ToArray();

            Assert.Equal(4, recordArray.Length);
            Assert.Equal("103", recordArray[0].Score);
            Assert.Equal("102", recordArray[1].Score);
            Assert.Equal("101", recordArray[2].Score);
            Assert.Equal("100", recordArray[3].Score);
        }

        [Fact]
        public async Task OddLimit()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 5, limit: 3, ownerIndex: 3);
            var recordArray = records.Records.ToArray();

            Assert.Equal(3, recordArray.Length);
            // owner score is 101
            Assert.Equal("102", recordArray[0].Score);
            Assert.Equal("101", recordArray[1].Score);
            Assert.Equal("100", recordArray[2].Score);
        }

        [Fact]
        public async Task NoRecords()
        {
            await Assert.ThrowsAsync<ApiResponseException>(() => CreateAndFetchRecords(numRecords: 1, limit: 0, ownerIndex: 0));
        }

        [Fact]
        public async Task OneRecordOneLimit()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 1, limit: 1, ownerIndex: 0);
            var recordArray = records.Records.ToArray();
            Assert.Equal(1, recordArray.Length);
            Assert.Equal("100", recordArray[0].Score);
        }

        [Fact]
        public async Task TwoRecordsTwoLimit()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 2, limit: 2, ownerIndex: 1);
            var recordArray = records.Records.ToArray();
            Assert.Equal(2, recordArray.Length);
            Assert.Equal("101", recordArray[0].Score);
            Assert.Equal("100", recordArray[1].Score);
        }

        [Fact]
        public async Task ThreeRecordsTwoLimit()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 3, limit: 2, ownerIndex: 1);
            var recordArray = records.Records.ToArray();
            Assert.Equal(2, recordArray.Length);
            Assert.Equal("101", recordArray[0].Score);
            Assert.Equal("100", recordArray[1].Score);
        }

        [Fact]
        public async Task ThreeRecordsThreeLimit()
        {
            IApiLeaderboardRecordList records = await CreateAndFetchRecords(numRecords: 3, limit: 3, ownerIndex: 1);
            var recordArray = records.Records.ToArray();

            Assert.Equal(3, recordArray.Length);
            Assert.Equal("102", recordArray[0].Score);
            Assert.Equal("101", recordArray[1].Score);
            Assert.Equal("100", recordArray[2].Score);
        }

        private async Task<IApiLeaderboardRecordList> CreateAndFetchRecords(int numRecords, int limit, int ownerIndex)
        {
            var authTasks = new List<Task<ISession>>();

            for (int i = 0; i < numRecords; i++)
            {
                authTasks.Add(_client.AuthenticateCustomAsync($"{Guid.NewGuid()}"));
            }

            ISession[] sessions = await Task.WhenAll(authTasks.ToArray());

            var listTasks = new List<Task<IApiLeaderboardRecord>>();

            for (int i = 0; i < numRecords; i++)
            {
                int score = 100 + numRecords - i - 1;
                listTasks.Add(_client.WriteLeaderboardRecordAsync(sessions[i], _leaderboardId, score));
            }

            Task.WaitAll(listTasks.ToArray());

            IApiLeaderboardRecordList records = await _client.ListLeaderboardRecordsAroundOwnerAsync(sessions[ownerIndex], _leaderboardId, sessions[ownerIndex].UserId, null, limit);
            return records;
        }
    }
}
