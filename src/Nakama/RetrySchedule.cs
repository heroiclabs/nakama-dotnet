/*
 * Copyright 2021 Heroic Labs
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
using System.Threading;

namespace Nakama
{
    internal class RetrySchedule
    {
        public RetryConfiguration Configuration { get; }
        public int? OriginTask { get; private set; }
        public List<Retry> Retries { get; }
        public CancellationTokenSource RetryTokenSource { get; }

        public RetrySchedule(RetryConfiguration configuration, CancellationTokenSource source)
        {
            Configuration = configuration;
            Retries = new List<Retry>();
            RetryTokenSource = source;
        }

        public void SetOriginTask(int id)
        {
            if (OriginTask.HasValue)
            {
                throw new InvalidOperationException("Cannot set a new request task id once it is set.");
            }

            OriginTask = id;
        }
    }
}
