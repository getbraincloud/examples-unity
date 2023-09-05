
/* 
* Copyright 2014 Amazon.com,
* Inc. or its affiliates. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the
* "License"). You may not use this file except in compliance
* with the License. A copy of the License is located at
*
* http://aws.amazon.com/apache2.0/
*
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, either express or implied. See the
* License for the specific language governing permissions and
* limitations under the License.
*/

#if __ANDROID__
using Android.App;
#endif
using System.Collections;

namespace com.amazon.device.iap.cpt 
{
    public delegate void GetUserDataResponseDelegate(GetUserDataResponse eventName);
    public delegate void PurchaseResponseDelegate(PurchaseResponse eventName);
    public delegate void GetProductDataResponseDelegate(GetProductDataResponse eventName);
    public delegate void GetPurchaseUpdatesResponseDelegate(GetPurchaseUpdatesResponse eventName);

    public interface IAmazonIapV2
    {
        // AmazonIapV2 API
        RequestOutput GetUserData();
        RequestOutput Purchase(SkuInput skuInput);
        RequestOutput GetProductData(SkusInput skusInput);
        RequestOutput GetPurchaseUpdates(ResetInput resetInput);
        void NotifyFulfillment(NotifyFulfillmentInput notifyFulfillmentInput);
        void UnityFireEvent(string jsonMessage);

        // Event API
        void AddGetUserDataResponseListener (GetUserDataResponseDelegate responseDelegate);
        void RemoveGetUserDataResponseListener (GetUserDataResponseDelegate responseDelegate);
        void AddPurchaseResponseListener (PurchaseResponseDelegate responseDelegate);
        void RemovePurchaseResponseListener (PurchaseResponseDelegate responseDelegate);
        void AddGetProductDataResponseListener (GetProductDataResponseDelegate responseDelegate);
        void RemoveGetProductDataResponseListener (GetProductDataResponseDelegate responseDelegate);
        void AddGetPurchaseUpdatesResponseListener (GetPurchaseUpdatesResponseDelegate responseDelegate);
        void RemoveGetPurchaseUpdatesResponseListener (GetPurchaseUpdatesResponseDelegate responseDelegate);
#if __ANDROID__
        void SetCurrentAndroidActivity(Activity activity);
#endif
    }
}

