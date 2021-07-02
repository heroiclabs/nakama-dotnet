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
    internal class SyncVarRouter
    {
        public event Action<IUserPresence> OnHandshakeRequestReady;
        public event Action<IUserPresence, bool> OnHandshakeResponseReady;
        public event Action OnHostVarsReady;
        public event Action OnGuestVarsReady;

        private GuestEgress _guestEgress;
        private GuestIngress _guestIngress;
        private HostEgress _hostEgress;
        private HostIngress _hostIngress;
        private PresenceTracker _presenceTracker;

        public SyncVarRouter(SyncVarKeys keys, PresenceTracker presenceTracker)
        {
            _guestEgress = new GuestEgress(keys);
            _guestIngress = new GuestIngress(keys, presenceTracker);
            _hostEgress = new HostEgress(keys, presenceTracker);
            _hostIngress = new HostIngress(keys);
            _presenceTracker = presenceTracker;
        }

        public void HandleLocalSharedVarChanged<T>(SyncVarKey key, ISharedVarEvent<T> evt, SharedVarCollections<T> collections)
        {
            GetEgress().HandleLocalSharedVarChanged<T>(key, evt.NewValue, collections);
        }

        public void HandleLocalUserVarChanged<T>(SyncVarKey key, IUserVarEvent<T> evt, UserVarCollections<T> collections)
        {
            GetEgress().HandleLocalUserVarChanged<T>(key, evt.NewValue, collections, evt.Target);
        }

        public void HandleHandshakeRequest(IUserPresence source, HandshakeRequest request, SyncCollections collections)
        {
            if (_hostEgress.IsValidHandshakeRequest(request))
            {
                _hostEgress.HandleValidHandshakeRequest(source, request, collections);
                OnHandshakeResponseReady(source, true);
            }

            OnHandshakeResponseReady(source, false);
        }

        public void HandleHandshakeResponse(IUserPresence source, HandshakeResponse response, SyncCollections collections)
        {
            if (response.Success)
            {
                HandleIncomingSyncValues(source, response.Store, collections);
            }
            else
            {
                throw new InvalidOperationException("Synced match handshake with host failed.");
            }
        }

        public void HandleIncomingSyncValues(IUserPresence source, SyncValues values, SyncCollections collections)
        {
            HandleIncomingSharedSyncValues(source, collections.SharedBoolCollections);
            HandleIncomingSharedSyncValues(source, collections.SharedFloatCollections);
            HandleIncomingSharedSyncValues(source, collections.SharedIntCollections);
            HandleIncomingSharedSyncValues(source, collections.SharedStringCollections);

            HandleIncomingUserSyncValues(source, collections.UserBoolCollections);
            HandleIncomingUserSyncValues(source, collections.UserFloatCollections);
            HandleIncomingUserSyncValues(source, collections.UserIntCollections);
            HandleIncomingUserSyncValues(source, collections.UserStringCollections);

            if (_presenceTracker.IsSelfHost())
            {
                OnHostVarsReady?.Invoke();
            }
            else
            {
                OnGuestVarsReady?.Invoke();
            }
        }

        private IVarEgress GetEgress()
        {
            if (_presenceTracker.IsSelfHost())
            {
                return _hostEgress;
            }

            return _guestEgress;
        }

        private IVarIngress GetIngress()
        {
            if (_presenceTracker.IsSelfHost())
            {
                return _hostIngress;
            }

            return _guestIngress;
        }

        private void HandleIncomingSharedSyncValues<T>(IUserPresence source, SharedVarCollections<T> collections)
        {
            IVarIngress ingress = GetIngress();

            foreach (SyncSharedValue<T> sharedValue in collections.SharedValuesIncoming)
            {
                ingress.HandleIncomingSharedVar(source, sharedValue, collections);
            }
        }

        private void HandleIncomingUserSyncValues<T>(IUserPresence source, UserVarCollections<T> collections)
        {
            IVarIngress ingress = GetIngress();

            foreach (SyncUserValue<T> userValue in collections.UserValuesIncoming)
            {
                ingress.HandleIncomingUserVar(source, userValue, collections);
            }
        }
    }
}
