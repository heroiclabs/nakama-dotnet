
/**
 * Copyright 2020 The Nakama Authors
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

using System.Collections.Generic;

namespace Nakama
{
    /// <summary>
    /// Extension methods for the <see cref="IHttpAdapter"> interface.
    /// </summary>
    public static class IHttpAdapterExt
    {
        /// <summary>
        /// Performs an in-place copy of keys and values from Nakama's error response dictionary into
        /// the data dictionary of an <see cref="ApiResponseException"/>.
        /// <param name="adapter">The adapter receiving the error response.</param>
        /// <param name="decodedResponse"> The decoded error response from the server.</param>
        /// <param name="e"> The exception whose data dictionary is being written to.</param>
        /// </summary>
        public static void CopyErrorDictionary(this IHttpAdapter adapter, Dictionary<string, object> decodedResponse, ApiResponseException e)
        {
            var errDict = decodedResponse["error"] as Dictionary<string, object>;

            foreach (KeyValuePair<string, object> keyVal in errDict)
            {
                e.Data[keyVal.Key] = keyVal.Value;
            }
        }
    }
}
