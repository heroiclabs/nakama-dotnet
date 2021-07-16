// Copyright 2021 The Nakama Authors
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

namespace Nakama.Tests
{
    internal class TestConfiguration
    {
        public string Scheme { get; }
        public string Host { get; }
        public int Port { get; }
        public string ServerKey { get; }
        public bool StdOut { get; }
        public LogLevel LogLevel { get; }

        internal TestConfiguration(string scheme, string host, int port, string serverKey, bool stdout, LogLevel logLevel)
        {
            Port = port;
            Scheme = scheme;
            Host = host;
            ServerKey = serverKey;
            StdOut = stdout;
            LogLevel = logLevel;
        }
    }
}
