/*
 * Copyright 2026 The Nakama Authors
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests.Api
{
    public class ProviderPayloadTest
    {
        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task AuthenticateAsync_SerializesProviderPayloadAndForwardsParams()
        {
            var live = TestsUtil.FromSettingsFile();
            var liveSession = await live.AuthenticateCustomAsync(Guid.NewGuid().ToString());
            var adapter = new RecordingAdapter($"{{\"created\":true,\"token\":\"{liveSession.AuthToken}\",\"refresh_token\":\"{liveSession.RefreshToken}\"}}");
            var client = TestsUtil.FromSettingsFile(TestsUtil.DefaultSettingsPath, adapter);

            var session = await client.AuthenticateAsync("foo-auth", new Dictionary<string, object>
            {
                { "id", "8f3c2b1a-5d4e-4f6a-9b7c-0d1e2f3a4b5c" },
                { "access_token", "ya29.a0AfB_byC3x9Qk" },
                { "expires_in", 3599 },
                { "email_verified", true },
                { "refresh_token", null },
                { "profile", new Dictionary<string, object> { { "display_name", "Mira" }, { "locale", "en-GB" } } },
                { "scopes", new List<object> { "openid", "profile", "email" } }
            }, "mira", true, new Dictionary<string, string> { { "platform", "ios" } });

            var request = adapter.Requests.Single();
            Assert.Equal("POST", request.Method);
            Assert.EndsWith("/v2/account/authenticate", request.Path);
            Assert.Contains("create=true", request.Query);
            Assert.Contains("username=mira", request.Query);
            Assert.StartsWith("Basic ", request.Headers["Authorization"]);
            Assert.Contains("\"provider\":\"foo-auth\"", request.Body);
            Assert.Contains("\"id\":\"8f3c2b1a-5d4e-4f6a-9b7c-0d1e2f3a4b5c\"", request.Body);
            Assert.Contains("\"expires_in\":3599", request.Body);
            Assert.Contains("\"email_verified\":true", request.Body);
            Assert.Contains("\"refresh_token\":null", request.Body);
            Assert.Contains("\"profile\":{\"display_name\":\"Mira\",\"locale\":\"en-GB\"}", request.Body);
            Assert.Contains("\"scopes\":[\"openid\",\"profile\",\"email\"]", request.Body);
            Assert.Contains("\"vars\":{\"platform\":\"ios\"}", request.Body);
            Assert.True(session.Created);
            Assert.Equal(liveSession.UserId, session.UserId);
            Assert.Equal(liveSession.AuthToken, session.AuthToken);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ValidatePurchaseAsync_PreservesPersistFalseAndExposesProviderMetadata()
        {
            var live = TestsUtil.FromSettingsFile();
            var session = await live.AuthenticateCustomAsync(Guid.NewGuid().ToString());
            var adapter = new RecordingAdapter(
                $"{{\"purchase\":[{{\"user_id\":\"{session.UserId}\", \"product_id\":\"com.example.gems_500\", \"transaction_id\":\"ord_7Hq2LmZ9xR\", \"store\":1000, \"purchase_time\":\"2026-11-04T09:15:27.512Z\", \"provider_response\":\"{{\\\"order_id\\\":\\\"ord_7Hq2LmZ9xR\\\",\\\"status\\\":\\\"paid\\\"}}\", \"environment\":2, \"provider\":\"webshop\"}}]}}");
            var client = new Client(live.Scheme, live.Host, live.Port, live.ServerKey, adapter, autoRefreshSession: false);

            var response = await client.ValidatePurchaseAsync(session, "webshop", new Dictionary<string, object>
            {
                { "order_id", "ord_7Hq2LmZ9xR" },
                { "product_id", "com.example.gems_500" },
                { "quantity", 2 },
                { "price", 4.99 },
                { "sandbox", false },
                { "coupon", null },
                { "receipt", new Dictionary<string, object> { { "signature", "MEUCIQDx3k" }, { "issued_at", 1793870127 } } },
                { "items", new List<object> { "gems_500", "bonus_50" } }
            }, persist: false);

            var request = adapter.Requests.Single();
            Assert.Equal("POST", request.Method);
            Assert.EndsWith("/v2/iap/purchase/webshop", request.Path);
            Assert.StartsWith("Bearer ", request.Headers["Authorization"]);
            Assert.Contains("\"persist\":false", request.Body);
            Assert.Contains("\"order_id\":\"ord_7Hq2LmZ9xR\"", request.Body);
            Assert.Contains("\"quantity\":2", request.Body);
            Assert.Contains("\"price\":4.99", request.Body);
            Assert.Contains("\"sandbox\":false", request.Body);
            Assert.Contains("\"coupon\":null", request.Body);
            Assert.Contains("\"receipt\":{\"signature\":\"MEUCIQDx3k\",\"issued_at\":1793870127}", request.Body);
            Assert.Contains("\"items\":[\"gems_500\",\"bonus_50\"]", request.Body);
            var purchase = response.Purchase.Single();
            Assert.Equal(session.UserId, purchase.UserId);
            Assert.Equal("ord_7Hq2LmZ9xR", purchase.TransactionId);
            Assert.Equal("com.example.gems_500", purchase.ProductId);
            Assert.Equal("webshop", purchase.Provider);
            Assert.Equal((ApiStoreProvider)1000, purchase.Store);
            Assert.Equal(ApiStoreEnvironment.PRODUCTION, purchase.Environment);
            Assert.Equal("2026-11-04T09:15:27.512Z", purchase.PurchaseTime);
            Assert.Equal("{\"order_id\":\"ord_7Hq2LmZ9xR\",\"status\":\"paid\"}", purchase.ProviderResponse);
            Assert.False(purchase.SeenBefore);
            Assert.Null(purchase.CreateTime);
            Assert.Null(purchase.UpdateTime);
        }

        [Fact(Timeout = TestsUtil.TIMEOUT_MILLISECONDS)]
        public async Task ValidateSubscriptionAsync_PreservesPersistFalseAndExposesProviderMetadata()
        {
            var live = TestsUtil.FromSettingsFile();
            var session = await live.AuthenticateCustomAsync(Guid.NewGuid().ToString());
            var adapter = new RecordingAdapter(
                $"{{\"subscription\":[{{\"user_id\":\"{session.UserId}\", \"product_id\":\"com.example.vip_monthly\", \"original_transaction_id\":\"sub_3Kd8PwQ1nV\", \"store\":1000, \"purchase_time\":\"2026-11-04T09:15:27.512Z\", \"environment\":1, \"expiry_time\":\"2026-12-04T09:15:27.512Z\", \"provider_response\":\"{{\\\"subscription_id\\\":\\\"sub_3Kd8PwQ1nV\\\",\\\"status\\\":\\\"active\\\"}}\", \"active\":true, \"provider\":\"webshop\"}}, {{\"user_id\":\"{session.UserId}\", \"product_id\":\"com.example.vip_yearly\", \"original_transaction_id\":\"sub_7Rt2LmX4cB\", \"store\":1000, \"environment\":1, \"expiry_time\":\"2027-11-04T09:15:27.512Z\", \"active\":true, \"provider\":\"webshop\"}}]}}");
            var client = new Client(live.Scheme, live.Host, live.Port, live.ServerKey, adapter, autoRefreshSession: false);

            var response = await client.ValidateSubscriptionAsync(session, "webshop", new Dictionary<string, object>
            {
                { "subscription_id", "sub_3Kd8PwQ1nV" },
                { "plan", "com.example.vip_monthly" },
                { "seats", 1 },
                { "trial", true },
                { "promo_code", null },
                { "billing", new Dictionary<string, object> { { "interval", "month" }, { "interval_count", 1 } } }
            }, persist: false);

            var request = adapter.Requests.Single();
            Assert.Equal("POST", request.Method);
            Assert.EndsWith("/v2/iap/subscription/webshop", request.Path);
            Assert.StartsWith("Bearer ", request.Headers["Authorization"]);
            Assert.Contains("\"persist\":false", request.Body);
            Assert.Contains("\"subscription_id\":\"sub_3Kd8PwQ1nV\"", request.Body);
            Assert.Contains("\"seats\":1", request.Body);
            Assert.Contains("\"trial\":true", request.Body);
            Assert.Contains("\"promo_code\":null", request.Body);
            Assert.Contains("\"billing\":{\"interval\":\"month\",\"interval_count\":1}", request.Body);
            var subscriptions = response.Subscription.ToList();
            Assert.Equal(2, subscriptions.Count);
            Assert.Equal("sub_7Rt2LmX4cB", subscriptions[1].OriginalTransactionId);
            Assert.Equal("com.example.vip_yearly", subscriptions[1].ProductId);
            var subscription = subscriptions[0];
            Assert.Equal(session.UserId, subscription.UserId);
            Assert.Equal("sub_3Kd8PwQ1nV", subscription.OriginalTransactionId);
            Assert.Equal("com.example.vip_monthly", subscription.ProductId);
            Assert.Equal("webshop", subscription.Provider);
            Assert.Equal((ApiStoreProvider)1000, subscription.Store);
            Assert.Equal(ApiStoreEnvironment.SANDBOX, subscription.Environment);
            Assert.True(subscription.Active);
            Assert.Equal("2026-12-04T09:15:27.512Z", subscription.ExpiryTime);
            Assert.Equal("{\"subscription_id\":\"sub_3Kd8PwQ1nV\",\"status\":\"active\"}", subscription.ProviderResponse);
            Assert.Null(subscription.CreateTime);
            Assert.Null(subscription.UpdateTime);
        }

        private sealed class RecordingAdapter : IHttpAdapter
        {
            private readonly string _response;

            public List<Request> Requests { get; } = new List<Request>();

            public ILogger Logger { get; set; }

            public TransientExceptionDelegate TransientExceptionDelegate => _ => false;

            public RecordingAdapter(string response)
            {
                _response = response;
            }

            Task<string> IHttpAdapter.SendAsync(string method, Uri uri, IDictionary<string, string> headers, byte[] body, int timeoutSec, CancellationToken? userCancelToken)
            {
                Requests.Add(new Request
                {
                    Method = method,
                    Path = uri.AbsolutePath,
                    Query = uri.Query,
                    Headers = headers,
                    Body = body == null ? string.Empty : Encoding.UTF8.GetString(body),
                });
                return Task.FromResult(_response);
            }
        }

        private sealed class Request
        {
            public string Method { get; set; }

            public string Path { get; set; }

            public string Query { get; set; }

            public IDictionary<string, string> Headers { get; set; }

            public string Body { get; set; }
        }
    }
}
