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

namespace Nakama.Examples
{
    public class PurchaseExamples
    {
        private IClient client;
        private ISession session;

        private async void ValidatePurchaseApple()
        {
            string appleReceipt = "<receipt>";

            IApiValidatePurchaseResponse response = await client.ValidatePurchaseAppleAsync(session, appleReceipt);

            foreach (IApiValidatedPurchase validatedPurchase in response.ValidatedPurchases)
            {
                System.Console.WriteLine("Validated purchase: " + validatedPurchase);
            }
        }

        private async void ValidatePurcahseGoogle()
        {
            string googleReceipt = "<receipt>";

            IApiValidatePurchaseResponse response = await client.ValidatePurchaseGoogleAsync(session, googleReceipt);

            foreach (IApiValidatedPurchase validatedPurchase in response.ValidatedPurchases)
            {
                System.Console.WriteLine("Validated purchase: " + validatedPurchase);
            }
        }

        private async void ValidatePurchaseHuawei()
        {
            string huaweiReceipt = "<receipt>";
            string huaweiSignature = "<signature>";

            IApiValidatePurchaseResponse response = await client.ValidatePurchaseHuaweiAsync(session, huaweiReceipt, huaweiSignature);

            foreach (IApiValidatedPurchase validatedPurchase in response.ValidatedPurchases)
            {
                System.Console.WriteLine("Validated purchase: " + validatedPurchase);
            }
        }
    }
}
