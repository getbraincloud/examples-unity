
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


namespace com.amazon.device.iap.cpt
{
    public abstract partial class AmazonIapV2Impl
    {
        private class AmazonIapV2Default : AmazonIapV2Base
        {
            protected override void Init() {}
            protected override void RegisterCallback() {}
            protected override void RegisterEventListener() {}
            protected override void RegisterCrossPlatformTool() {}
            protected override string NativeGetUserDataJson(string jsonMessage)
            {
                return "{}";
            }
            protected override string NativePurchaseJson(string jsonMessage)
            {
                return "{}";
            }
            protected override string NativeGetProductDataJson(string jsonMessage)
            {
                return "{}";
            }
            protected override string NativeGetPurchaseUpdatesJson(string jsonMessage)
            {
                return "{}";
            }
            protected override string NativeNotifyFulfillmentJson(string jsonMessage)
            {
                return "{}";
            }
        }
    }
}
