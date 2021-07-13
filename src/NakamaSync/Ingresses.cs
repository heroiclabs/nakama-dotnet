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

namespace NakamaSync
{
    internal class Ingresses
    {
        public SharedRoleIngress SharedRoleIngress { get; }
        public UserRoleIngress UserRoleIngress { get; }

        public Ingresses(VarKeys keys, VarRegistry registry, EnvelopeBuilder builder, RolePresenceTracker rolePresenceTracker)
        {
            var guestIngress = new GuestIngress(keys, rolePresenceTracker);
            var sharedHostIngress = new SharedHostIngress(keys, builder);
            var userHostIngress = new UserHostIngress(keys, builder);
            SharedRoleIngress = new SharedRoleIngress(guestIngress, sharedHostIngress, registry);
            UserRoleIngress = new UserRoleIngress(guestIngress, userHostIngress, registry);
        }
    }
}