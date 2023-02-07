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

using Microsoft.Extensions.Configuration;

namespace Nakama.Tests
{
    internal static class TestsUtil
    {
        public const int TIMEOUT_MILLISECONDS = 5000;
        public const int TIMEOUT_MILLISECONDS_MATCHMAKER = 15000;

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
            var settings = new ConfigurationBuilder().AddJsonFile(path).Build();
            var port = System.Convert.ToInt32(settings["PORT"]);
            var client = new Client(settings["SCHEME"], settings["HOST"], port, settings["SERVER_KEY"], adapter);
            if (System.Convert.ToBoolean(settings["STDOUT"]))
            {
                client.Logger = new StdoutLogger();
            }

            return client;
        }
    }
}
