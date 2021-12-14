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
using System.Linq;
using System.Collections;
using Nakama;

namespace NakamaSync
{
    internal class VarValue<T> : IVarValue<T>
    {
        public int Version { get; private set; }
        public UserPresence Writer { get; private set; }
        public ValidationStatus ValidationStatus { get; private set; }
        public T Value { get; private set; }

        IUserPresence IVarValue<T>.Writer => Writer;

        public VarValue()
        {
            Version = 0;
            Writer = null;
            ValidationStatus = ValidationStatus.None;
            Value = default(T);
        }

        public bool CanMergeWith(T otherValue)
        {
            // todo refactor this spaghetti method
            if (typeof(IDictionary).IsAssignableFrom(typeof(T)))
            {
                IDictionary otherValueAsDict = (IDictionary) otherValue;
                IDictionary valueAsDict = (IDictionary) Value;

                if (otherValueAsDict == null || valueAsDict == null)
                {
                    return false;
                }

                foreach (var key in valueAsDict.Keys)
                {
                    if (otherValueAsDict.Contains(key))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public VarValue(int version, UserPresence writer, ValidationStatus status, T value)
        {
            Version = version;
            Writer = writer;
            ValidationStatus = status;

            if ((typeof(IDictionary).IsAssignableFrom(typeof(T))))
            {
                Value = MergeDictionary((IDictionary) Value, (IDictionary) value);
            }
            else
            {
                Value = value;
            }
        }

        private T MergeDictionary(IDictionary existingDict, IDictionary incomingDict)
        {
            var newDictionary = (IDictionary) Activator.CreateInstance(typeof(T));

            var existingDictionary = (IDictionary) Value;
            var incomingDictionary = (IDictionary) incomingDict;

            if (existingDictionary != null)
            {
                foreach (var key in existingDictionary.Keys)
                {
                    System.Console.WriteLine("adding key from existing " + key);
                    newDictionary[key] = existingDictionary[key];
                }
            }

            if (incomingDictionary != null)
            {
                foreach (var key in incomingDictionary.Keys)
                {
                    System.Console.WriteLine($"user adding key from incoming " + key);
                    newDictionary[key] = incomingDictionary[key];
                }
            }

            return (T) newDictionary;
        }
    }
}
