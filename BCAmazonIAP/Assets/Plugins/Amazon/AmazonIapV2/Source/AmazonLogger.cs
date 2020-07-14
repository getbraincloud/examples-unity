
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

#if UNITY_EDITOR || UNITY_IPHONE || UNITY_ANDROID
using UnityEngine;
using com.amazon.device.iap.cpt.log;
#endif

using System;

namespace com.amazon.device.iap.cpt 
{
    public class AmazonLogger
    {
        private readonly String tag;
        
        public AmazonLogger(String tag)
        {
            this.tag = tag;
        }
        
        public void Debug(String msg)
        {
#if UNITY_EDITOR || UNITY_IPHONE || UNITY_ANDROID
                AmazonLogging.Log(AmazonLogging.AmazonLoggingLevel.Verbose, tag, msg);
#elif __ANDROID__
                Android.Util.Log.Debug(tag, msg);
#elif __IOS__
                System.Console.WriteLine(msg);
#endif
        }
        
        public String getTag()
        {
            return tag;
        }
    }
}

