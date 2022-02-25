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
    /// An identifier for a storage object.
    /// </summary>
    /// <inheritdoc cref="IApiReadStorageObjectId"/>
    public class StorageObjectId : IApiReadStorageObjectId, IApiDeleteStorageObjectId
    {
        /// <inheritdoc cref="IApiReadStorageObjectId" />
        public string Collection { get; set; }

        /// <inheritdoc cref="IApiReadStorageObjectId" />
        public string Key { get; set; }

        /// <inheritdoc />
        public string Version { get; set; }

        /// <inheritdoc />
        public string UserId { get; set; }

        public override string ToString()
        {
            return $"StorageObjectId(Collection='{Collection}', Key='{Key}', Version='{Version}', UserId='{UserId}')";
        }
    }
}
