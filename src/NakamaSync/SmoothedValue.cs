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

namespace NakamaSync
{
    internal struct RawValue
    {
        public float Value { get; }
        public DateTime Time { get; }

        public RawValue(float value, DateTime time)
        {
            Value = value;
            Time = time;
        }
    }

    internal struct SmoothedValue
    {
        public float RawValue { get; }
        public float Value { get; }
        public DateTime Time { get; }

        public SmoothedValue(float value, float smoothedValue, DateTime time)
        {
            RawValue = value;
            Value = value;
            Time = time;
        }
    }
}
