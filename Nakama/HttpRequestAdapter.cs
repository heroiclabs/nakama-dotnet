/**
 * Copyright 2019 The Nakama Authors
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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Nakama.TinyJson;

namespace Nakama
{
    /// <summary>
    /// HTTP Request adapter which uses the .NET HttpClient to send requests.
    /// </summary>
    /// <remarks>
    /// Accept header is always set as 'application/json'.
    /// </remarks>
    public class HttpRequestAdapter : IHttpAdapter
    {
        /// <inheritdoc cref="IHttpAdapter.Logger"/>
        public ILogger Logger { get; set; }

        public TransientExceptionDelegate TransientExceptionDelegate => IsTransientException;

        private readonly HttpClient _httpClient;

        public HttpRequestAdapter(HttpClient httpClient)
        {
            _httpClient = httpClient;

            // remove cap of max timeout on HttpClient from 100 seconds.
            _httpClient.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        }

        /// <inheritdoc cref="IHttpAdapter"/>
        public async Task<string> SendAsync(string method, Uri uri, IDictionary<string, string> headers, byte[] body,
            int timeout, CancellationToken? userCancelToken)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = uri,
                Method = new HttpMethod(method),
                Headers =
                {
                    Accept = {new MediaTypeWithQualityHeaderValue("application/json")}
                }
            };

            foreach (var kv in headers)
            {
                request.Headers.Add(kv.Key, kv.Value);
            }

            if (body != null)
            {
                request.Content = new ByteArrayContent(body);
            }

            var timeoutTokenSource = new CancellationTokenSource();
            CancellationToken timeoutToken = timeoutTokenSource.Token;
            timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            CancellationTokenSource linkedSource = null;

            if (userCancelToken.HasValue)
            {
                linkedSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, userCancelToken.Value);
            }

            Logger?.InfoFormat("Send: method='{0}', uri='{1}', body='{2}'", method, uri, body);

            var response = await _httpClient.SendAsync(request, linkedSource == null ? timeoutToken : linkedSource.Token);

            var contents = await response.Content.ReadAsStringAsync();
            response.Content?.Dispose();

            if (((int)response.StatusCode) >= 500)
            {
                // TODO think of best way to map HTTP code to GRPC code since we can't rely
                // on server to process it. Manually adding the mapping to SDK seems brittle.
                throw new ApiResponseException((int) response.StatusCode, contents, -1);
            }

            Logger?.InfoFormat("Received: status={0}, contents='{1}'", response.StatusCode, contents);

            if (response.IsSuccessStatusCode)
            {
                return contents;
            }

            var decoded = contents.FromJson<Dictionary<string, object>>();
            string message = decoded.ContainsKey("message") ? decoded["message"].ToString() : string.Empty;
            int grpcCode = decoded.ContainsKey("code") ? (int) decoded["code"] : -1;

            var exception = new ApiResponseException((int) response.StatusCode, message, grpcCode);

            if (decoded.ContainsKey("error"))
            {
                IHttpAdapterUtil.CopyResponseError(this, decoded["error"], exception);
            }

            throw exception;
        }

        /// <summary>
        /// A new HTTP adapter with configuration for gzip support in the underlying HTTP client.
        /// </summary>
        /// <remarks>
        /// NOTE Decompression does not work with Mono AOT on Android.
        /// </remarks>
        /// <param name="decompression">If automatic decompression should be enabled with the HTTP adapter.</param>
        /// <param name="compression">If automatic compression should be enabled with the HTTP adapter.</param>
        /// <returns>A new HTTP adapter.</returns>
        public static IHttpAdapter WithGzip(bool decompression = false, bool compression = false)
        {
            var handler = new HttpClientHandler();
            if (handler.SupportsAutomaticDecompression && decompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            var client =
                new HttpClient(compression ? (HttpMessageHandler) new GZipHttpClientHandler(handler) : handler);
            return new HttpRequestAdapter(client);
        }

        private bool IsTransientException(Exception e)
        {
            if (e is ApiResponseException apiException)
            {
                switch (apiException.StatusCode)
                {
                    case 500: // Internal Server Error often (but not always) indicates a transient issue in Nakama, e.g., DB connectivity.
                    case 502: // LB returns this to client if server sends corrupt/invalid data to LB, which may be a transient issue.
                    case 503: // LB returns this to client if LB determines or is told that server is unable to handle forwarded from LB, which may be a transient issue.
                    case 504: // LB returns this to client if LB cannot communicate with server, which may be a temporary issue.
                        return true;
                }
            }

            return false;
        }
    }
}
