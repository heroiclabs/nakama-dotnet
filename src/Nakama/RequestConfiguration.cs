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

using System.Threading;

namespace Nakama
{
    /// <summary>
    /// A low-level configuration to use for requests from <see cref="IClient"/>.
    /// </summary>
    public class RequestConfiguration
    {
        /// <summary>
        /// A <see cref="Nakama.RetryConfiguration"/> used to control automatic retry requests.
        /// This configuration will override the <see cref="IClient.GlobalRetryConfiguration"/>.
        /// </summary>
        public RetryConfiguration RetryConfiguration { get; }

        /// <summary>
        /// A <see cref="CancellationTokenSource"> that issues cancellation tokens to client requests. Can
        /// be used to cancel requests that have been sent but not yet resolved.
        /// </summary>
        public CancellationTokenSource Canceller { get; }

        /// <summary>
        /// Create a new request configuration.
        /// </summary>
        /// <param name="retryConfiguration">
        /// A <see cref="Nakama.RetryConfiguration"/> used to control automatic retry requests.
        /// This configuration will override the <see cref="IClient.GlobalRetryConfiguration"/>.
        /// </param>
        /// <param name="canceller">
        /// A <see cref="CancellationTokenSource"> that issues cancellation tokens to client requests. Can
        /// be used to cancel requests that have been sent but not yet resolved.
        /// </param>
        /// <returns>A new request configuration.</returns>
        public RequestConfiguration(RetryConfiguration retryConfiguration, CancellationTokenSource canceller = null)
        {
            RetryConfiguration = retryConfiguration;
            Canceller = canceller;
        }

        /// <summary>
        /// Create a new request configuration.
        /// </summary>
        /// <param name="canceller">
        /// A <see cref="CancellationTokenSource"> that issues cancellation tokens to client requests. Can
        /// be used to cancel requests that have been sent but not yet resolved.
        /// </param>
        /// <returns>A new request configuration.</returns>
        public RequestConfiguration(CancellationTokenSource canceller)
        {
            Canceller = canceller;
        }
    }
}
