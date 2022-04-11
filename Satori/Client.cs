// Copyright 2022 The Satori Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Satori
{
    /// <inheritdoc cref="IClient"/>
    public class Client : IClient
    {
        /// <summary>
        /// The host address of the server. Defaults to "127.0.0.1".
        /// </summary>
        string Host { get; }

        /// <summary>
        /// The port number of the server. Defaults to 7350.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// The protocol scheme used to connect with the server. Must be either "http" or "https".
        /// </summary>
        string Scheme { get; }

        /// <summary>
        /// The key used to authenticate with the server without a session. Defaults to "defaultkey".
        /// </summary>
        string ServerKey { get; }

        /// <summary>
        /// Set the timeout in seconds on requests sent to the server.
        /// </summary>
        int Timeout { get; set; }

        public Client(string serverKey) : this(serverKey, HttpRequestAdapter.WithGzip())
        {
        }

        public Client(string serverKey, IHttpAdapter adapter) : this(DefaultScheme,
            DefaultHost, DefaultPort, serverKey,  adapter)
        {
        }

        public Client(string scheme, string host, int port, string serverKey) : this(
            scheme, host, port, serverKey, HttpRequestAdapter.WithGzip())
        {
            Host = host;
            Port = port;
            Scheme = scheme;
            ServerKey = serverKey;
            _apiClient = new ApiClient(new UriBuilder(scheme, host, port).Uri, adapter, DefaultTimeout);
        }

        /// <inheritdoc cref="IClient.AuthenticateAsync"/>
        public async Task<IApiSession> AuthenticateAsync(
            string basicAuthUsername,
            string basicAuthPassword,
            ApiAuthenticateRequest body,
            CancellationToken? cancellationToken)
            {

            }

        /// <inheritdoc cref="IClient.AuthenticateLogoutAsync"/>
        public async Task AuthenticateLogoutAsync(
            ISession session,
            ApiAuthenticateLogoutRequest body,
            CancellationToken? cancellationToken)
            {

            }

        /// <inheritdoc cref="IClient.AuthenticateRefreshAsync"/>
        public async Task<IApiSession> AuthenticateRefreshAsync(
            string basicAuthUsername,
            string basicAuthPassword,
            ApiAuthenticateRefreshRequest body,
            CancellationToken? cancellationToken)
            {

            }

        /// <inheritdoc cref="IClient.EventAsync"/>
        public async Task EventAsync(
            ISession session,
            ApiEventRequest body,
            CancellationToken? cancellationToken)
            {

            }


        /// <inheritdoc cref="IClient.GetExperimentsAsync"/>
        public async Task<IApiExperimentList> GetExperimentsAsync(
            ISession session,
            IEnumerable<string> names,
            CancellationToken? cancellationToken)
            {

            }

        /// <inheritdoc cref="IClient.GetFlagsAsync"/>
        public async Task<IApiFlagList> GetFlagsAsync(
            ISession session,
            IEnumerable<string> names,
            CancellationToken? cancellationToken);


        /// <inheritdoc cref="IClient.IdentifyAsync"/>
        public async Task<IApiSession> IdentifyAsync(
            ISession session,
            ApiIdentifyRequest body,
            CancellationToken? cancellationToken)
            {

            }


        /// <inheritdoc cref="IClient.GetLiveEentsAsync"/>
        public async Task<IApiLiveEventList> GetLiveEventsAsync(
            ISession session,
            IEnumerable<string> names,
            CancellationToken? cancellationToken)
            {

            }

        /// <inheritdoc cref="IClient.ListPropertiesAsync"/>
        public async Task<IApiProperties> ListPropertiesAsync(
            ISession session,
            CancellationToken? cancellationToken)
            {

            }

        /// <inheritdoc cref="IClient.UpdatePropertiesAsync"/>
        public async Task UpdatePropertiesAsync(
            ISession session,
            ApiUpdatePropertiesRequest body,
            CancellationToken? cancellationToken)
            {

            }
	}
}
