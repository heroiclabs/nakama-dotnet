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
    public delegate bool ValidationHandler<T>(IUserPresence source, ValueChange<T> change);

    public abstract class Var<T> : IVar
    {
        public bool RequiresValidation
        {
            get;
            protected set;
        }

        internal IUserPresence Self
        {
            get;
            set;
        }

        IUserPresence IVar.Self
        {
            get => Self;
            set => Self = value;
        }

        public ValidationStatus ValidationStatus
        {
            get;
            internal set;
        }

        protected ValidationStatus _validationStatus;
        protected T _value;
        protected ValidationHandler<T> _validationHandler;

        public T GetValue()
        {
            return _value;
        }

        public void MarkRequiresValidation(ValidationHandler<T> validationHandler)
        {
            _validationHandler = validationHandler;
        }

        internal bool InvokeValidationHandler(IUserPresence source, ValueChange<T> change)
        {
            return _validationHandler(source, change);
        }

        internal abstract void Reset();
    }
}