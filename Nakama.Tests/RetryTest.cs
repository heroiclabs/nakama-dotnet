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

using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System;
using System.Net.Http;
using System.Threading;
using FluentAssertions;

namespace Nakama.Tests
{
    public class RetryTest
    {
        [Fact]
        public async Task TransientHttpAdapter_ServerDefault_CreatesSession()
        {
            var adapterSchedule = new TransientAdapterResponseType[1] { TransientAdapterResponseType.ServerOk };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);
            ISession session = await client.AuthenticateCustomAsync("test_id");
            Assert.NotNull(session);
        }

        [Fact]
        public async Task RetryConfiguration_OneRetries_RetriesExactlyOnce()
        {
            var adapterSchedule = new TransientAdapterResponseType[2]
                { TransientAdapterResponseType.TransientError, TransientAdapterResponseType.ServerOk };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            int lastNumRetry = -1;

            RetryListener retryListener = (int numRetry, Retry retry) => { lastNumRetry = numRetry; };

            var config = new RetryConfiguration(baseDelayMs: 10, maxRetries: 1, retryListener);
            client.GlobalRetryConfiguration = config;

            ISession session = await client.AuthenticateCustomAsync("test_id");

            Assert.NotNull(session);
            Assert.Equal(1, lastNumRetry);
        }

        [Fact]
        public async Task RetryConfiguration_FiveRetries_RetriesExactlyFiveTimes()
        {
            var adapterSchedule = new TransientAdapterResponseType[6]
            {
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

            RetryListener retryListener = (int numRetry, Retry retry) => { lastNumRetry = numRetry; };

            var config = new RetryConfiguration(baseDelayMs: 1, maxRetries: 5, retryListener);
            client.GlobalRetryConfiguration = config;

            Task<ISession> sessionTask = client.AuthenticateCustomAsync("test_id");

            ISession session = await sessionTask;
            Assert.NotNull(session);
            Assert.Equal(5, lastNumRetry);
        }

        [Fact]
        public async Task RetryConfiguration_PastMaxRetries_ThrowsTaskCancelledException()
        {
            var adapterSchedule = new TransientAdapterResponseType[4]
            {
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError
            };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            int lastNumRetry = 3;

            RetryListener retryListener = (int numRetry, Retry retry) => { lastNumRetry = numRetry; };

            var config = new RetryConfiguration(baseDelayMs: 500, maxRetries: 3, retryListener);
            client.GlobalRetryConfiguration = config;

            Task<ISession> sessionTask = client.AuthenticateCustomAsync("test_id");

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await sessionTask);
            Assert.Equal(3, lastNumRetry);
        }

        [Fact]
        public async Task RetryConfiguration_ZeroRetries_RetriesZeroTimes()
        {
            var adapterSchedule = new TransientAdapterResponseType[1] { TransientAdapterResponseType.TransientError };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            int lastNumRetry = -1;

            RetryListener retryListener = (int numRetry, Retry retry) => { lastNumRetry = numRetry; };

            var config = new RetryConfiguration(baseDelayMs: 10, maxRetries: 0, retryListener);
            client.GlobalRetryConfiguration = config;

            Task<ISession> sessionTask = client.AuthenticateCustomAsync("test_id");

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await sessionTask);
            Assert.Equal(-1, lastNumRetry);
        }

        [Fact]
        public async Task RetryConfiguration_OverrideSet_OverridesGlobal()
        {
            var adapterSchedule = new TransientAdapterResponseType[4]
            {
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.ServerOk
            };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            int lastNumRetry = -1;

            RetryListener retryListener = (int numRetry, Retry retry) => { lastNumRetry = numRetry; };

            var globalConfig = new RetryConfiguration(baseDelayMs: 10, maxRetries: 1, retryListener);
            client.GlobalRetryConfiguration = globalConfig;

            var localConfig = new RetryConfiguration(baseDelayMs: 10, maxRetries: 3, retryListener);
            var session = await client.AuthenticateCustomAsync("test_id", null, true, null, localConfig);

            Assert.NotNull(session);
            Assert.Equal(3, lastNumRetry);
        }

        [Fact]
        public async Task RetryConfiguration_Delay_ExpectedExponentialTimes()
        {
            var adapterSchedule = new TransientAdapterResponseType[4]
            {
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.ServerOk
            };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);


            var retries = new List<Retry>();

            RetryListener retryListener = (int numRetry, Retry retry) => { retries.Add(retry); };

            var config = new RetryConfiguration(baseDelayMs: 10, maxRetries: 3, retryListener);
            client.GlobalRetryConfiguration = config;

            Task<ISession> sessionTask = client.AuthenticateCustomAsync("test_id");

            ISession session = await sessionTask;
            Assert.NotNull(session);

            Assert.Equal(10, retries[0].ExponentialBackoff);
            Assert.Equal(20, retries[1].ExponentialBackoff);
            Assert.Equal(40, retries[2].ExponentialBackoff);
        }

        [Fact]
        public async Task RetryConfiguration_Delay_ExpectedDelays()
        {
            var adapterSchedule = new TransientAdapterResponseType[3]
            {
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
            };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);
            var retries = new List<Retry>();

            RetryListener retryListener = (int numRetry, Retry retry) => { retries.Add(retry); };

            var config = new RetryConfiguration(baseDelayMs: 10, maxRetries: 3, retryListener);
            client.GlobalRetryConfiguration = config;

            DateTime timeBeforeRequest = DateTime.Now;
            DateTime timeAfterRequest = default(DateTime);

            try
            {
                await client.AuthenticateCustomAsync("test_id");
            }
            catch
            {
                timeAfterRequest = DateTime.Now;
            }

            int expectedElapsedTime = retries.Sum(retry => retry.JitterBackoff);
            int actualElapsedTime = (int)(timeAfterRequest - timeBeforeRequest).TotalMilliseconds;

            // actual will be slightly higher due to cpu elapsed time
            Assert.True(expectedElapsedTime < actualElapsedTime);
        }

        [Fact]
        public async Task RetryConfiguration_NullConfiguration_DoesNotThrowNullRef()
        {
            var adapterSchedule = new TransientAdapterResponseType[3]
            {
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
            };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            client.GlobalRetryConfiguration = null;

            await Assert.ThrowsAsync<ApiResponseException>(async () => await client.AuthenticateCustomAsync("test_id"));
        }

        [Fact]
        public async Task RetryConfiguration_NoRetries_ThrowsBaseApiResponseException()
        {
            var adapterSchedule = new TransientAdapterResponseType[3]
            {
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
            };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            client.GlobalRetryConfiguration = new RetryConfiguration(baseDelayMs: 1, maxRetries: 0);

            try
            {
                await client.AuthenticateCustomAsync("test_id");
                throw new Exception("Test failed due to not throwing an exception");
            }
            catch (TaskCanceledException e)
            {
                Assert.True(e.GetBaseException() != null && e.GetBaseException() is ApiResponseException);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [Fact]
        public async Task RetryConfiguration_NonTransientError_Throws()
        {
            var adapterSchedule = new TransientAdapterResponseType[1]
            {
                TransientAdapterResponseType.NonTransientError,
            };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            client.GlobalRetryConfiguration = new RetryConfiguration(baseDelayMs: 1, maxRetries: 3);

            ApiResponseException e =
                await Assert.ThrowsAsync<ApiResponseException>(async () =>
                    await client.AuthenticateCustomAsync("test_id"));
        }
        
        [Fact]
        public async Task RetryInvoker_ShouldStopRetrying_WhenTotalTimeoutExceeded()
        {
            // Arrange
            var maxRetries = 10;
            var maxTotalTimeout = 250; // Ultimately must respect this
            var config = new RetryConfiguration(
                baseDelayMs: 100,
                maxRetries: maxRetries,
                listener: (_, _) => { },
                jitter: RetryJitter.FullJitter,
                maxTotalTimeoutMs: maxTotalTimeout
            );

            var history = new RetryHistory("", config, CancellationToken.None);
            var invoker = new RetryInvoker(ex => true); // Always retry

            int callCount = 0;
            Func<Task<bool>> failingRequest = () =>
            {
                ++callCount;
                throw new HttpRequestException("Simulated network error");
            };

            // The invoker should stop executing retries once cumulative time hits/exceeds 250ms
            await Assert.ThrowsAsync<TaskCanceledException>(() => 
                invoker.InvokeWithRetry(failingRequest, history)
            );

            // Call count should be ~5 despite 10 max retries
            callCount.Should().BeLessThan(6);
        }

        [Fact]
        public async Task RetryInvoker_ShouldNotScheduleBackoff_ThatExceedsTotalTimeout()
        {
            var maxTotalTimeout = 250;
            var config = new RetryConfiguration(
                baseDelayMs: 100,
                maxRetries: 10,
                listener: (_, _) => { },
                jitter: (retries, delay, random) => delay,
                maxTotalTimeoutMs: maxTotalTimeout
            );

            var history = new RetryHistory("", config, CancellationToken.None);
            var invoker = new RetryInvoker(ex => true);

            Func<Task<bool>> failingRequest = () => throw new HttpRequestException("Simulated network error");

            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                invoker.InvokeWithRetry(failingRequest, history)
            );

            history.Retries.Sum(r => r.JitterBackoff).Should().BeLessOrEqualTo(maxTotalTimeout);
            history.Retries.Count.Should().Be(1);
        }

        [Fact]
        public async Task RetryInvoker_Succeeds_WhenRequestRecoversBeforeTimeout()
        {
            // Arrange
            var baseDelayMs = 10;
            var maxRetries = 5;
            var config = new RetryConfiguration(
                baseDelayMs: baseDelayMs,
                maxRetries: maxRetries,
                listener: (_, _) => { },
                jitter: RetryJitter.FullJitter,
                maxTotalTimeoutMs: 1000
            );

            var history = new RetryHistory("", config, CancellationToken.None);
            var invoker = new RetryInvoker(ex => ex is HttpRequestException);

            int attemptCount = 0;
            string expectedResult = "Success!";

            // Fail twice with transient exception, then succeed on the 3rd try
            Func<Task<string>> recoveringRequest = () =>
            {
                ++attemptCount;
                if (attemptCount < 3)
                {
                    throw new HttpRequestException("Temporary network error");
                }

                return Task.FromResult(expectedResult);
            };

            // Act
            string result = await invoker.InvokeWithRetry(recoveringRequest, history);

            // Assert
            result.Should().Be(expectedResult);
            attemptCount.Should().Be(3);
            history.Retries.Count.Should().Be(2); // Recorded 2 failed attempts before success
        } 
    }
}
