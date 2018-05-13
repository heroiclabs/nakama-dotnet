/**
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

using System.Collections.Generic;
using System.Net.Http;
using Nakama.TinyJson;

namespace Nakama.Tests.Api
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class LeaderboardTest
    {
        private IClient _client;
        private string _leaderboardId;

        // ReSharper disable RedundantArgumentDefaultValue
        [SetUp]
        public async Task SetUp()
        {
            _client = new Client("defaultkey", "127.0.0.1", 7350, false);

            // Must create a leaderboard.
            var payload = new Dictionary<string, string>
            {
                {"operator", "best"}
            }.ToJson();
            var rpc = await _client.RpcAsync("defaultkey", "clientrpc.create_leaderboard", payload);
            _leaderboardId = rpc.Payload.FromJson<Dictionary<string, string>>()["leaderboard_id"];
        }

        [Test]
        public async Task ShouldWriteLeaderboardRecord()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            const long score = long.MaxValue;
            const int subscore = 10;
            const string metadata = "{\"race_conditions\": \"wet\"}";
            var record = await _client.WriteLeaderboardRecordAsync(session, _leaderboardId, score, subscore, metadata);

            Assert.NotNull(record);
            Assert.IsNotEmpty(record.CreateTime);
            Assert.IsNotEmpty(record.UpdateTime);
            Assert.AreEqual(_leaderboardId, record.LeaderboardId);
            Assert.AreEqual(1, record.NumScore);
            Assert.AreEqual(score, record.Score);
            Assert.AreEqual(subscore, record.Subscore);
            Assert.AreEqual(session.UserId, record.OwnerId);
            Assert.AreEqual(session.Username, record.Username);
        }

        [Test]
        public async Task ShouldNotWriteLeaderboardRecord()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            var ex = Assert.ThrowsAsync<HttpRequestException>(() =>
                _client.WriteLeaderboardRecordAsync(session, "invalid", 0L));
            Assert.AreEqual("404 (Not Found)", ex.Message);
        }

        [Test]
        public async Task ShouldListLeaderboardRecords()
        {
            var session1 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var session2 = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");

            // Write a couple of records.
            await _client.WriteLeaderboardRecordAsync(session1, _leaderboardId, 10L);
            await _client.WriteLeaderboardRecordAsync(session2, _leaderboardId, 20L);

            var result = await _client.ListLeaderboardRecordsAsync(session1, _leaderboardId, null, 10);

            Assert.NotNull(result);
            Assert.IsEmpty(result.OwnerRecords);
            Assert.IsNotEmpty(result.Records);
            Assert.That(result.Records, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task ShouldListLeaderboardRecordsEmpty()
        {
            var session = await _client.AuthenticateCustomAsync($"{Guid.NewGuid()}");
            var result = await _client.ListLeaderboardRecordsAsync(session, _leaderboardId);

            Assert.NotNull(result);
            Assert.IsNull(result.NextCursor);
            Assert.IsNull(result.PrevCursor);
            Assert.IsEmpty(result.Records);
            Assert.IsEmpty(result.OwnerRecords);
        }
    }
}
