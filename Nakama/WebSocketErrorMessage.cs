/**
 * Copyright 2018 The Nakama Authors
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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Nakama
{
    /// <summary>
    /// A logical error received on the WebSocket connection.
    /// </summary>
    internal class WebSocketErrorMessage
    {
        [DataMember(Name = "code"), Preserve] public int Code { get; set; }

        [DataMember(Name = "context"), Preserve] public Dictionary<string, string> Context { get; set; }

        [DataMember(Name = "message"), Preserve] public string Message { get; set; }

        public override string ToString()
        {
            return $"WebSocketErrorMessage(Code={Code}, Context={Context}, Message='{Message}')";
        }
    }
}
