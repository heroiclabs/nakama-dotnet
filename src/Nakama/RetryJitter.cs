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

namespace Nakama
{
    /// <summary>
    /// The Jitter algorithm is responsible for introducing randomness to a retry schedule.
    /// </summary>
    /// <param name="retryHistory">Information about previous retry attempts in the retry schedule.</param>
    /// <param name="retryDelay">A span of time between the last failed attempt in the retry schedule
    /// and the next upcoming attempt.</param>
    /// <param name="random">A <see cref="Random"/> object that has been seeded by <see cref="IClient.RetryJitterSeed"/>.
    /// <returns>A new span of time between the last failed attempt in the retry schedule and the next upcoming attempt.</returns>
    public delegate TimeSpan Jitter(IList<Retry> retryHistory, TimeSpan retryDelay, Random random);

    /// <summary>
    /// A collection of <see cref="Jitter"/> algorithms.
    /// </summary>
    public static class RetryJitter
    {
        /// <summary>
        /// FullJitter is a Jitter algorithm that selects a random point between now and the next retry time.
        /// </summary>
        public static TimeSpan FullJitter(IList<Retry> retries, TimeSpan retryDelay, Random random)
        {
            return TimeSpan.FromMilliseconds(retryDelay.Milliseconds * random.NextDouble());
        }
    }
}
