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
    using System;

    /// <summary>
    /// A group of options used to configure the <c>ClientWebSocketListener</c>.
    /// </summary>
    public sealed class WebSocketOptions
    {
        /// <summary>
        /// The time to wait for the WebSocket close event. Defaults to 5 seconds.
        /// </summary>
        public TimeSpan CloseTimeout { get; set; }

        /// <summary>
        /// A logger which can output log messages. Defaults to <c>NullLogger</c>.
        /// </summary>
        public ILogger Logger { get; set; }

        public WebSocketOptions()
        {
            CloseTimeout = TimeSpan.FromSeconds(5);
            Logger = NullLogger.Instance; // dont log by default.
        }

        internal void ValidateOptions()
        {
            if (CloseTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("CloseTimeout must be greater than 0.");
            }
        }

        internal WebSocketOptions Clone()
        {
            return new WebSocketOptions
            {
                CloseTimeout = CloseTimeout,
                Logger = Logger
            };
        }
    }
}
