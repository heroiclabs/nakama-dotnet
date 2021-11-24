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

namespace NakamaSync
{
    /// <summary>
    /// A data-transfer object for a sync var.
    /// </summary>
    internal interface ISerializableVar<T> : ISerializableVar
    {
        T Value { get; }
    }

    /// <summary>
    /// A data-transfer object for a sync var.
    /// </summary>
    internal interface ISerializableVar
    {
        int LockVersion { get; set; }
        ValidationStatus Status { get; set; }
        AckType AckType { get; set; }
    }
}