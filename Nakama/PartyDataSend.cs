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

using System.Runtime.Serialization;

namespace Nakama
{
    /// <summary>
    /// Send data to a party.
    /// </summary>
    internal class PartyDataSend
    {
      [DataMember(Name = "party_id"), Preserve]
      public string PartyId { get; set; }

      [DataMember(Name = "op_code"), Preserve]
      public string OpCode { get; set; }

      [DataMember(Name = "data"), Preserve]
      public string Data { get; set; }

      public override string ToString() => $"PartyDataSend(PartyId='{PartyId}', OpCode={OpCode}, Data='{Data}')";
    }
}
