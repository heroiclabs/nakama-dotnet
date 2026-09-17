/*
 * Copyright 2026 Heroic Labs
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
    /// Listens for a request which has stopped retrying and is about to fail.
    /// </summary>
    /// <remarks>
    /// Invoked when either the retry attempts or the total timeout are exhausted. It is not invoked when the
    /// failure was never retriable in the first place, nor when the caller's cancellation token is cancelled.
    /// </remarks>
    /// <param name="retriesAttempted">The number of retries made before giving up, excluding the first attempt.</param>
    /// <param name="cause">The last failure observed from the request.</param>
    public delegate void RetriesExhaustedListener(int retriesAttempted, Exception cause);
}
