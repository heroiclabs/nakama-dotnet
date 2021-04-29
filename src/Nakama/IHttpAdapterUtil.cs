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
    /// Utility methods for the <see cref="IHttpAdapter"> interface.
    /// NOTE: DO NOT USE EXTENSION METHODS as Unity cannot cross-compile
    /// them properly to WebGL.
    /// </summary>
    public static class IHttpAdapterUtil
    {
        /// <summary>
        /// Performs an in-place copy of data from Nakama's error response into
        /// the data dictionary of an <see cref="Nakama.ApiResponseException"/>.
        /// </summary>
        /// <param name="adapter">The adapter receiving the error response.</param>
        /// <param name="err"> The decoded error field from the server response.</param>
        /// <param name="e"> The exception whose data dictionary is being written to.</param>
        public static void CopyResponseError(IHttpAdapter adapter, object err, ApiResponseException e)
        {
            var errString = err as string;
            var errDict = err as Dictionary<string, object>;

            if (errString != null)
            {
                e.Data["error"] = err;
            }
            else if (errDict != null)
            {
                foreach (KeyValuePair<string, object> keyVal in errDict)
                {
                    e.Data[keyVal.Key] = keyVal.Value;
                }
            }
        }
    }
}
