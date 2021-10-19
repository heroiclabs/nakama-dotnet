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

using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nakama.Tests
{
    /// <summary>
    /// An adapter which throws transient/retryable exceptions when a socket communicates with the server.
    /// </summary>
    public class TransientExceptionSocketAdapter : ISocketAdapter
    {
        public class NetworkSchedule
        {
            public TransientAdapterResponseType[] ConnectResponses { get; }
            public List<Tuple<TimeSpan, TransientAdapterResponseType>> PostConnect { get; }

            public NetworkSchedule(TransientAdapterResponseType[] connectResponses)
            {
                ConnectResponses = connectResponses;
                PostConnect = new List<Tuple<TimeSpan, TransientAdapterResponseType>>();
            }

            public NetworkSchedule(TransientAdapterResponseType[] connect, List<Tuple<TimeSpan, TransientAdapterResponseType>> postConnect)
            {
                ConnectResponses = connect;
                PostConnect = postConnect;
            }
        }

        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }

        public event Action Connected;
        public event Action Closed;

        public event Action<Exception> ReceivedError;
        public event Action<ArraySegment<byte>> Received;

        private NetworkSchedule _schedule;
        private int _consecutiveConnectAttempts;

        public TransientExceptionSocketAdapter(NetworkSchedule schedule)
        {
            _schedule = schedule;
        }

        public void Close()
        {
            _consecutiveConnectAttempts = 0;
            Closed?.Invoke();
        }

        public async void Connect(Uri uri, int timeout, CancellationTokenSource canceller)
        {
            canceller?.Token.ThrowIfCancellationRequested();

            if (_schedule.ConnectResponses[_consecutiveConnectAttempts] == TransientAdapterResponseType.ServerOk)
            {
                _consecutiveConnectAttempts++;
                IsConnected = true;
                Connected?.Invoke();
            }
            else
            {
                _consecutiveConnectAttempts++;
                ReceivedError?.Invoke(new System.IO.IOException("Connect exception."));
            }

            foreach (var postConnect in _schedule.PostConnect)
            {
                await Task.Delay(postConnect.Item1);

                if (postConnect.Item2 == TransientAdapterResponseType.TransientError)
                {
                    IsConnected = false;
                    ReceivedError?.Invoke(new System.IO.IOException("Post connect exception."));
                    Close();
                }
            }
        }

        public void Dispose() {}

        public void Send(ArraySegment<byte> buffer, CancellationToken cancellationToken, bool reliable = true)
        {
            throw new NotImplementedException("This adapter cannot send messages.");
        }
    }
}
