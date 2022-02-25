/**
 * Copyright 2018 The Nakama Authors
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

using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Nakama
{
    internal class GZipHttpClientHandler : DelegatingHandler
    {
        public GZipHttpClientHandler(HttpMessageHandler innerHandler)
        {
            InnerHandler = innerHandler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if ((request.Method == HttpMethod.Post || request.Method == HttpMethod.Put) && request.Content != null)
            {
                request.Content = new GZipContent(request.Content);
            }

            return base.SendAsync(request, ct);
        }
    }

    internal class GZipContent : HttpContent
    {
        private readonly HttpContent _content;

        public GZipContent(HttpContent content)
        {
            _content = content;
            // Must copy all pre-existing headers.
            foreach (var header in content.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            Headers.ContentEncoding.Add("gzip");
        }

        protected override async Task SerializeToStreamAsync(System.IO.Stream stream, TransportContext context)
        {
            using (var gzip = new GZipStream(stream, CompressionMode.Compress, true))
            {
                await _content.CopyToAsync(gzip);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
