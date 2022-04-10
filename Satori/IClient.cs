// Copyright 2018 The Nakama Authors
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

namespace Nakama
{
    /// <summary>
    /// A client for the API in Nakama server.
    /// </summary>
    public interface IClient
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
        /// Set the timeout in seconds on requests sent to the server.
        /// </summary>
        int Timeout { get; set; }
	}
}