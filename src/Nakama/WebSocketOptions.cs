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

namespace Nakama
{
    using System;
    using System.Net.Security;

    /// <summary>
    /// A group of options used to configure the <c>WebSocketWrapper</c>.
    /// </summary>
    public sealed class WebSocketOptions
    {
        /// <summary>
        /// The validation handler to use with SSL certificates.
        /// </summary>
        public RemoteCertificateValidationCallback CertificateValidationHandler { get; set; }

        /// <summary>
        /// The time to wait for the WebSocket close event. Defaults to 5 seconds.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; }

        /// <summary>
        /// Support both IPv4 and IPv6 addresses over the socket. Does not work with some versions of Mono.
        /// </summary>
        public bool DualMode { get; set; }

        /// <summary>
        /// A logger which can output log messages. Defaults to <c>NullLogger</c>.
        /// </summary>
        public ILogger Logger { get; set; }

        public WebSocketOptions()
        {
            CertificateValidationHandler = delegate { return true; };
            ConnectTimeout = TimeSpan.FromMilliseconds(5000);
            DualMode = false;
            Logger = NullLogger.Instance; // dont log by default.
        }

        internal void ValidateOptions()
        {
            if (ConnectTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("ConnectTimeout must be greater than 0.");
            }
        }

        internal WebSocketOptions Clone()
        {
            return new WebSocketOptions
            {
                CertificateValidationHandler = CertificateValidationHandler,
                ConnectTimeout = ConnectTimeout,
                DualMode = DualMode,
                Logger = Logger
            };
        }
    }
}
