// Copyright 2023 The Nakama Authors
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

namespace Nakama
{
    internal static class PresenceUtil
    {
        /// <summary>
        /// Performs an in-place update of a presence list with the joins and leaves of a presence event.
        /// </summary>
        public static void UpdatePresences(List<UserPresence> currentPresences, IEnumerable<IUserPresence> joins, IEnumerable<IUserPresence> leaves)
        {
            var newPresences = new Dictionary<string, UserPresence>();

            foreach (UserPresence presence in currentPresences)
            {
                newPresences[presence.UserId] = presence;
            }

            foreach (UserPresence join in joins)
            {
                if (newPresences.ContainsKey(join.UserId))
                {
                    // unexpected
                    continue;
                }

                newPresences.Add(join.UserId, join);
            }

            foreach (IUserPresence leave in leaves)
            {
                if (!newPresences.ContainsKey(leave.UserId))
                {
                    // unexpected
                    continue;
                }

                newPresences.Remove(leave.UserId);
            }
        }
    }
}
