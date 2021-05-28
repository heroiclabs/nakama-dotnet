/**
 * Copyright 2021 The Nakama Authors
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
using System.Threading.Tasks;

namespace Nakama.Tests
{
    public enum TransientResponseType
    {
        ServerDefault,
        TransientError
    }

    /// <summary>
    /// An adapter which throws transient/retryable exceptions whenever a request is made.
    /// </summary>
    public class TransientExceptionHttpAdapter : IHttpAdapter
    {
        public ILogger Logger { get; set; }

        private int _sendAttempts = 0;
        private readonly TransientResponseType[] _sendSchedule;
        private readonly IHttpAdapter _httpRequestAdapter = HttpRequestAdapter.WithGzip();

        public TransientExceptionHttpAdapter(params TransientResponseType[] sendSchedule)
        {
            _sendSchedule = sendSchedule;
        }

        Task<string> IHttpAdapter.SendAsync(string method, Uri uri, IDictionary<string, string> headers, byte[] body, int timeoutSec)
        {
            if (_sendAttempts > _sendSchedule.Length - 1)
            {
                throw new IndexOutOfRangeException("The number of send attempts has exceeded the length of the send schedule.");
            }

            TransientResponseType responseType = _sendSchedule[_sendAttempts];
            _sendAttempts++;

            switch (responseType)
            {
                case TransientResponseType.TransientError:
                    throw new ApiResponseException(500, "This exception represents a transient error.", -1);
                default:
                    return _httpRequestAdapter.SendAsync(method, uri, headers, body, timeoutSec);
            }
        }
    }
}
