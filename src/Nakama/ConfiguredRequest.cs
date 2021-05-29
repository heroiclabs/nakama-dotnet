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
    /// A low-level configuration to use for requests from <see cref="IClient"/>.
    /// Intended for advanced use only.
    /// </summary>
    public class ConfiguredRequest
    {
        private readonly RetryInvoker _invoker;
        private readonly RetryConfiguration _retryConfiguration;

        internal ConfiguredRequest(RetryConfiguration retryConfiguration, RetryInvoker invoker)
        {
            _retryConfiguration = retryConfiguration;
            _invoker = invoker;
        }

        /// <summary>
        /// Invokes the client request.
        /// </summary>
        /// <param name="request">The client request.</param>
        /// <typeparam name="T">The type parameter of the task representing the request.</typeparam>
        /// <returns>A task representing the request.</returns>
        public Task<T> Invoke<T>(Func<Task<T>> request)
        {
            return _invoker.InvokeWithRetry(request, new RetryHistory(_retryConfiguration));
        }

        /// <summary>
        /// Invokes the client request.
        /// </summary>
        /// <param name="request">The client request.</param>
        /// <returns>A task representing the request.</returns>
        public Task Invoke(Func<Task> request)
        {
            return request();
        }
    }
}
