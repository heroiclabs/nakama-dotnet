// Copyright 2018 The Nakama Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Nakama
{
    /// <summary>
    /// A simple logger to write log messages to an output sink.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a formatted string with the ERROR level.
        /// </summary>
        /// <param name="format">A string with zero or more format items.</param>
        /// <param name="args">An object array with zero or more objects to format.</param>
        void ErrorFormat(string format, params object[] args);

        /// <summary>
        /// Logs a formatted string with the INFO level.
        /// </summary>
        /// <param name="format">A string with zero or more format items.</param>
        /// <param name="args">An object array with zero or more objects to format.</param>
        void InfoFormat(string format, params object[] args);

        /// <summary>
        /// Logs a formatted string with the WARN level.
        /// </summary>
        /// <param name="format">A string with zero or more format items.</param>
        /// <param name="args">An object array with zero or more objects to format.</param>
        void WarnFormat(string format, params object[] args);
    }
}
