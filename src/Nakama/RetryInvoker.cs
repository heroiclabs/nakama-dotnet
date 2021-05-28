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

        private readonly Dictionary<int, RetryListener> _retryListeners = new Dictionary<int, RetryListener>();
        private readonly Random _random;

        public RetryInvoker(int jitterSeed = 0)
        {
            JitterSeed = jitterSeed;
            _random = new Random(JitterSeed);
        }

        public RetryConfiguration GlobalRetryConfiguration { get; set; } = new RetryConfiguration(
            baseDelay: TimeSpan.FromSeconds(1),
            jitter: RetryJitter.FullJitter,
            maxRetries: 5,
            maxDelay: TimeSpan.FromSeconds(16));

        public async Task<T> InvokeWithRetry<T>(string retryId, Func<Task<T>> request, RetrySchedule schedule = null, RetryConfiguration configuration)
        {
            configuration = configuration ?? GlobalRetryConfiguration;
            schedule = schedule ?? new RetrySchedule(configuration);

            try
            {
                return await request();
            }
            catch (Exception e)
            {
                if (IsTransientException(e))
                {
                    await Backoff(schedule, e);
                    return await InvokeWithRetry<T>(retryId, request, schedule, configuration);
                }
                else
                {
                    throw e;
                }
            }
            finally
            {
                TryRemoveRetryListeners(schedule);
            }
        }

        public async Task InvokeWithRetry(string retryId, Func<Task> request, RetrySchedule schedule = null, RetryConfiguration configuration)
        {
            configuration = configuration ?? GlobalRetryConfiguration;
            schedule = schedule ?? new RetrySchedule(configuration);

            try
            {
                await request();
            }
            catch (Exception e)
            {
                if (IsTransientException(e))
                {
                    await Backoff(schedule, e);
                    await InvokeWithRetry(retryId, request, schedule, configuration);
                }
                else
                {
                    throw e;
                }
            }
            finally
            {
                TryRemoveRetryListeners(schedule);
            }
        }

        public void ListenForRetries(Task task, RetryListener listener)
        {
            if (_retryListeners.ContainsKey(task.Id))
            {
                _retryListeners[task.Id] += listener;
            }
            else
            {
                _retryListeners[task.Id] = listener;
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

        private void TryInvokeRetryListeners(RetrySchedule schedule, Retry newRetry)
        {
            if (schedule.OriginTask.HasValue && _retryListeners.ContainsKey(schedule.OriginTask.Value))
            {
                _retryListeners[schedule.OriginTask.Value].Invoke(
                    schedule.Retries.Count, newRetry);
            }
        }

        private void TryRemoveRetryListeners(RetrySchedule schedule)
        {
            if (schedule.OriginTask.HasValue && _retryListeners.ContainsKey(schedule.OriginTask.Value))
            {
                _retryListeners.Remove(schedule.OriginTask.Value);
            }
        }

        private Task Backoff(RetrySchedule schedule, Exception e)
        {
            if (schedule.Retries.Count >= schedule.Configuration.MaxAttempts)
            {
                throw new TaskCanceledException("Exceeded max retry attempts.", e);
            }

            Retry newRetry = CreateNewRetry(schedule);

            schedule.Retries.Add(newRetry);
            TryInvokeRetryListeners(schedule, newRetry);
            return Task.Delay(newRetry.JitterBackoff.Milliseconds);
        }
    }
}
