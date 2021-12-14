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
    internal class LocalValues<T>
    {
        public VarValue<T> CurrentValue { get; private set; }
        public VarValue<T> LastValidValue { get; private set; }
        public VarValue<T> LastValue { get; private set; }

        public LocalValues()
        {
            CurrentValue = new VarValue<T>();
            LastValidValue = new VarValue<T>();
            LastValue = new VarValue<T>();
        }

        public void SetNew(VarValue<T> newValue)
        {
            LastValue = CurrentValue;

            if (LastValue.CanMerge(newValue.Value))
            {
                newValue.Merge(LastValue.Value);
            }

            CurrentValue = newValue;

            // last valid value can have ValidationStatus.None in the case that a handler
            // wasn't added to the variable at the time the value was received.
            // todo we could also force the validation handler to immutable and passed via constructor
            // to avoid this type of edge case.
            if (newValue.ValidationStatus == ValidationStatus.Valid || newValue.ValidationStatus == ValidationStatus.None)
            {
                LastValue = newValue;
            }
        }

        public void Reset()
        {
            CurrentValue = new VarValue<T>();
            LastValidValue = new VarValue<T>();
            LastValue = new VarValue<T>();
        }

        public void Rollback()
        {
            LastValue = CurrentValue;
            CurrentValue = LastValidValue;
        }
    }
}