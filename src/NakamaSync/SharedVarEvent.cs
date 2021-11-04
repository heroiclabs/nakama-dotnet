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

using Nakama;

namespace NakamaSync
{
    public class SharedVarEvent<T> : ISharedVarEvent<T>
    {
        public IUserPresence Source { get; }
        public SharedVar<T> Var { get; }
        public IValueChange<T> ValueChange { get; }
        public IValidationChange ValidationChange { get; }

        internal SharedVarEvent(IUserPresence source, SharedVar<T> var, ValueChange<T> valueChange, ValidationChange validationChange)
        {
            Source = source;
            Var = var;
            ValueChange = valueChange;
            ValidationChange = validationChange;
        }
    }
}
