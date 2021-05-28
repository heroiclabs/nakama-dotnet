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

        public async Task<T> InvokeWithRetry<T>(Func<Task<T>> request, RetryHistory history = null)
        {
            try
            {
                return await request();
            }
            catch (Exception e)
            {
                if (IsTransientException(e))
                {
                    await Backoff(history, e);
                    return await InvokeWithRetry<T>(request, history);
                }
                else
                {
                    throw e;
                }
            }
        }

        public async Task InvokeWithRetry(Func<Task> request, RetryHistory history = null)
        {
            try
            {
                await request();
            }
            catch (Exception e)
            {
                if (IsTransientException(e))
                {
                    await Backoff(history, e);
                    await InvokeWithRetry(request, history);
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

        private Retry CreateNewRetry(RetryHistory history)
        {
            double delaySeconds = Math.Pow(history.Configuration.BaseDelay.TotalSeconds, history.Retries.Count + 1);
            TimeSpan expoBackoff = TimeSpan.FromSeconds(delaySeconds);
            TimeSpan jitteredBackoff = history.Configuration.Jitter(history.Retries, expoBackoff, this._random);
            return new Retry(expoBackoff, jitteredBackoff);
        }

        private Task Backoff(RetryHistory history, Exception e)
        {
            if (history.Retries.Count >= history.Configuration.MaxAttempts)
            {
                throw new TaskCanceledException("Exceeded max retry attempts.", e);
            }

            Retry newRetry = CreateNewRetry(history);
            history.Retries.Add(newRetry);
            history.Listener?.Invoke(history.Retries.Count, newRetry);
            return Task.Delay(newRetry.JitterBackoff.Milliseconds);
        }
    }
}
