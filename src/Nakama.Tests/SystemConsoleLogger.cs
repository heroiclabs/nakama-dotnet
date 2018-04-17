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

namespace Nakama.Tests
{
    /// <summary>
    /// A logger which writes messages to the <c>System.Console</c>.
    /// </summary>
    public class SystemConsoleLogger : ILogger
    {
        /// <inheritdoc />
        public void Debug(object message)
        {
            System.Console.WriteLine("[DEBUG] {0}", message);
        }

        /// <inheritdoc />
        public void DebugFormat(string format, params object[] args)
        {
            System.Console.WriteLine("[DEBUG] " + format, args);
        }

        /// <inheritdoc />
        public void Info(object message)
        {
            System.Console.WriteLine("[INFO] {0}", message);
        }

        /// <inheritdoc />
        public void InfoFormat(string format, params object[] args)
        {
            System.Console.WriteLine("[INFO] " + format, args);
        }

        /// <inheritdoc />
        public void Warn(object message)
        {
            System.Console.WriteLine("[WARN] {0}", message);
        }

        /// <inheritdoc />
        public void WarnFormat(string format, params object[] args)
        {
            System.Console.WriteLine("[WARN] " + format, args);
        }

        /// <inheritdoc />
        public void Error(object message)
        {
            System.Console.WriteLine("[ERROR] {0}", message);
        }

        /// <inheritdoc />
        public void ErrorFormat(string format, params object[] args)
        {
            System.Console.WriteLine("[ERROR] " + format, args);
        }
    }
}
