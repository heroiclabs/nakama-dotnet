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

using Xunit;

namespace Satori.Tests
{
    public class SatoriTest
    {
        public const int _TIMEOUT_MILLISECONDS = 5000;

        private readonly Client _testClient = new Client("http", "localhost", 7450, "", Nakama.HttpRequestAdapter.WithGzip());

        [Fact(Timeout = _TIMEOUT_MILLISECONDS)]
        public void TestAuthenticate()
        {
            _testClient.AuthenticateAsync()
        }
    }
}
