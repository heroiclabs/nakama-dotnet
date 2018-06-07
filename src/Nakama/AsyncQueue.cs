/**
 * Copyright 2018 The Nakama Authors
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

namespace Nakama
{
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    internal class AsyncQueue<T>
    {
        private readonly ConcurrentQueue<T> _bufferQueue;
        private readonly ConcurrentQueue<TaskCompletionSource<T>> _completerQueue;
        private readonly object _lock = new object();

        public AsyncQueue()
        {
            _bufferQueue = new ConcurrentQueue<T>();
            _completerQueue = new ConcurrentQueue<TaskCompletionSource<T>>();
        }

        public void Enqueue(T item)
        {
            TaskCompletionSource<T> completer;
            do
            {
                if (_completerQueue.TryDequeue(out completer) && !completer.Task.IsCanceled &&
                    completer.TrySetResult(item))
                {
                    return;
                }
            } while (completer != null);

            lock (_lock)
            {
                if (_completerQueue.TryDequeue(out completer) && !completer.Task.IsCanceled &&
                    completer.TrySetResult(item))
                {
                    return;
                }

                _bufferQueue.Enqueue(item);
            }
        }

        public Task<T> DequeueAsync(CancellationToken ct)
        {
            lock (_lock)
            {
                T item;
                if (_bufferQueue.TryDequeue(out item)) return Task.FromResult(item);
                var completer = new TaskCompletionSource<T>();
                ct.Register(() => completer.TrySetCanceled());
                _completerQueue.Enqueue(completer);
                return completer.Task;
            }
        }
    }
}
