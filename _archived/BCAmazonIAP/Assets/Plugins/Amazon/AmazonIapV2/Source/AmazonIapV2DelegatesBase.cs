
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

using System;
using System.Runtime.InteropServices;

namespace com.amazon.device.iap.cpt
{
    public abstract partial class AmazonIapV2Impl
    {
        private abstract class AmazonIapV2DelegatesBase : AmazonIapV2Base
        {
            private const string CrossPlatformTool = "XAMARIN";
            protected CallbackDelegate callbackDelegate;
            protected CallbackDelegate eventDelegate;
            
            protected override void Init()
            {
                NativeInit();
            }
            
            protected override void RegisterCallback()
            {
                this.callbackDelegate = new CallbackDelegate(callback);
                NativeRegisterCallback(callbackDelegate);
            }
            
            protected override void RegisterEventListener()
            {
                this.eventDelegate = new CallbackDelegate(FireEvent);
                NativeRegisterEventListener(eventDelegate);
            }
            
            protected override void RegisterCrossPlatformTool()
            {
                NativeRegisterCrossPlatformTool(CrossPlatformTool);
            }
            
            public override void UnityFireEvent(string jsonMessage)
            {
                throw new NotSupportedException("UnityFireEvent is not supported");
            }

            protected abstract void NativeInit();
            protected abstract void NativeRegisterCallback(CallbackDelegate callback);
            protected abstract void NativeRegisterEventListener(CallbackDelegate callback);
            protected abstract void NativeRegisterCrossPlatformTool(string crossPlatformTool);
        }
    }
}

