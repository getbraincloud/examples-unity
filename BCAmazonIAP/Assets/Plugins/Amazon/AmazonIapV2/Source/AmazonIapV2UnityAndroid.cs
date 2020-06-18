
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

using System.Runtime.InteropServices;
#if UNITY_ANDROID
using UnityEngine;
#endif
namespace com.amazon.device.iap.cpt
{
    public abstract partial class AmazonIapV2Impl
    {
#if UNITY_ANDROID
        private class AmazonIapV2UnityAndroid : AmazonIapV2UnityBase
        {
            [DllImport ("AmazonIapV2Bridge")]
            private static extern string nativeRegisterCallbackGameObject(string name);


            [DllImport ("AmazonIapV2Bridge")]
            private static extern string nativeInit();

            [DllImport ("AmazonIapV2Bridge")]
            private static extern string nativeGetUserDataJson(string jsonMessage);

            [DllImport ("AmazonIapV2Bridge")]
            private static extern string nativePurchaseJson(string jsonMessage);

            [DllImport ("AmazonIapV2Bridge")]
            private static extern string nativeGetProductDataJson(string jsonMessage);

            [DllImport ("AmazonIapV2Bridge")]
            private static extern string nativeGetPurchaseUpdatesJson(string jsonMessage);

            [DllImport ("AmazonIapV2Bridge")]
            private static extern string nativeNotifyFulfillmentJson(string jsonMessage);

            public static new AmazonIapV2UnityAndroid Instance
            {
                get
                {
                    return AmazonIapV2UnityBase.getInstance<AmazonIapV2UnityAndroid>();
                }
            }

            protected override void NativeInit()
            {
                AmazonIapV2UnityAndroid.nativeInit();
            }

            protected override void RegisterCallback()
            {
                AmazonIapV2UnityAndroid.nativeRegisterCallbackGameObject(gameObject.name);
            }

            protected override void RegisterEventListener()
            {
                AmazonIapV2UnityAndroid.nativeRegisterCallbackGameObject(gameObject.name);
            }

            protected override void NativeRegisterCrossPlatformTool(string crossPlatformTool)
            {
            }

            protected override string NativeGetUserDataJson(string jsonMessage)
            {
                return AmazonIapV2UnityAndroid.nativeGetUserDataJson(jsonMessage);
            }

            protected override string NativePurchaseJson(string jsonMessage)
            {
                return AmazonIapV2UnityAndroid.nativePurchaseJson(jsonMessage);
            }

            protected override string NativeGetProductDataJson(string jsonMessage)
            {
                return AmazonIapV2UnityAndroid.nativeGetProductDataJson(jsonMessage);
            }

            protected override string NativeGetPurchaseUpdatesJson(string jsonMessage)
            {
                return AmazonIapV2UnityAndroid.nativeGetPurchaseUpdatesJson(jsonMessage);
            }

            protected override string NativeNotifyFulfillmentJson(string jsonMessage)
            {
                return AmazonIapV2UnityAndroid.nativeNotifyFulfillmentJson(jsonMessage);
            }

        }
#endif
    }
}

