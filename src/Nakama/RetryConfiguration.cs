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

namespace Nakama
{
    /// <summary>
    /// A configuration for controlling retriable requests.
    ///
    /// Retry configurations can be assigned to the <see cref="IClient"/> on a request-by-request basis using
    /// using <see cref="IClient.ConfigureRetry"/> by passing the name of the method to configure as a key.
    ///
    /// Retry configurations can also be assigned on a global basis using <see cref="IClient.GlobalRetryConfiguration"/>.
    /// Configurations assigned via <see cref="IClient.ConfigureRetry"/> take precedence over the global configuration.
    ///
    /// Note that assigning a new configuration does not change the configuration used by outstanding requests.
    /// </summary>
    public class RetryConfiguration
    {
        /// <summary>
        /// The base delay used to calculate the time before making another request attempt.
        /// This base will be raised to N, where N is the number of retry attempts.
        /// </summary>
        public TimeSpan BaseDelay { get; }

        /// <summary>
        /// The jitter algorithm used to apply randomness to the retry delay. Defaults to <see cref="RetryJitter.FullJitter"/>
        /// </summary>
        public Jitter Jitter { get; }

        /// <summary>
        /// The maximum number of attempts to make before cancelling the request task.
        /// </summary>
        /// <value></value>
        public int MaxAttempts { get; }

        /// <summary>
        /// The maximum amount of time to wait between requests.
        /// </summary>
        /// <value></value>
        public TimeSpan? MaxDelay { get; }

        /// <summary>
        /// Create a new retry configuration.
        /// </summary>
        /// <param name="baseDelay">The base delay used to calculate the time before making another request attempt.</param>
        /// <param name="maxAttempts">The maximum number of attempts to make before cancelling the request task.</param>
        /// <returns></returns>
        public RetryConfiguration(TimeSpan baseDelay, int maxAttempts) :
            this(baseDelay, maxAttempts, null, RetryJitter.FullJitter) {}

        /// <summary>
        /// Create a new retry configuration.
        /// </summary>
        /// <param name="baseDelay">The base delay used to calculate the time before making another request attempt.</param>
        /// <param name="maxAttempts">The maximum number of attempts to make before cancelling the request task.</param>
        /// <param name="maxDelay">The maximum number of attempts to make before cancelling the request task.</param>
        /// <returns></returns>
        public RetryConfiguration(TimeSpan baseDelay, int maxAttempts, TimeSpan maxDelay) :
            this(baseDelay, maxAttempts, maxDelay, RetryJitter.FullJitter) {}

        /// <summary>
        /// Create a new retry configuration.
        /// </summary>
        /// <param name="baseDelay">The base delay used to calculate the time before making another request attempt.</param>
        /// <param name="maxAttempts">The maximum number of attempts to make before cancelling the request task.</param>
        /// <param name="maxDelay">The maximum number of attempts to make before cancelling the request task.</param>
        /// <param name="jitter">/// The jitter algorithm used to apply randomness to the retry delay.</param>
        public RetryConfiguration(TimeSpan baseDelay, int maxAttempts, TimeSpan? maxDelay, Jitter jitter)
        {
            BaseDelay = baseDelay;
            Jitter = jitter;
            MaxAttempts = maxAttempts;
            MaxDelay = maxDelay;
        }
    }
}
