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
using System.Collections.Generic;

namespace NakamaSync
{
    public class SyncOpcodes
    {
        public int HandshakeRequest { get; }
        public int HandshakeResponse { get; }
        public int Data { get; }
        public int Rpc { get;}

        public SyncOpcodes(int handshakeRequest, int handshakeResponse, int data, int rpc)
        {
            HashSet<int> allCodes = new HashSet<int>();

            if (!allCodes.Add(handshakeRequest) ||
                !allCodes.Add(handshakeResponse) ||
                !allCodes.Add(data) ||
                !allCodes.Add(rpc))
            {
                throw new ArgumentException("Each opcode must be a unique integer.");
            }

            HandshakeRequest = handshakeRequest;
            HandshakeResponse = handshakeResponse;
            Data = data;
            Rpc = rpc;
        }
    }
}
