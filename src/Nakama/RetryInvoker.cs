/*
 * Copyright 2021 Heroic Labs
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
using System.Net.Http;
using System.Threading.Tasks;

namespace Nakama
{
    /// <summary>
    /// Invokes requests with retry and exponential backoff.
    /// </summary>
    internal class RetryInvoker
    {
        public int JitterSeed { get; private set; }

        private readonly Random _random;

        public RetryInvoker(int jitterSeed = 0)
        {
            JitterSeed = jitterSeed;
            _random = new Random(JitterSeed);
        }

        public async Task<T> InvokeWithRetry<T>(Func<Task<T>> request, RetrySchedule schedule = null)
        {
            try
            {
                return await request();
            }
            catch (Exception e)
            {
                if (IsTransientException(e))
                {
                    await Backoff(schedule, e);
                    return await InvokeWithRetry<T>(request, schedule);
                }
                else
                {
                    throw e;
                }
            }
        }

        public async Task InvokeWithRetry(Func<Task> request, RetrySchedule schedule = null)
        {
            try
            {
                await request();
            }
            catch (Exception e)
            {
                if (IsTransientException(e))
                {
                    await Backoff(schedule, e);
                    await InvokeWithRetry(request, schedule);
                }
                else
                {
                    throw e;
                }
            }
        }

        /// <summary>
        /// Whether or not the provided exception represents a temporary erroroneous state in the connection
        /// or on the server.
        /// </summary>
        private bool IsTransientException(Exception e)
        {
            return (e is ApiResponseException apiException && apiException.StatusCode >= 500) || e is HttpRequestException;
        }

        private Retry CreateNewRetry(RetrySchedule schedule)
        {
            double delaySeconds = Math.Pow(schedule.Configuration.BaseDelay.TotalSeconds, schedule.Retries.Count + 1);
            TimeSpan expoBackoff = TimeSpan.FromSeconds(delaySeconds);
            TimeSpan jitteredBackoff = schedule.Configuration.Jitter(schedule.Retries, expoBackoff, this._random);
            return new Retry(expoBackoff, jitteredBackoff);
        }

        private Task Backoff(RetrySchedule schedule, Exception e)
        {
            if (schedule.Retries.Count >= schedule.Configuration.MaxAttempts)
            {
                throw new TaskCanceledException("Exceeded max retry attempts.", e);
            }

            Retry newRetry = CreateNewRetry(schedule);
            schedule.Retries.Add(newRetry);
            schedule.Listener?.Invoke(schedule.Retries.Count, newRetry);
            return Task.Delay(newRetry.JitterBackoff.Milliseconds);
        }
    }
}
