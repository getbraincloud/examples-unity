
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
using System.Collections.Generic;
using System.Diagnostics;
using com.amazon.device.iap.cpt.json;

namespace com.amazon.device.iap.cpt 
{
    public abstract partial class AmazonIapV2Impl
    {
        private abstract class AmazonIapV2Base : AmazonIapV2Impl
        {
            static readonly System.Object startLock = new System.Object();
            static volatile bool startCalled = false;
        
            protected void Start () 
            {
                if(startCalled) 
                {
                    return;
                }
            
                lock(startLock)
                {
                    if(startCalled == false)
                    {
                        Init();
                        RegisterCallback();
                        RegisterEventListener();
                        RegisterCrossPlatformTool();
                        startCalled = true;
                    }
                }
            }

            protected abstract void Init();
            protected abstract void RegisterCallback();
            protected abstract void RegisterEventListener();
            protected abstract void RegisterCrossPlatformTool();
            
            public AmazonIapV2Base()
            {
                logger = new AmazonLogger(this.GetType().Name);
            }

            public override void UnityFireEvent(string jsonMessage)
            {
                AmazonIapV2Impl.FireEvent(jsonMessage);
            }
            public override RequestOutput GetUserData()
            {
                Start();
                return RequestOutput.CreateFromJson(GetUserDataJson("{}"));
            }

            private string GetUserDataJson(string jsonMessage)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string result = NativeGetUserDataJson(jsonMessage);

                stopwatch.Stop();
                logger.Debug(string.Format("Successfully called native code in {0} ms", stopwatch.ElapsedMilliseconds));

                return result;
            }
        
            protected abstract string NativeGetUserDataJson(string jsonMessage);

            public override RequestOutput Purchase(SkuInput skuInput)
            {
                Start();
                return RequestOutput.CreateFromJson(PurchaseJson(skuInput.ToJson()));
            }

            private string PurchaseJson(string jsonMessage)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string result = NativePurchaseJson(jsonMessage);

                stopwatch.Stop();
                logger.Debug(string.Format("Successfully called native code in {0} ms", stopwatch.ElapsedMilliseconds));

                return result;
            }
        
            protected abstract string NativePurchaseJson(string jsonMessage);

            public override RequestOutput GetProductData(SkusInput skusInput)
            {
                Start();
                return RequestOutput.CreateFromJson(GetProductDataJson(skusInput.ToJson()));
            }

            private string GetProductDataJson(string jsonMessage)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string result = NativeGetProductDataJson(jsonMessage);

                stopwatch.Stop();
                logger.Debug(string.Format("Successfully called native code in {0} ms", stopwatch.ElapsedMilliseconds));

                return result;
            }
        
            protected abstract string NativeGetProductDataJson(string jsonMessage);

            public override RequestOutput GetPurchaseUpdates(ResetInput resetInput)
            {
                Start();
                return RequestOutput.CreateFromJson(GetPurchaseUpdatesJson(resetInput.ToJson()));
            }

            private string GetPurchaseUpdatesJson(string jsonMessage)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string result = NativeGetPurchaseUpdatesJson(jsonMessage);

                stopwatch.Stop();
                logger.Debug(string.Format("Successfully called native code in {0} ms", stopwatch.ElapsedMilliseconds));

                return result;
            }
        
            protected abstract string NativeGetPurchaseUpdatesJson(string jsonMessage);

            public override void NotifyFulfillment(NotifyFulfillmentInput notifyFulfillmentInput)
            {
                Start();
                                Jsonable.CheckForErrors(Json.Deserialize(NotifyFulfillmentJson(notifyFulfillmentInput.ToJson())) as Dictionary<string, object>);
            }

            private string NotifyFulfillmentJson(string jsonMessage)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string result = NativeNotifyFulfillmentJson(jsonMessage);

                stopwatch.Stop();
                logger.Debug(string.Format("Successfully called native code in {0} ms", stopwatch.ElapsedMilliseconds));

                return result;
            }
        
            protected abstract string NativeNotifyFulfillmentJson(string jsonMessage);


            public override void AddGetUserDataResponseListener(GetUserDataResponseDelegate responseDelegate)
            {
                Start();
                string eventId = "getUserDataResponse";
                lock(eventLock)
                {
                    if (eventListeners.ContainsKey(eventId))
                    {
                        eventListeners[eventId].Add(new GetUserDataResponseDelegator (responseDelegate));
                    }
                    else 
                    {
                        var list = new List<IDelegator>();
                        list.Add(new GetUserDataResponseDelegator(responseDelegate));
                        eventListeners.Add(eventId, list);
                    }
                }
            }
            
            public override void RemoveGetUserDataResponseListener(GetUserDataResponseDelegate responseDelegate)
            {
                Start();
                string eventId = "getUserDataResponse";
                lock(eventLock)
                {
                    if (eventListeners.ContainsKey(eventId))
                    {
                        foreach(GetUserDataResponseDelegator delegator in eventListeners[eventId])
                        {
                            if(delegator.responseDelegate == responseDelegate)
                            {
                                eventListeners[eventId].Remove(delegator);
                                return;
                            }
                        }
                    }
                }
            }
            public override void AddPurchaseResponseListener(PurchaseResponseDelegate responseDelegate)
            {
                Start();
                string eventId = "purchaseResponse";
                lock(eventLock)
                {
                    if (eventListeners.ContainsKey(eventId))
                    {
                        eventListeners[eventId].Add(new PurchaseResponseDelegator (responseDelegate));
                    }
                    else 
                    {
                        var list = new List<IDelegator>();
                        list.Add(new PurchaseResponseDelegator(responseDelegate));
                        eventListeners.Add(eventId, list);
                    }
                }
            }
            
            public override void RemovePurchaseResponseListener(PurchaseResponseDelegate responseDelegate)
            {
                Start();
                string eventId = "purchaseResponse";
                lock(eventLock)
                {
                    if (eventListeners.ContainsKey(eventId))
                    {
                        foreach(PurchaseResponseDelegator delegator in eventListeners[eventId])
                        {
                            if(delegator.responseDelegate == responseDelegate)
                            {
                                eventListeners[eventId].Remove(delegator);
                                return;
                            }
                        }
                    }
                }
            }
            public override void AddGetProductDataResponseListener(GetProductDataResponseDelegate responseDelegate)
            {
                Start();
                string eventId = "getProductDataResponse";
                lock(eventLock)
                {
                    if (eventListeners.ContainsKey(eventId))
                    {
                        eventListeners[eventId].Add(new GetProductDataResponseDelegator (responseDelegate));
                    }
                    else 
                    {
                        var list = new List<IDelegator>();
                        list.Add(new GetProductDataResponseDelegator(responseDelegate));
                        eventListeners.Add(eventId, list);
                    }
                }
            }
            
            public override void RemoveGetProductDataResponseListener(GetProductDataResponseDelegate responseDelegate)
            {
                Start();
                string eventId = "getProductDataResponse";
                lock(eventLock)
                {
                    if (eventListeners.ContainsKey(eventId))
                    {
                        foreach(GetProductDataResponseDelegator delegator in eventListeners[eventId])
                        {
                            if(delegator.responseDelegate == responseDelegate)
                            {
                                eventListeners[eventId].Remove(delegator);
                                return;
                            }
                        }
                    }
                }
            }
            public override void AddGetPurchaseUpdatesResponseListener(GetPurchaseUpdatesResponseDelegate responseDelegate)
            {
                Start();
                string eventId = "getPurchaseUpdatesResponse";
                lock(eventLock)
                {
                    if (eventListeners.ContainsKey(eventId))
                    {
                        eventListeners[eventId].Add(new GetPurchaseUpdatesResponseDelegator (responseDelegate));
                    }
                    else 
                    {
                        var list = new List<IDelegator>();
                        list.Add(new GetPurchaseUpdatesResponseDelegator(responseDelegate));
                        eventListeners.Add(eventId, list);
                    }
                }
            }
            
            public override void RemoveGetPurchaseUpdatesResponseListener(GetPurchaseUpdatesResponseDelegate responseDelegate)
            {
                Start();
                string eventId = "getPurchaseUpdatesResponse";
                lock(eventLock)
                {
                    if (eventListeners.ContainsKey(eventId))
                    {
                        foreach(GetPurchaseUpdatesResponseDelegator delegator in eventListeners[eventId])
                        {
                            if(delegator.responseDelegate == responseDelegate)
                            {
                                eventListeners[eventId].Remove(delegator);
                                return;
                            }
                        }
                    }
                }
            }

#if __ANDROID__
            public override void SetCurrentAndroidActivity(Activity activity)
            {
                // do nothing
            }
#endif
        }
    }
}

