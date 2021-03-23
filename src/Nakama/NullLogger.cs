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
    /// <summary>
    /// A logger which writes to nowhere.
    /// </summary>
    internal class NullLogger : ILogger
    {
        public static readonly ILogger Instance = new NullLogger();

        private NullLogger()
        {
        }

        /// <inheritdoc cref="ILogger.ErrorFormat"/>
        public void ErrorFormat(string format, params object[] args)
        {
        }

        /// <inheritdoc cref="ILogger.InfoFormat"/>
        public void InfoFormat(string format, params object[] args)
        {
        }

        /// <inheritdoc cref="ILogger.WarnFormat"/>
        public void WarnFormat(string format, params object[] args)
        {
        }
    }
}
