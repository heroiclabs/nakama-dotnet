/**
* Copyright 2021 The Nakama Authors
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

using System;
using Nakama;

namespace NakamaSync
{
    /// <summary>
    /// A variable containing a value for a user in the match. The value is synchronized across all users
    /// but can only be set by the associated user.
    /// </summary>
    public class PresenceVar<T> : Var<T>
    {
        public IUserPresence Presence { get; private set; }

        public PresenceVar(long opcode) : base(opcode)
        {
        }

        internal override void Reset()
        {
            SetPresence(null);
            base.Reset();
        }

        internal void SetPresence(IUserPresence presence)
        {
            if (presence?.UserId == Presence?.UserId)
            {
                // todo log warning here?
                return;
            }

            if (presence?.UserId == SyncMatch?.Self?.UserId)
            {
                throw new InvalidOperationException("PresenceVar cannot have a presence id equal to self id.");
            }

            this.Presence = presence;
        }
    }
}
