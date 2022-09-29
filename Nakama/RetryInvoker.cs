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
using System.Threading.Tasks;

namespace Nakama
{
    /// <summary>
    /// Invokes requests with retry and exponential backoff.
    /// </summary>
    internal class RetryInvoker
    {
        private readonly TransientExceptionDelegate _del;

        public RetryInvoker(TransientExceptionDelegate del)
        {
            if (del == null)
            {
                throw new ArgumentException("Cannot initialize retry invoker with a null transient exception delegate.");
            }

            _del = del;
        }

        public async Task<T> InvokeWithRetry<T>(Func<Task<T>> request, RetryHistory history)
        {
            try
            {
                return await request();
            }
            catch (Exception e)
            {
                if (history.Configuration != null && _del(e))
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

        public async Task InvokeWithRetry(Func<Task> request, RetryHistory history)
        {
            try
            {
                await request();
            }
            catch (Exception e)
            {
                if (history.Configuration != null && _del(e))
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

        private Retry CreateNewRetry(RetryHistory history)
        {
            int expoBackoff = System.Convert.ToInt32(Math.Pow(2, history.Retries.Count)) * history.Configuration.BaseDelayMs;
            int jitteredBackoff = history.Configuration.Jitter(history.Retries, expoBackoff, history.Random);
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
            history.Configuration.RetryListener?.Invoke(history.Retries.Count, newRetry);

            if (history.UserCancelToken.HasValue)
            {
                return Task.Delay(newRetry.JitterBackoff, history.UserCancelToken.Value);
            }
            else
            {
                return Task.Delay(newRetry.JitterBackoff);
            }
        }
    }
}
