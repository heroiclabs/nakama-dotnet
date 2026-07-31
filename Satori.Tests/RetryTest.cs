/*
 * Copyright 2026 Heroic Labs
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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Satori.Tests
{
    public class RetryTest
    {
        [Fact]
        public async Task RetryInvoker_ShouldStopRetrying_WhenTotalTimeoutExceeded()
        {
            // Arrange
            var maxRetries = 10;
            var maxTotalTimeout = 250; // Ultimately must respect this
            var config = new RetryConfiguration(
                baseDelayMs: 100,
                maxRetries: maxRetries,
                listener: (numRetry, retry) => { },
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
        public async Task RetryInvoker_Succeeds_WhenRequestRecoversBeforeTimeout()
        {
            // Arrange
            var baseDelayMs = 10;
            var maxRetries = 5;
            var config = new RetryConfiguration(
                baseDelayMs: baseDelayMs,
                maxRetries: maxRetries,
                listener: (numRetry, retry) => { },
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
