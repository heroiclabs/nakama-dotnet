// Copyright 2018 The Nakama Authors
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

using System;
using System.Collections.Generic;
using Xunit;

namespace Nakama.Tests
{
    // NOTE Test name patterns are: MethodName_StateUnderTest_ExpectedBehavior
    public class SessionTest
    {
        private const string AuthToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1MTY5MTA5NzMsInVpZCI6ImY0MTU4ZjJiLTgwZjMtNDkyNi05NDZiLWE4Y2NmYzE2NTQ5MCIsInVzbiI6InZUR2RHSHl4dmwifQ.gzLaMQPaj5wEKoskOSALIeJLOYXEVFoPx3KY0Jm1EVU";

        private const string AuthTokenVariables =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1MTY5MTA5NzMsInVpZCI6ImY0MTU4ZjJiLTgwZjMtNDkyNi05NDZiLWE4Y2NmYzE2NTQ5MCIsInVzbiI6InZUR2RHSHl4dmwiLCJ2cnMiOnsiazEiOiJ2MSIsImsyIjoidjIifX0.Hs9ltsNmtrTJXi2U21jjuXcd-3DMsyv4W6u1vyDBMTo";

        private const string RefreshToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1aWQiOiI1NTVjNDQwMC0yZGIxLTRkYmEtOTgwMC1jZjBmYzljMTVjMTAiLCJ1c24iOiJ1YWVuWGxFRnlhIiwiZXhwIjoxNjE2MzQ3OTc2fQ.l6bKhmcEbGHKV8YQVDKF8ysmWgOqcz3tCDSRn0eIKPw";

        [Fact]
        public void GetVariables_VariablesField_Empty()
        {
            var session = Session.Restore(AuthToken);
            Assert.NotNull(session.AuthToken);
            Assert.Equal(AuthToken, session.AuthToken);
            Assert.NotNull(session.Vars);
            Assert.Empty(session.Vars);
        }

        [Fact]
        public void GetVariables_VariablesField_Values()
        {
            var session = Session.Restore(AuthTokenVariables);
            Assert.NotNull(session.AuthToken);
            Assert.NotNull(session.Username);
            Assert.Equal("vTGdGHyxvl", session.Username);
            Assert.NotNull(session.UserId);
            Assert.Equal("f4158f2b-80f3-4926-946b-a8ccfc165490", session.UserId);
            Assert.NotNull(session.Vars);
            Assert.Contains(session.Vars, pair => pair.Key.Equals("k1") || pair.Key.Equals("k2"));
        }

        [Fact]
        public async void GetVariables_VariablesField_FromAuthenticate()
        {
            var client = ClientUtil.FromSettingsFile();
            var id = Guid.NewGuid().ToString();
            var vars = new Dictionary<string, string> {{"k1", "v1"}};
            var session = await client.AuthenticateDeviceAsync(id, null, true, vars);
            Assert.NotNull(session);
            Assert.NotNull(session.Vars);
            Assert.Equal(vars, session.Vars);
        }

        [Fact]
        public async void GetRefreshToken_RefreshTokenField_FromAuthenticate()
        {
            var client = ClientUtil.FromSettingsFile();
            var id = Guid.NewGuid().ToString();
            var session = await client.AuthenticateDeviceAsync(id);
            Assert.NotNull(session);
            Assert.NotNull(session.RefreshToken);
            Assert.NotEqual(0L, session.RefreshExpireTime);
        }

        [Fact]
        public async void SessionLogout_RefreshTokenField_Disabled()
        {
            var client = ClientUtil.FromSettingsFile();
            var id = Guid.NewGuid().ToString();
            var session = await client.AuthenticateDeviceAsync(id);
            Assert.NotNull(session);
            await client.SessionLogoutAsync(session);
            var ex = await Assert.ThrowsAsync<ApiResponseException>(() => client.GetAccountAsync(session));
            Assert.Equal(401, ex.StatusCode);
        }

        [Fact]
        public void GetUsername_UsernameField_NotNull()
        {
            var session = Session.Restore(AuthToken);
            Assert.NotNull(session.AuthToken);
            Assert.Equal(AuthToken, session.AuthToken);
            Assert.NotNull(session.Username);
            Assert.Equal("vTGdGHyxvl", session.Username);
        }

        [Fact]
        public void GetUserId_UserIdField_NotNull()
        {
            var session = Session.Restore(AuthToken);
            Assert.NotNull(session.AuthToken);
            Assert.Equal(AuthToken, session.AuthToken);
            Assert.NotNull(session.UserId);
            Assert.Equal("f4158f2b-80f3-4926-946b-a8ccfc165490", session.UserId);
        }

        [Fact]
        public void IsExpired_ExpiredField_True()
        {
            var session = Session.Restore(AuthToken);
            Assert.NotNull(session.AuthToken);
            Assert.Equal(AuthToken, session.AuthToken);
            Assert.Equal(1516910973, session.ExpireTime);
            Assert.NotInRange(session.CreateTime, 0, 0);
            Assert.True(session.IsExpired);
        }

        [Fact]
        public void IsRefreshExpired_RefreshExpiredField_True()
        {
            var session = Session.Restore(AuthToken, RefreshToken);
            Assert.NotNull(session);
            Assert.Equal(RefreshToken, session.RefreshToken);
            Assert.Equal(1616347976, session.RefreshExpireTime);
            Assert.NotInRange(session.RefreshExpireTime, 0, 0);
            Assert.True(session.IsRefreshExpired);
        }

        [Fact]
        public void Restore_AuthTokenEmptyString_Null()
        {
            var session = Session.Restore("");
            Assert.Null(session);
        }

        [Fact]
        public void Restore_RefreshTokenNull_Valid()
        {
            var session = Session.Restore(AuthToken, null);
            Assert.NotNull(session);
            Assert.Null(session.RefreshToken);
            Assert.Equal(0L, session.RefreshExpireTime);
            Assert.True(session.IsRefreshExpired);
        }
    }
}
