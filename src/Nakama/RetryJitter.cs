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
    /// The Jitter algorithm is responsible for introducing randomness to a delay before a retry.
    /// </summary>
    /// <param name="retryHistory">Information about previous retry attempts.</param>
    /// <param name="retryDelay">A delay (milliseconds) between the last failed attempt in the retry history
    /// and the next upcoming attempt.</param>
    /// <param name="random">A <see cref="Random"/> object that has been seeded by <see cref="IClient.RetryJitterSeed"/>.
    /// <returns>A new delay (milliseconds) between the last failed attempt in the retry history and the next upcoming attempt.</returns>
    public delegate int Jitter(IList<Retry> retryHistory, int retryDelay, Random random);

    /// <summary>
    /// A collection of <see cref="Jitter"/> algorithms.
    /// </summary>
    public static class RetryJitter
    {
        /// <summary>
        /// FullJitter is a Jitter algorithm that selects a random point between now and the next retry time.
        /// </summary>
        public static int FullJitter(IList<Retry> retries, int retryDelay, Random random)
        {
            return System.Convert.ToInt32(retryDelay * random.NextDouble());
        }
    }
}
