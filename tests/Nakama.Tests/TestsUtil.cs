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

using System;
using Microsoft.Extensions.Configuration;

namespace Nakama.Tests
{
    internal static class TestsUtil
    {
        public const int TIMEOUT_MILLISECONDS = 5000;
        public const int MATCHMAKER_TIMEOUT_MILLISECONDS = 25000;
        public const string DefaultSettingsPath = "settings.json";

        public static IClient FromSettingsFile()
        {
            return FromSettingsFile(DefaultSettingsPath);
        }

        public static IClient FromSettingsFile(string path)
        {
            return FromSettingsFile(path, HttpRequestAdapter.WithGzip());
        }

        public static IClient FromSettingsFile(string path, IHttpAdapter adapter)
        {
            var configuration = LoadConfiguration(path);
            return FromConfiguration(configuration);
        }

        public static IClient FromConfiguration(TestConfiguration configuration)
        {
            var client = new Client(configuration.Scheme, configuration.Host, configuration.Port, configuration.ServerKey);

            {
                client.Logger = new StdoutLogger();
                client.Logger.LogLevel = configuration.LogLevel;
            }

            return client;
        }

        public static TestConfiguration LoadConfiguration(string path = DefaultSettingsPath)
        {
            var settings = new ConfigurationBuilder().AddJsonFile(path).Build();

            LogLevel logLevel;

            if (!Enum.TryParse<LogLevel>(settings["LOG_LEVEL"], ignoreCase: true, out logLevel))
            {
                logLevel = LogLevel.Info;
            }

            return new TestConfiguration(
                settings["SCHEME"],
                settings["HOST"],
                System.Convert.ToInt32(settings["PORT"]),
                settings["SERVER_KEY"],
                System.Convert.ToBoolean(settings["STDOUT"]),
                logLevel);
        }
    }
}
