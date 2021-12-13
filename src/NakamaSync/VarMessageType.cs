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
    internal enum VarMessageType
    {
        None = 1,
        // other client acknowledging new client.
        HandshakeResponse = 2,
        // host responding to var requiring validation.
        ValidationStatus = 3,
        // new client requesting initial state from existing clients.
        HandshakeRequest = 4,
        // version conflict due to an unobserved write.
        VersionConflict = 5,
    }
}