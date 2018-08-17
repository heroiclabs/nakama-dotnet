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
    /// A group of options to configure the <c>Client</c>.
    /// </summary>
    public sealed class ClientOptions
    {
        /// <summary>
        /// The default host address used by the client.
        /// </summary>
        public const string DefaultHost = "127.0.0.1";

        /// <summary>
        /// The default port used by the client.
        /// </summary>
        public const int DefaultPort = 7350;

        /// <summary>
        /// The default server key used to authenticate with the server.
        /// </summary>
        public const string DefaultServerKey = "defaultkey";

        /// <summary>
        /// Enable automatic compression with requests in the client.
        /// </summary>
        public bool AutomaticCompression { get; set; }

        /// <summary>
        /// Enable automatic decompression with responses in the client.
        /// </summary>
        public bool AutomaticDecompression { get; set; }

        /// <summary>
        /// Enable SSL scheme with the client.
        /// </summary>
        public bool EnableSsl { get; set; }

        /// <summary>
        /// The host address of the server the client will connect.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The port number of the server the client will connect.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Verifies the remote Secure Sockets Layer (SSL) certificate used for authentication.
        /// </summary>
        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }

        /// <summary>
        /// The number of retries the client will use with each request.
        /// </summary>
        public int Retries { get; set; }

        /// <summary>
        /// The default timeout for each request attempt to the server.
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// The server key which should be used to authenticate with the server.
        /// </summary>
        public string ServerKey { get; set; }

        public ClientOptions()
        {
            AutomaticCompression = false;
            AutomaticDecompression = false; // Does not work with Mono AOT on Android.
            EnableSsl = false;
            Host = DefaultHost;
            Port = DefaultPort;
            Retries = 3;
            Timeout = TimeSpan.FromSeconds(5);
            ServerCertificateValidationCallback = delegate { return true; };
            ServerKey = DefaultServerKey;
        }

        internal void ValidateOptions()
        {
            if (Retries < 0)
            {
                throw new ArgumentException("Retries must be zero or greater.");
            }

            if (ServerCertificateValidationCallback == null)
            {
                throw new ArgumentException("ServerCertificateValidationCallback cannot be null.");
            }
        }

        internal ClientOptions Clone()
        {
            return new ClientOptions
            {
                AutomaticCompression = AutomaticCompression,
                AutomaticDecompression = AutomaticDecompression,
                EnableSsl = EnableSsl,
                Host = Host,
                Port = Port,
                Retries = Retries,
                Timeout = Timeout,
                ServerCertificateValidationCallback = ServerCertificateValidationCallback,
                ServerKey = ServerKey
            };
        }
    }
}
