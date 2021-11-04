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
    internal abstract class ProxyVar<TVar, K> : IVar<object> where TVar : IVar<K> where K : class
    {
        public event ResetHandler OnReset
        {
            add
            {
                _proxyTarget.OnReset += value;
            }

            remove
            {
                _proxyTarget.OnReset -= value;

            }
        }

        public ValidationHandler<K> ValidationHandler
        {
            get => _proxyTarget.ValidationHandler;
            set => _proxyTarget.ValidationHandler = value;
        }

        ValidationHandler<object> IVar<object>.ValidationHandler
        {
            get
            {
                return (source, change) =>
                {
                    var kChange = new ValueChange<K>(change.OldValue as K, change.NewValue as K);
                    return this.ValidationHandler(source, kChange);
                };
            }
            set
            {
                _proxyTarget.ValidationHandler = (source, change) =>
                {
                    var kChange = new ValueChange<K>(change.OldValue as K, change.NewValue as K);
                    return this.ValidationHandler(source, kChange);
                };
            }
        }

        protected TVar _proxyTarget;

        internal ProxyVar(TVar proxyTarget)
        {
            _proxyTarget = proxyTarget;
        }

        internal IUserPresence Self
        {
            get => _proxyTarget.Self;
            set => _proxyTarget.Self = value;
        }

        IUserPresence IVar.Self
        {
            get => Self;
            set => Self = value;
        }

        public ValidationStatus ValidationStatus
        {
            get => _proxyTarget.ValidationStatus;
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
            get => _proxyTarget.IsHost;
            set => _proxyTarget.IsHost = value;
        }

        internal ILogger Logger
        {
            get => _proxyTarget.Logger;
            set => _proxyTarget.Logger = value;
        }

        ILogger IVar.Logger
        {
            get => Logger;
            set => Logger = value;
        }

        public object GetValue()
        {
            return _proxyTarget.GetValue();
        }

        internal void Reset()
        {
            _proxyTarget.Reset();
        }

        void IVar.Reset()
        {
            this.Reset();
        }

        public void SetValidationStatus(ValidationStatus status)
        {
            this._proxyTarget.SetValidationStatus(status);
        }
    }
}
