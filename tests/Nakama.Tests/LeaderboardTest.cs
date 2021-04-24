﻿/**
 * Copyright 2018 The Nakama Authors
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

namespace Nakama.Tests.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;
    using TinyJson;

    public class LeaderboardTest : IAsyncLifetime
    {
        private IClient _client;
        private string _leaderboardId;

        // ReSharper disable RedundantArgumentDefaultValue

        public LeaderboardTest()
        {
            _client = new Client("http", "127.0.0.1", 7350, "defaultkey");
        }

        [Fact]
        public async Task ShouldWriteLeaderboardRecord()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            const long score = long.MaxValue;
            const int subscore = 10;
            const string metadata = "{\"race_conditions\": \"wet\"}";
            var record = await _client.WriteLeaderboardRecordAsync(session, _leaderboardId, score, subscore, metadata);

            Assert.NotNull(record);
            Assert.NotEmpty(record.CreateTime);
            Assert.NotEmpty(record.UpdateTime);
            Assert.Equal(_leaderboardId, record.LeaderboardId);
            Assert.Equal(1, record.NumScore);
            Assert.Equal(score, long.Parse(record.Score));
            Assert.Equal(subscore, long.Parse(record.Subscore));
            Assert.Equal(session.UserId, record.OwnerId);
            Assert.Equal(session.Username, record.Username);
        }

        [Fact]
        public async Task ShouldListLeaderboardRecordsWithOwnerId()
        {
            string guid = Guid.NewGuid().ToString();
            string username = guid + "_username";
            var session = await _client.AuthenticateCustomAsync(guid, username);
            await _client.WriteLeaderboardRecordAsync(session, _leaderboardId, 10L);
            var result = await _client.ListLeaderboardRecordsAsync(session, _leaderboardId, new[] {session.UserId});

            Assert.NotNull(result);
            Assert.Null(result.NextCursor);
            Assert.Null(result.PrevCursor);
            Assert.NotEmpty(result.Records);
            Assert.Equal(result.OwnerRecords.Count(r => r.OwnerId == session.UserId), 1);
            Assert.True(result.Records.Any(record => record.Username == username));
            Assert.True(result.OwnerRecords.Any(record => record.Username == username));
        }

        [Fact]
        public async Task ShouldListLeaderboardRecordsEmpty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var result = await _client.ListLeaderboardRecordsAsync(session, _leaderboardId);

            Assert.NotNull(result);
            Assert.Null(result.NextCursor);
            Assert.Null(result.PrevCursor);
            Assert.Empty(result.Records);
            Assert.Empty(result.OwnerRecords);
        }

        [Fact]
        public async Task ShouldDeleteLeaderboardRecord()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _client.WriteLeaderboardRecordAsync(session, _leaderboardId, 10L);
            await _client.DeleteLeaderboardRecordAsync(session, _leaderboardId);
            var result = await _client.ListLeaderboardRecordsAsync(session, _leaderboardId, null, 100);

            Assert.NotNull(result);
            Assert.Null(result.NextCursor);
            Assert.Null(result.PrevCursor);
            Assert.Empty(result.Records);
        }

        [Fact]
        public async Task ShouldDeleteLeaderboardRecordNotFound()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _client.DeleteLeaderboardRecordAsync(session, _leaderboardId);
        }

        [Fact (Skip = "investigate this!")]
        public async Task ShouldDeleteLeaderboardRecordNotExists()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            await _client.DeleteLeaderboardRecordAsync(session, "invalid");
        }

        [Fact]
        public async Task ShouldReturnFirstAroundOwner()
        {
            const int NUM_TOTAL_RECORDS = 10;

            var authTasks = new List<Task<ISession>>();

            for (int i = 0; i < NUM_TOTAL_RECORDS; i++)
            {
                authTasks.Add( _client.AuthenticateCustomAsync($"{Guid.NewGuid()}"));
            }

            ISession[] sessions = await Task.WhenAll(authTasks.ToArray());

            var listTasks = new List<Task<IApiLeaderboardRecord>>();

            const int BASE_SCORE = 100;

            for (int i = 0; i < NUM_TOTAL_RECORDS; i++)
            {
                int score = BASE_SCORE + i;
                listTasks.Add(_client.WriteLeaderboardRecordAsync(sessions[i], _leaderboardId, score));
            }

            Task.WaitAll(listTasks.ToArray());

            const int NUM_RECORDS_TO_REQUEST = 4;

            int ownerIndex = 8;
            IApiLeaderboardRecordList records = await _client.ListLeaderboardRecordsAroundOwnerAsync(sessions[ownerIndex], _leaderboardId, sessions[ownerIndex].UserId, null, NUM_RECORDS_TO_REQUEST);
            IApiLeaderboardRecord[] recordArray = records.Records.ToArray();

            Assert.True(recordArray.Length == NUM_RECORDS_TO_REQUEST);

            Assert.Equal("109", recordArray[0].Score);
            Assert.Equal("108", recordArray[1].Score);
            Assert.Equal("107", recordArray[2].Score);
        }

        public async Task InitializeAsync()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            // Must create a leaderboard.
            var payload = new Dictionary<string, string>
            {
                {"operator", "best"}
            }.ToJson();


            var rpc = await _client.RpcAsync(session, "clientrpc.create_leaderboard", payload);
            _leaderboardId = rpc.Payload.FromJson<Dictionary<string, string>>()["leaderboard_id"];
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
