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
    // todo smooth var and group vars aren't quite similar to presence and self vars...
    // should these be distinct concepts? self, presence, and shared refer to permission model.
    // smooth and group refer to...other concepts.
    public class SmoothVar
    {
        public event Action<IVarEvent<float>> OnValueChanged;

        private readonly SmoothingQueue _smoothingQueue;
        private readonly Var<float> _var;
        private readonly int _bufferSize;

        public SmoothVar(Var<float> var, SmoothingFunction fn, int timeBufferMs)
        {
            _var = var;
            _bufferSize = timeBufferMs;
            _smoothingQueue = new SmoothingQueue(fn, timeBufferMs);
            var.OnValueChanged += HandleValueChanged;
        }

        private void HandleValueChanged(IVarEvent<float> evt)
        {
            double timeDifference = _var.SyncMatch.GetElapsedTimeMs() - evt.SendTimeMs;


            // todo what if invalid?


            _smoothingQueue.Enqueue(new SmoothedValue(evt.ValueChange.NewValue, DateTime.UtcNow));

            OnValueChanged?.Invoke(_smoothingQueue.GetSmoothedValues());
        }

        private class SmoothingDelta
        {
            public IValueChange<float> Delta { get; }
            public DateTime Time { get; }

            public SmoothingDelta(IValueChange<float> delta, DateTime time)
            {
                Delta = delta;
                Time = time;
            }
        }
    }
}
