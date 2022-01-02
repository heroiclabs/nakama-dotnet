/**
 * Copyright 2020 The Nakama Authors
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
using System.Threading;
using System.Threading.Tasks;

namespace Nakama
{
    /// <summary>
    /// An adapter which implements the HTTP protocol.
    /// </summary>
    public interface IHttpAdapter
    {
        // A delegate used by the adapter to determine whether or not an error from the server
        // should be retried or not (i.e., is 'transient').
        TransientExceptionDelegate TransientExceptionDelegate { get; }

        /// <summary>
        /// The logger to use with the adapter.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// Send a HTTP request.
        /// </summary>
        /// <param name="method">HTTP method to use for this request.</param>
        /// <param name="uri">The fully qualified URI to use.</param>
        /// <param name="headers">Request headers to set.</param>
        /// <param name="body">Request content body to set.</param>
        /// <param name="timeoutSec">Request timeout.</param>
        /// <param name="userCancelToken">A user-generated token that can be used to cancel the request.</param>
        /// <returns>A task which resolves to the contents of the response.</returns>
        Task<string> SendAsync(string method, Uri uri, IDictionary<string, string> headers, byte[] body, int timeoutSec = 3, CancellationToken? userCancelToken = null);
    }
}
