/**
 * Copyright 2020 The Nakama Authors
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

using System.Runtime.Serialization;

namespace Nakama.SocketInternal
{
    /// <summary>
    /// A Protobuf-Net serializable class corresponding to the well-known
    /// google.protobuf.IntValue type.
    ///
    /// Keep in mind that grpc-gateway will automatically deserialize a vool into a IntValue;
    /// there is no need to for the client to utilize this for JSON.
    /// </summary>
    [DataContract]
    public struct IntValue
    {
        public bool HasValue => _nullable.HasValue;
        public int Value => _nullable.Value;

        [DataMember(Order = 1), Preserve]
        private int? _nullable { get; set; }

        public override string ToString()
        {
            return $"IntValue(Value='{_nullable}')";
        }

    }
}
