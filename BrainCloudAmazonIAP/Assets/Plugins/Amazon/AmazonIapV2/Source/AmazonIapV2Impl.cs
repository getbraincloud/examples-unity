
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
    #define UNITY_PLATFORM
#endif

#if __ANDROID__
using Android.App;
#endif
#if UNITY_PLATFORM
using UnityEngine;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using com.amazon.device.iap.cpt.json;

namespace com.amazon.device.iap.cpt 
{
    /// <summary> Provides a lazy-initialized singleton implementation of IAmazonIapV2. </summary> 

#if UNITY_PLATFORM
    public abstract partial class AmazonIapV2Impl : MonoBehaviour, IAmazonIapV2
#else
    public abstract partial class AmazonIapV2Impl : IAmazonIapV2
#endif
    {
        protected delegate void CallbackDelegate(string jsonMessage);
        static AmazonLogger logger;
        static readonly Dictionary<string, IDelegator> callbackDictionary = new Dictionary<string, IDelegator>();
        static readonly System.Object callbackLock = new System.Object();
        static readonly Dictionary<string, List<IDelegator>> eventListeners = new Dictionary<string, List<IDelegator>>();
        static readonly System.Object eventLock = new System.Object();
        
        private AmazonIapV2Impl() {}
        
        public static IAmazonIapV2 Instance {
            get
            { 
                return Builder.instance; 
            }
        }

        private class Builder
        {
            // A static constructor tells the C# compiler not to mark type as beforefieldinit, and thus lazy load this class
            static Builder() {}

            internal static readonly IAmazonIapV2 instance =
#if UNITY_EDITOR
                new AmazonIapV2UnityEditor();
#elif UNITY_ANDROID
                AmazonIapV2UnityAndroid.Instance;
#elif __ANDROID__
                AmazonIapV2Android.Instance;
#else
                new AmazonIapV2Default();
#endif
        }
#if __IOS__
        [ObjCRuntime.MonoPInvokeCallbackAttribute (typeof (CallbackDelegate))]
#endif
        public static void callback(string jsonMessage) 
        {
            Dictionary<string, object> message = null;
            try 
            {
                logger.Debug ("Executing callback");
                message = Json.Deserialize(jsonMessage) as Dictionary<string, object>;
                string callerId = message["callerId"] as string;
                Dictionary<string, object> response = message["response"] as Dictionary<string, object>;
                callbackCaller(response, callerId);
            }
            catch (KeyNotFoundException e)
            {
                logger.Debug("callerId not found in callback");
                throw new AmazonException("Internal Error: Unknown callback id", e);
            }
            catch (AmazonException e)
            {
                logger.Debug("Async call threw exception: " + e.ToString());
            }
        }

        private static void callbackCaller(Dictionary<string, object> response, string callerId)
        {
            IDelegator delegator = null;

            try
            {
                Jsonable.CheckForErrors(response);
                
                lock(AmazonIapV2Impl.callbackLock)
                {
                    delegator = AmazonIapV2Impl.callbackDictionary[callerId];
                    AmazonIapV2Impl.callbackDictionary.Remove(callerId);
                
                    delegator.ExecuteSuccess(response);
                }
            }
            catch (AmazonException e)
            {
                 lock(AmazonIapV2Impl.callbackLock)
                 {
                    if (delegator == null) {
                        delegator = AmazonIapV2Impl.callbackDictionary[callerId];                        
                    }
                    AmazonIapV2Impl.callbackDictionary.Remove(callerId);                 
                    
                    delegator.ExecuteError(e);
                 }
            }
        }

#if __IOS__
        [ObjCRuntime.MonoPInvokeCallbackAttribute (typeof (CallbackDelegate))]
#endif
        public static void FireEvent(string jsonMessage)
        {
            try
            {
                logger.Debug("eventReceived");
                Dictionary<string, object> jsonDict = Json.Deserialize(jsonMessage) as Dictionary<string, object>;
                string eventId = jsonDict["eventId"] as string;
                Dictionary<string, object> response = null;
                
                if(jsonDict.ContainsKey("response"))
                {
                    response = jsonDict["response"] as Dictionary<string, object>;
                    Jsonable.CheckForErrors(response);
                }

                lock(eventLock)
                {
                    List<IDelegator> listeners;
                    if (eventListeners.TryGetValue(eventId, out listeners))
                    {
                        foreach (var delegator in listeners)
                        {
                            if(response != null)
                            {
                                delegator.ExecuteSuccess(response);
                            }
                            else
                            {
                                delegator.ExecuteSuccess();
                            }
                        }
                    }
                    else
                    {
                        logger.Debug("No listeners found for event " + eventId + ". Ignoring event");
                    }
                }
            }
            catch (AmazonException e)
            {
                logger.Debug("Event call threw exception: " + e.ToString());
            }
        }
        // AmazonIapV2 API
        public abstract RequestOutput GetUserData();
        public abstract RequestOutput Purchase(SkuInput skuInput);
        public abstract RequestOutput GetProductData(SkusInput skusInput);
        public abstract RequestOutput GetPurchaseUpdates(ResetInput resetInput);
        public abstract void NotifyFulfillment(NotifyFulfillmentInput notifyFulfillmentInput);
        public abstract void UnityFireEvent(string jsonMessage);
        // Event API
        public abstract void AddGetUserDataResponseListener (GetUserDataResponseDelegate responseDelegate);
        public abstract void RemoveGetUserDataResponseListener (GetUserDataResponseDelegate responseDelegate);
        public abstract void AddPurchaseResponseListener (PurchaseResponseDelegate responseDelegate);
        public abstract void RemovePurchaseResponseListener (PurchaseResponseDelegate responseDelegate);
        public abstract void AddGetProductDataResponseListener (GetProductDataResponseDelegate responseDelegate);
        public abstract void RemoveGetProductDataResponseListener (GetProductDataResponseDelegate responseDelegate);
        public abstract void AddGetPurchaseUpdatesResponseListener (GetPurchaseUpdatesResponseDelegate responseDelegate);
        public abstract void RemoveGetPurchaseUpdatesResponseListener (GetPurchaseUpdatesResponseDelegate responseDelegate);

#if __ANDROID__
        public abstract void SetCurrentAndroidActivity(Activity activity);
#endif
    }
}

