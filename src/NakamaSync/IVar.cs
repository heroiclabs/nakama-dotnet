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

        bool IsHost
        {
            get;
            set;
        }

        void Reset();

        void SetValidationStatus(ValidationStatus status);
    }

    internal interface IIncomingVar<out T> : IVar
    {
        event Action<IVarEvent<T>> OnValueChanged;

        T GetValue();

        ValidationStatus ValidationStatus
        {
            get;
        }

        bool SetValue(IUserPresence source, object value, ValidationStatus validationStatus);
    }

    internal interface IIOutgoingVar<T> : IVar
    {
        ValidationHandler<T> ValidationHandler { get; set; }

    }

    public interface IVarEvent<out T>
    {
        IUserPresence Source { get; }
        IValueChange<T> ValueChange { get; }
        IValidationChange ValidationChange { get; }
    }
}
