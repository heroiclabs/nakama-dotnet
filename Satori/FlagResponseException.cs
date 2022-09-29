// Copyright 2022 The Satori Authors
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

using System.Collections.Generic;

namespace Satori
{
    /// <summary>
    /// A special type of network exception that provides feature flags for the user
    /// in the case of a temporarily offline client.
    /// This inherits from <see cref="ApiResponseException"/> so that users may catch it like any other
    /// network exception but still access the offline flags they supplied in their original request.
    /// </summary>
    public class FlagResponseException : ApiResponseException
    {
        public IApiFlagList OfflineFlagList { get; }

        internal FlagResponseException(long statusCode, string content, int grpcCode, IEnumerable<FlagRequest> flagRequests) : base(statusCode, content, grpcCode)
        {
            var apiFlagList = new ApiFlagList();
            apiFlagList._flags = new List<ApiFlag>();

            foreach (var flagRequest in flagRequests)
            {
                apiFlagList._flags.Add(new ApiFlag(){Name = flagRequest.Name, Value = flagRequest.OfflineValue, ConditionChanged = false});
            }

            OfflineFlagList = apiFlagList;
        }
    }
}
