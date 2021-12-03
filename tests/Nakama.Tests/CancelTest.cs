/**
* Copyright 2021 The Nakama Authors
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

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests
{
    public class CancelTest
    {
        [Fact]
        public async void TestBasicCancel()
        {
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath);

            var canceller = new CancellationTokenSource();

            Task<ISession> authTask = client.AuthenticateCustomAsync("test_id", null, true, null, null, canceller.Token);
            canceller.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await authTask);
        }

        [Fact]
        public async void TestCancelDuringBackoff()
        {
            var adapterSchedule = new TransientAdapterResponseType[3] {
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
                TransientAdapterResponseType.TransientError,
            };

            var adapter = new TransientExceptionHttpAdapter(adapterSchedule);

            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            var canceller = new CancellationTokenSource();

            RetryListener retryListener = (int numRetry, Retry retry) => {
                canceller.Cancel();
            };


            Task<ISession> authTask = client.AuthenticateCustomAsync("test_id", null, true, null, new RetryConfiguration(100, 2, retryListener), canceller.Token);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await authTask);
        }
    }
}
