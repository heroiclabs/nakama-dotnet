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

    public abstract class Var<T> : IVar<T>
    {
        public ValidationHandler<T> ValidationHandler { get; set; }

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

        public bool IsHost
        {
            get
            {
                return (this as IVar).IsHost;
            }
            protected set
            {
                (this as IVar).IsHost = value;
            }
        }


        bool IVar.IsHost
        {
            get;
            set;
        }

        internal ILogger Logger
        {
            get;
            set;
        }

        ILogger IVar.Logger
        {
            get => Logger;
            set => Logger = value;
        }

        protected ValidationStatus _validationStatus;
        protected T _value;

        public T GetValue()
        {
            return _value;
        }

        internal abstract void Reset();

        void IVar.Reset()
        {
            this.Reset();
        }
    }
}
