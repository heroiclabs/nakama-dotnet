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
    /// Listens to retry events for a particular request.
    /// </summary>
    /// <param name="numRetry">The number of retries made so far, including this retry.</param>
    /// <param name="retry">An holding inromation about the retry attempt.</param>
    /// <param name="cancellationSource">A token source that issues cancellation tokens to all retry requests.
    /// Call <see cref="CancellationTokenSource.Cancel"/> on this object to cancel the existing retry request
    /// and prevent future retries by raising a <see cref="TaskCanceledException"/>.
    /// </param>
    public delegate void RetryListener(int numRetry, Retry retry, CancellationTokenSource cancellationSource);
}