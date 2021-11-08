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
using Nakama;

namespace NakamaSync
{
    internal interface IVar
    {
        event ResetHandler OnReset;

        HostTracker HostTracker { get; set; }

        ValidationStatus ValidationStatus
        {
            get;
        }

        string Key
        {
            get;
        }

        IUserPresence Self
        {
            get;
            set;
        }

        ILogger Logger
        {
            get;
            set;
        }

        void Reset();

        void SetValidationStatus(ValidationStatus status);
    }

    internal interface IVar<T> : IVar
    {
        event Action<IVarEvent<T>> OnValueChanged;

        T GetValue();

        void SetValue(IUserPresence source, T value);
    }

    public interface IVarEvent<T>
    {
        IUserPresence Source { get; }
        IValueChange<T> ValueChange { get; }
        IValidationChange ValidationChange { get; }
    }
}
