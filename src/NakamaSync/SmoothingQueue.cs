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

namespace NakamaSync
{
    public delegate float SmoothingFunction(float value);

    internal class SmoothingQueue
    {
        private Queue<SmoothedValue> _bufferedValues;
        private readonly SmoothingFunction _fn;
        private readonly int _timeBuffer;

        public SmoothingQueue(SmoothingFunction fn, int timeBufferMs)
        {
            _bufferedValues = new Queue<SmoothedValue>();

            if (fn == null)
            {
                throw new ArgumentException("Cannot have null smoothing function");
            }

            if (timeBufferMs <= 0)
            {
                throw new ArgumentException("Must have positive time buffer");
            }

            _fn = fn;
            _timeBuffer = timeBufferMs;
        }

        public void Enqueue(SmoothedValue delta)
        {
            _bufferedValues.Enqueue(delta);
        }

        public float GetSmoothedValue()
        {
            IEnumerable<SmoothedValue> valuesToSmooth = GetValues();
            var smoothedValues = new List<float>();

            foreach (SmoothedValue value in valuesToSmooth)
            {
                smoothedValues.Add(_fn(value.RawValue));
            }

            return smoothedValues;
        }

        private IEnumerable<SmoothedValue> GetValues()
        {
            var values = new Queue<SmoothedValue>();

            DateTime now = DateTime.UtcNow;
            DateTime adjustedNow = now.AddMilliseconds(-_timeBuffer);

            while (_bufferedValues.Count > 0)
            {
                SmoothedValue value = _bufferedValues.Dequeue();

                if (value.Time >= adjustedNow)
                {
                    values.Enqueue(value);
                }
            }

            // we don't care about buffering past values our time offset
            _bufferedValues = values;

            return _bufferedValues;
        }
    }
}
