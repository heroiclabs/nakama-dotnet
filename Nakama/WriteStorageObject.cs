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
    /// <inheritdoc cref="IApiWriteStorageObject"/>
    public class WriteStorageObject : IApiWriteStorageObject
    {
        /// <inheritdoc cref="Collection"/>
        public string Collection { get; set; }

        /// <inheritdoc cref="Key"/>
        public string Key { get; set; }

        /// <inheritdoc cref="PermissionRead"/>
        public int PermissionRead { get; set; } = 1;

        /// <inheritdoc cref="PermissionWrite"/>
        public int PermissionWrite { get; set; } = 1;

        /// <inheritdoc cref="Value"/>
        public string Value { get; set; }

        /// <inheritdoc cref="Version"/>
        public string Version { get; set; }
    }
}
