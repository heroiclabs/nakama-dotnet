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
    /// A logger which writes log messages from the client.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log a message object with the DEBUG level.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        void Debug(object message);

        /// <summary>
        /// Logs a formatted string with the DEBUG level.
        /// </summary>
        /// <param name="format">A string with zero or more format items.</param>
        /// <param name="args">An object array with zero or more objects to format.</param>
        void DebugFormat(string format, params object[] args);

        /// <summary>
        /// Log a message object with the INFO level.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        void Info(object message);

        /// <summary>
        /// Logs a formatted string with the INFO level.
        /// </summary>
        /// <param name="format">A string with zero or more format items.</param>
        /// <param name="args">An object array with zero or more objects to format.</param>
        void InfoFormat(string format, params object[] args);

        /// <summary>
        /// Log a message object with the WARN level.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        void Warn(object message);

        /// <summary>
        /// Logs a formatted string with the WARN level.
        /// </summary>
        /// <param name="format">A string with zero or more format items.</param>
        /// <param name="args">An object array with zero or more objects to format.</param>
        void WarnFormat(string format, params object[] args);

        /// <summary>
        /// Log a message object with the ERROR level.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        void Error(object message);

        /// <summary>
        /// Logs a formatted string with the ERROR level.
        /// </summary>
        /// <param name="format">A string with zero or more format items.</param>
        /// <param name="args">An object array with zero or more objects to format.</param>
        void ErrorFormat(string format, params object[] args);
    }
}
