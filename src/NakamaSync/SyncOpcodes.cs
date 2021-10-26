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
    public struct SyncOpcodes
    {
        public int HandshakeRequestOpcode { get; }
        public int HandshakeResponseOpcode { get; }
        public int DataOpcode { get; }
        public int RpcOpcode { get;}

        public SyncOpcodes(int handshakeRequestOpcode, int handshakeResponseOpcode, int dataOpcode, int rpcOpcode)
        {
            HashSet<int> allCodes = new HashSet<int>();

            if (!allCodes.Add(handshakeRequestOpcode) ||
                !allCodes.Add(handshakeResponseOpcode) ||
                !allCodes.Add(dataOpcode) ||
                !allCodes.Add(rpcOpcode))
            {
                throw new ArgumentException("Each opcode must be a unique integer.");
            }

            HandshakeRequestOpcode = handshakeRequestOpcode;
            HandshakeResponseOpcode = handshakeResponseOpcode;
            DataOpcode = dataOpcode;
            RpcOpcode = rpcOpcode;
        }
    }
}
