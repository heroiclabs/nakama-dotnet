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

using System.Threading.Tasks;

namespace NakamaSync
{
    internal class Handshaker
    {
        private GuestHandshaker _guestHandshaker;
        private HostHandshaker _hostHandhaker;
        private RolePresenceTracker _presenceTracker;

        // todo expose handshake timeout? or keep same as socket timeout?
        public Handshaker(GuestHandshaker guestHandshaker, HostHandshaker hostHandshaker, RolePresenceTracker presenceTracker)
        {
            _guestHandshaker = guestHandshaker;
            _hostHandhaker = hostHandshaker;
            _presenceTracker = presenceTracker;
        }

        // if a new user joins and is determined host, it still must handshake...
        // how to best handle this
        public Task WaitForHandshake()
        {
            if (_presenceTracker.IsSelfHost())
            {
                return Task.CompletedTask;
            }
            else
            {
                return _guestHandshaker.GetHandshakeTask();
            }
        }

        public void Subscribe(SyncSocket socket)
        {
            _hostHandhaker.Subscribe(socket);
        }
    }
}