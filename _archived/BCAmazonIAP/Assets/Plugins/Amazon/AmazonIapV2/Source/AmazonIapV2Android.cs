
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
using Android.Runtime;
#endif
using System.Runtime.InteropServices;

namespace com.amazon.device.iap.cpt
{
    public abstract partial class AmazonIapV2Impl
    {
#if __ANDROID__
        private class AmazonIapV2Android : AmazonIapV2DelegatesBase
        {
            private static AmazonIapV2Android instance;
            private static JValue androidActivity;
            
            private AmazonIapV2Android()
            {
            }
            
            public static AmazonIapV2Android Instance
            {
                get
                {
                    if (instance == null)
                    {
                        instance = new AmazonIapV2Android();
                        Java.Lang.JavaSystem.LoadLibrary("AmazonIapV2Bridge");
                        instance.Start();
                    }
                    
                    return instance;
                }
            }
            
            [DllImport ("AmazonIapV2Bridge")]
            private static extern string nativeRegisterCallback(CallbackDelegate callback);

            [DllImport ("AmazonIapV2Bridge")]
            private static extern string nativeRegisterEventListener(CallbackDelegate callback);


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


            protected override void NativeInit()
            {
                AmazonIapV2Android.nativeInit();
            }

            protected override void NativeRegisterCallback(CallbackDelegate callback)
            {
                AmazonIapV2Android.nativeRegisterCallback(callback);
            }

            protected override void NativeRegisterEventListener(CallbackDelegate callback)
            {
                AmazonIapV2Android.nativeRegisterEventListener(callback);
            }

            protected override void NativeRegisterCrossPlatformTool(string crossPlatformTool)
            {
            }

            protected override string NativeGetUserDataJson(string jsonMessage)
            {
                return AmazonIapV2Android.nativeGetUserDataJson(jsonMessage);
            }

            protected override string NativePurchaseJson(string jsonMessage)
            {
                return AmazonIapV2Android.nativePurchaseJson(jsonMessage);
            }

            protected override string NativeGetProductDataJson(string jsonMessage)
            {
                return AmazonIapV2Android.nativeGetProductDataJson(jsonMessage);
            }

            protected override string NativeGetPurchaseUpdatesJson(string jsonMessage)
            {
                return AmazonIapV2Android.nativeGetPurchaseUpdatesJson(jsonMessage);
            }

            protected override string NativeNotifyFulfillmentJson(string jsonMessage)
            {
                return AmazonIapV2Android.nativeNotifyFulfillmentJson(jsonMessage);
            }

            
            [DllImport ("AmazonIapV2Bridge")]
            private static extern void nativeSetCurrentAndroidActivity(JValue activity);
            
            public override void SetCurrentAndroidActivity(Activity activity)
            {
                Start();
                AmazonIapV2Android.androidActivity = new JValue(activity);
                AmazonIapV2Android.nativeSetCurrentAndroidActivity(androidActivity);
            }
        }
#endif
    }
}

