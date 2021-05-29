/**
 * Copyright 2020 The Nakama Authors
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
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests
{
    public class RetryTest
    {
        [Fact]
        public async void TransientHttpAdapter_ServerDefault_CreatesSession()
        {
            var adapterSchedule = new TransientAdapterResponseType[1]{TransientAdapterResponseType.ServerOk};

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);
            ISession session = await client.AuthenticateCustomAsync("test_id");
            Assert.NotNull(session);
        }

        [Fact]
        public async void RetryConfiguration_OneRetries_RetriesExactlyOnce()
        {
            var adapterSchedule = new TransientAdapterResponseType[2]{TransientAdapterResponseType.TransientError, TransientAdapterResponseType.ServerOk};

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            int lastNumRetry = -1;

            RetryListener retryListener = (int numRetry, Retry retry) => {
                lastNumRetry = numRetry;
            };

            var config = new RetryConfiguration(baseDelay: TimeSpan.FromMilliseconds(100), maxRetries: 1, retryListener);
            client.GlobalRetryConfiguration = config;

            ISession session = await client.AuthenticateCustomAsync("test_id");

            Assert.NotNull(session);
            Assert.Equal(1, lastNumRetry);
        }

        [Fact]
        public async void RetryConfiguration_FiveRetries_RetriesExactlyFiveTimes()
        {
            var adapterSchedule = new TransientAdapterResponseType[6] {
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.ServerOk
            };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            int lastNumRetry = -1;

            RetryListener retryListener = (int numRetry, Retry retry) => {
                lastNumRetry = numRetry;
            };

            var config = new RetryConfiguration(baseDelay: TimeSpan.FromMilliseconds(50), maxRetries: 5, retryListener);
            client.GlobalRetryConfiguration = config;

            Task<ISession> sessionTask = client.AuthenticateCustomAsync("test_id");

            ISession session = await sessionTask;
            Assert.NotNull(session);
            Assert.Equal(5, lastNumRetry);
        }

        [Fact]
        public async void RetryConfiguration_PastMaxRetries_ThrowsTaskCancelledException()
        {
            var adapterSchedule = new TransientAdapterResponseType[4]{
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError};

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            int lastNumRetry = 3;

            RetryListener retryListener = (int numRetry, Retry retry) => {
                lastNumRetry = numRetry;
            };

            var config = new RetryConfiguration(baseDelay: TimeSpan.FromMilliseconds(100), maxRetries: 3, retryListener);
            client.GlobalRetryConfiguration = config;


            Task<ISession> sessionTask = client.AuthenticateCustomAsync("test_id");

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await sessionTask);
            Assert.Equal(3, lastNumRetry);
        }

        [Fact]
        public async void RetryConfiguration_ZeroRetries_RetriesZeroTimes()
        {
            var adapterSchedule = new TransientAdapterResponseType[0]{};

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            int lastNumRetry = -1;

            RetryListener retryListener = (int numRetry, Retry retry) => {
                lastNumRetry = numRetry;
            };

            var config = new RetryConfiguration(baseDelay: TimeSpan.FromMilliseconds(100), maxRetries: 1, retryListener);
            client.GlobalRetryConfiguration = config;

            Task<ISession> sessionTask = client.AuthenticateCustomAsync("test_id");

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await sessionTask);
            Assert.Equal(-1, lastNumRetry);
        }

        [Fact]
        public async void RetryConfiguration_OverrideSet_OverridesGlobal()
        {
            var adapterSchedule = new TransientAdapterResponseType[3] {
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.ServerOk
            };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            int lastNumRetry = -1;

            RetryListener retryListener = (int numRetry, Retry retry) => {
                lastNumRetry = numRetry;
            };

            var globalConfig = new RetryConfiguration(baseDelay: TimeSpan.FromMilliseconds(100), maxRetries: 1);
            client.GlobalRetryConfiguration = globalConfig;

            var localConfig = new RetryConfiguration(baseDelay: TimeSpan.FromMilliseconds(100), maxRetries: 3);
            Task<ISession> sessionTask = client.ConfigureRequest(localConfig)
                .Invoke(() => client.AuthenticateCustomAsync("test_id"));

            ISession session = await sessionTask;
            Assert.NotNull(session);
        }

        [Fact]
        public async void RetryConfiguration_Delay_ExpectedTimes()
        {

        }
    }
}
