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

using Microsoft.Extensions.Configuration;

namespace Nakama.Tests
{
    internal static class TestsUtil
    {
        public const int TIMEOUT_MILLISECONDS = 5000;

        private const string SettingsPath = "settings.json";

        public static IClient FromSettingsFile(string path = SettingsPath)
        {
            var configuration = LoadConfiguration(path);
            return FromConfiguration(configuration);
        }

        public static IClient FromConfiguration(TestConfiguration configuration)
        {
            var client = new Client(configuration.Host, configuration.Scheme, configuration.Port, configuration.ServerKey);

            if (configuration.Stdout)
            {
                client.Logger = new StdoutLogger();
            }

            return client;
        }

        public static TestConfiguration LoadConfiguration(string path = SettingsPath)
        {
            var settings = new ConfigurationBuilder().AddJsonFile(path).Build();
            return new TestConfiguration(
                System.Convert.ToInt32(settings["PORT"]),
                settings["SCHEME"],
                settings["HOST"],
                settings["SERVER_KEY"],
                System.Convert.ToBoolean(settings["STDOUT"]));

        }
    }

    internal class TestConfiguration
    {
        public int Port { get; }
        public string Scheme { get; }
        public string Host { get; }
        public string ServerKey { get; }
        public bool Stdout { get; }

        internal TestConfiguration(int port, string scheme, string host, string serverKey, bool stdout)
        {
            Port = port;
            Scheme = scheme;
            Host = host;
            ServerKey = serverKey;
            Stdout = stdout;
        }
    }
}
