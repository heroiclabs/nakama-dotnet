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
    /// A logger which proxies log messages from <c>vtortola.WebSockets.ILogger</c>.
    /// </summary>
    internal class WebSocketLogger : vtortola.WebSockets.ILogger
    {
        /// <inheritdoc />
        public bool IsDebugEnabled { get; }

        /// <inheritdoc />
        public bool IsWarningEnabled { get; }

        /// <inheritdoc />
        public bool IsErrorEnabled { get; }

        private readonly ILogger _logger;

        public WebSocketLogger(ILogger logger, bool trace)
        {
            IsDebugEnabled = trace;
            IsWarningEnabled = trace;
            IsErrorEnabled = trace;
            _logger = logger;
        }

        /// <inheritdoc />
        public void Debug(string message, Exception error = null)
        {
            _logger.DebugFormat(message, error);
        }

        /// <inheritdoc />
        public void Warning(string message, Exception error = null)
        {
            _logger.WarnFormat(message, error);
        }

        /// <inheritdoc />
        public void Error(string message, Exception error = null)
        {
            _logger.ErrorFormat(message, error);
        }
    }
}
