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
    public interface IValidationChange
    {
        ValidationStatus OldStatus { get; }
        ValidationStatus NewStatus { get; }
    }

    public class ValidationChange : IValidationChange
    {
        public ValidationStatus OldStatus { get; }
        public ValidationStatus NewStatus { get; }

        public ValidationChange(ValidationStatus oldStatus, ValidationStatus newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}
