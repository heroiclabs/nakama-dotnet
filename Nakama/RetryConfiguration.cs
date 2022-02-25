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

namespace Nakama
{
    /// <summary>
    /// A configuration for controlling retriable requests.
    /// </summary>
    /// <remarks>
    /// Retry configurations can be assigned to the <see cref="IClient"/> on a request-by-request basis via
    /// the see <cref="RequestConfiguration"/> parameter.
    ///
    /// Retry configurations can also be assigned on a global basis using <see cref="IClient.GlobalRetryConfiguration"/>.
    /// Configurations passed via the see <cref="RequestConfiguraiton"/> parameter take precedence over the global configuration.
    /// </remarks>
    public class RetryConfiguration
    {
        /// <summary>
        /// The base delay (milliseconds) used to calculate the time before making another request attempt.
        /// This base will be raised to N, where N is the number of retry attempts.
        /// </summary>
        public int BaseDelayMs { get; }

        /// <summary>
        /// The jitter algorithm used to apply randomness to the retry delay. Defaults to <see cref="RetryJitter.FullJitter"/>
        /// </summary>
        public Jitter Jitter { get; }

        /// <summary>
        /// The maximum number of attempts to make before cancelling the request task.
        /// </summary>
        public int MaxAttempts { get; }

        /// <summary>
        /// A callback that is invoked before a new retry attempt is made.
        /// </summary>
        public RetryListener RetryListener { get; }

        /// <summary>
        /// Create a new retry configuration.
        /// </summary>
        /// <param name="baseDelayMs">The base delay (milliseconds) used to calculate the time before making another request attempt.</param>
        /// <param name="maxRetries">The maximum number of attempts to make before cancelling the request task.</param>
        public RetryConfiguration(int baseDelayMs, int maxRetries) :
            this(baseDelayMs, maxRetries, null, RetryJitter.FullJitter) {}

        /// <summary>
        /// Create a new retry configuration.
        /// </summary>
        /// <param name="baseDelayMs">The base delay (milliseconds) used to calculate the time before making another request attempt.</param>
        /// <param name="maxRetries">The maximum number of attempts to make before cancelling the request task.</param>
        /// <param name="listener">A callback that is invoked before a new retry attempt is made.</param>
        public RetryConfiguration(int baseDelayMs, int maxRetries, RetryListener listener) :
            this(baseDelayMs, maxRetries, listener, RetryJitter.FullJitter) {}

        /// <summary>
        /// Create a new retry configuration.
        /// </summary>
        /// <param name="baseDelayMs">The base delay (milliseconds) used to calculate the time before making another request attempt.</param>
        /// <param name="maxRetries">The maximum number of attempts to make before cancelling the request task.</param>
        /// <param name="listener">A callback that is invoked before a new retry attempt is made.</param>
        /// <param name="jitter">/// The jitter algorithm used to apply randomness to the retry delay.</param>
        public RetryConfiguration(int baseDelayMs, int maxRetries, RetryListener listener, Jitter jitter)
        {
            BaseDelayMs = baseDelayMs;
            RetryListener = listener;
            MaxAttempts = maxRetries;
            Jitter = jitter;
        }
    }
}
