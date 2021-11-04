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
using System.Collections.Generic;
using Nakama;

namespace NakamaSync
{
    /// <summary>
    /// A variable whose single value is synchronized across all clients connected to the same match.
    /// TODO implement an ownership model?
    /// </summary>
    internal class ProxySharedVar<T> : ProxyVar<SharedVar<T>, T>, ISharedVar<object> where T : class
    {
        public event Action<ISharedVarEvent<object>> OnValueChanged;

        public ProxySharedVar(SharedVar<T> proxyTarget) : base(proxyTarget)
        {
            proxyTarget.OnValueChanged += evt =>
            {
                var newValueChange = new ValueChange<object>(evt.ValueChange.OldValue, evt.ValueChange.NewValue);
                var newEvt = new SharedVarEvent<object>(evt.Source, newValueChange, evt.ValidationChange);
                this.OnValueChanged?.Invoke(newEvt);
            };

            proxyTarget.OnReset += Reset;
        }

        public void SetValue(object value)
        {
            this._proxyTarget.SetValue(value as T);
        }

        void ISharedVar<object>.SetValue(IUserPresence source, object value, ValidationStatus validationStatus)
        {
            (this._proxyTarget as ISharedVar<T>).SetValue(source, value as T, validationStatus);
        }
    }

    internal class ProxySharedVarDictionary<T> : ProxyVar<SharedVar<IDictionary<string, T>>, IDictionary<string, T>>, ISharedVar<object>
    {
        public event Action<ISharedVarEvent<object>> OnValueChanged;

        public ProxySharedVarDictionary(SharedVar<IDictionary<string, T>> proxyTarget) : base(proxyTarget)
        {
            proxyTarget.OnValueChanged += evt =>
            {
                var newValueChange = new ValueChange<object>(evt.ValueChange.OldValue, evt.ValueChange.NewValue);
                var newEvt = new SharedVarEvent<object>(evt.Source, newValueChange, evt.ValidationChange);
                this.OnValueChanged?.Invoke(newEvt);
            };

            proxyTarget.OnReset += Reset;
        }

        public void SetValue(object value)
        {
            (this as ISharedVar<object>).SetValue(Self, value, ValidationStatus);
        }

        void ISharedVar<object>.SetValue(IUserPresence source, object value, ValidationStatus validationStatus)
        {
            if (value != null)
            {
                // todo the actual value coming into this method here depends on serializer
                var asSerializedDict = (IDictionary<string, object>) value;
                var obj = new Dictionary<string, T>();

                foreach (var item in asSerializedDict)
                {
                    obj[item.Key] = (T) item.Value;
                }

                (this._proxyTarget as ISharedVar<IDictionary<string, T>>).SetValue(source, obj, validationStatus);
            }
            else
            {
                (this._proxyTarget as ISharedVar<IDictionary<string, T>>).SetValue(source, null, validationStatus);
            }
        }
    }
}
