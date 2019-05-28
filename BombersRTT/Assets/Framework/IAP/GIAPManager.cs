using System;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using BrainCloud;

#if BUY_CURRENCY_ENABLED
using UnityEngine.Purchasing;
#endif
namespace Gameframework
{
    // Deriving the Purchaser class from IStoreListener enables it to receive messages from Unity Purchasing.
    public class GIAPManager : SingletonBehaviour<GIAPManager>
#if BUY_CURRENCY_ENABLED 
        , IStoreListener
#endif
    {
        #region Public Consts
        public const string JSON_PRODUCT_INVENTORY = "productInventory";

        //platforms
        public const string IAP_PLATFORM_APPWORLD = "appworld";
        public const string IAP_PLATFORM_FACEBOOK = "facebook";
        public const string IAP_PLATFORM_GOOGLEPLAY = "googlePlay";
        public const string IAP_PLATFORM_ITUNES = "itunes";
        public const string IAP_PLATFORM_STEAM = "steam";
        public const string IAP_PLATFORM_WINDOWS = "windows";
        public const string IAP_PLATFORM_WINDOWSPHONE = "windowsPhone";

        // General product identifiers for the consumable, non-consumable, and subscription products.
        public const string PRODUCT_TYPE_CONSUMABLE = "consumable";
        public const string PRODUCT_TYPE_NON_CONSUMABLE = "nonconsumable";
        public const string PRODUCT_TYPE_SUBSCRIPTION = "subscription";

        //response keys
        public const string KEY_TYPE = "type";
        public const string KEY_CATEGORY = "category";
        public const string KEY_TITLE = "title";
        public const string KEY_DESCRIPTION = "description";
        public const string KEY_IMAGEURL = "imageUrl";
        public const string KEY_REFERENCEPRICE = "referencePrice";
        public const string KEY_PRICEDATA = "priceData";
        public const string KEY_DEFAULTPRICEDATA = "defaultPriceData";
        public const string KEY_IDS = "ids";
        public const string KEY_ISPROMOTION = "isPromotion";
        public const string KEY_CURRENCY = "currency";
        public const string KEY_PARENT_CURRENCY = "parentCurrency";
        public const string KEY_PEER_CURRENCY = "peerCurrency";
        public const string KEY_ITUNESID = "itunesId";
        public const string KEY_ANDROIDID = "id";
        public const string KEY_PRODUCT_FBURL = "fbUrl";
        public const string KEY_PRODUCTID = "itemId";

        #endregion

        public bool IsPurchasing
        {
            get { return m_bIsPurchasing; }
            private set { m_bIsPurchasing = value; }
        }

        #region Public
        /// <summary>
        /// Make an In App Purchase with this product ID
        /// </summary>
        /// <param name="braincloudProductId"></param>
        /// <param name="in_success"></param>
        /// <param name="in_failure"></param>
        public void BuyProductID(string braincloudProductId, SuccessCallback in_success = null, FailureCallback in_failure = null, bool in_forcedRealPurchase = false)
        {
            IAPProduct product = GetIAPProductByBrainCloudID(braincloudProductId);
#if DEBUG_IAP_ENABLED
            if (!in_forcedRealPurchase)
            {
                GStateManager.InitializeDelegate init = null;
                init = (BaseState state) =>
                {
                    IAPMessageSubState messageSubState = state as IAPMessageSubState;
                    if (messageSubState)
                    {
                        GStateManager.Instance.OnInitializeDelegate -= init;
                        messageSubState.LateInit(product, GPlayerMgr.Instance.OnVirtualCurrencies + in_success, in_failure);
                    }
                };

                GStateManager.Instance.OnInitializeDelegate += init;
                GStateManager.Instance.PushSubState(IAPMessageSubState.STATE_NAME);
            }
            else
#endif
            {
                handlePurchase(product.StoreProductId, GPlayerMgr.Instance.OnVirtualCurrencies + in_success, in_failure);
            }
        }

#if BUY_CURRENCY_ENABLED
        /// <summary>
        /// Initialized callback
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="extensions"></param>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            // Purchasing has succeeded initializing. Collect our Purchasing references.
            if (m_enableDebug)
            {
                GDebug.Log("OnInitialized: PASS");
            }

            // Overall Purchasing system, configured with products for this application.
            m_StoreController = controller;
            // Store specific subsystem, for accessing device-specific store features.
            m_StoreExtensionProvider = extensions;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            updateStoreProductInfo();
#endif
            CheckAllSubscriptions();
            GEventManager.TriggerEvent(GEventManager.ON_IAP_PRODUCTS_UPDATED);
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
            if (m_enableDebug)
            {
                GDebug.LogError("OnInitializeFailed InitializationFailureReason:" + error);
            }
        }
#endif

        public void CheckAllSubscriptions()
        {
#if BUY_CURRENCY_ENABLED
            List<IAPProduct> list = GetIAPProductsByType(ProductType.Subscription);
#else
            List<IAPProduct> list = new List<IAPProduct>();
#endif

            for (int index = 0; index < list.Count; index++)
            {
                CheckSubscription(list[index].BrainCloudProductID);
            }
        }

        /// <summary>
        /// Find the product and check if it's still subscribed
        /// </summary>
        /// <param name="id"></param>
        /// <param name="in_success"></param>
        /// <param name="in_fail"></param>
        public void CheckSubscription(string braincloudProductId)
        {
            // TODO
            /*
            IAPProduct iapProduct = GetIAPProductByBrainCloudID(braincloudProductId);
            if (iapProduct != null)
            {
                Product product = m_StoreController.products.WithID(iapProduct.StoreProductId);

                if (product != null && product.hasReceipt)
                {
                    Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(product.receipt);
                    string payload = (string)jsonMessage["Payload"];
                    GCore.Wrapper.Client.ProductService.VerifyItunesReceipt(payload, GPlayerMgr.Instance.SubscriptionSuccess, GPlayerMgr.Instance.SubscriptionFailed);
                }
            }
            */
        }
#if BUY_CURRENCY_ENABLED
        private void handleProcessPurchase(string in_storeId, string in_receipt = null)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            IsPurchasing = false;

            IAPProduct product = GetIAPProductByStoreId(in_storeId);
            if (product != null)
            {
                if (m_enableDebug)
                {
                    GDebug.Log(string.Format("PurchaseProcessingResult with id: {0}", product.StoreProductId));
                }

                //TODO: ADD MORE PLATFORMS[SMRJ]
#if UNITY_IOS
                Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_receipt);// args.purchasedProduct.receipt);
                // itunes
                Dictionary<string, object> receipt = new Dictionary<string, object>();
                receipt["receipt"] = (string)jsonMessage["Payload"];
                GCore.Wrapper.Client.AppStoreService.VerifyPurchase("itunes", JsonWriter.Serialize(receipt), m_successCallback, m_failureCallback);
#elif UNITY_ANDROID
                Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_receipt);// args.purchasedProduct.receipt);
                // ANDROID
                Dictionary<string, object> payload = (Dictionary<string, object>)JsonReader.Deserialize((string)jsonMessage["Payload"]);
                Dictionary<string, object> json = (Dictionary<string, object>)JsonReader.Deserialize((string)payload["json"]);

                Dictionary<string, object> receipt = new Dictionary<string, object>();
                receipt["productId"] = json["productId"];
                receipt["orderId"] = json["orderId"];
                receipt["token"] = json["purchaseToken"];
                //receipt["developerPayload"] = json["developerPayload"];

                GCore.Wrapper.Client.AppStoreService.VerifyPurchase("googlePlay", JsonWriter.Serialize(receipt), m_successCallback, m_failureCallback);

#elif UNITY_WEBGL
                // TODO: need to confirm integration
                Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_receipt);//args.purchasedProduct.receipt);
                Dictionary<string, object> receipt = new Dictionary<string, object>();
                receipt["signedRequest"] = (string)jsonMessage["Payload"];
                GCore.Wrapper.Client.AppStoreService.VerifyPurchase("facebook", JsonWriter.Serialize(receipt), m_successCallback, m_failureCallback);

#elif STEAMWORKS_ENABLED
                // STEAM
                Dictionary<string, object> purchaseData = new Dictionary<string, object>();
                purchaseData[BrainCloudConsts.JSON_LANGUAGE] = "en"; // TODO get proper language
                purchaseData[BrainCloudConsts.JSON_ITEM_ID] = product.StoreProductId;

                // steam is a two step process, where you start a purchase, and then finalize it, but we keep these callbacks for later, 
                // after processing the start purhcase succesfully
                GCore.Wrapper.Client.AppStoreService.StartPurchase("steam", JsonWriter.Serialize(purchaseData), onSteamStartPurchaseSuccess, m_failureCallback);
#endif
            }
            else
            {
                if (m_enableDebug)
                {
                    GDebug.LogError(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", in_storeId));
                }

                if (m_failureCallback != null)
                {
                    m_failureCallback.Invoke(-1, -1, "{'reason':'ProcessPurchase: FAIL. Unrecognized product' }", null);
                }
            }
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            handleProcessPurchase(args.purchasedProduct.definition.id, args.purchasedProduct.receipt);

            // Return a flag indicating whether this product has completely been received, or if the application needs 
            // to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
            // saving purchased products to the cloud, and when that save is delayed. 
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            IsPurchasing = false;
            // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
            // this reason with the user to guide their troubleshooting actions.
            if (m_enableDebug)
            {
                GDebug.LogError(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
            }

            if (m_failureCallback != null)
            {
                m_failureCallback.Invoke(-1, -1, "{'reason':'" + failureReason.ToString() + "' }", product);
            }
        }
#endif
        /// <summary>
        /// Callback for succesful ProductService.GetSalesInventory
        /// </summary>
        /// <param name="in_jsonString"></param>
        /// <param name="in_object"></param>
        public void OnReadInventory(string in_jsonString, object in_object)
        {
            //Build IAPProducts based on all platform responses
            // ios and google play store now supported [smrj]

            m_productInventory.Clear(); // Clearing to fill with fresh data

            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_jsonString);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            var jsonArray = jsonData[JSON_PRODUCT_INVENTORY] as Array;
            Dictionary<string, object> product;
            Dictionary<string, object> priceData;

#if UNITY_IOS
            Dictionary<string, object> idsObject; //itunes may have an array of ids for iphone, ipad and appletv
#endif
            Dictionary<string, object> currencyRewards;

            IAPProduct iapProduct = null;

#if BUY_CURRENCY_ENABLED && !STEAMWORKS_ENABLED
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
#endif
            for (int index = 0; index < jsonArray.Length; index++)
            {
                product = ((Dictionary<string, object>)jsonArray.GetValue(index));
                priceData = ((Dictionary<string, object>)product[KEY_PRICEDATA]);

#if UNITY_ANDROID
                var storeId = (string)priceData[KEY_ANDROIDID];
#elif UNITY_WEBGL
                var storeId = (string)product[KEY_PRODUCT_FBURL];
#elif UNITY_IOS
                var idsArray = priceData[KEY_IDS] as Array;
                idsObject = ((Dictionary<string, object>)idsArray.GetValue(0));
                var storeId = (string)idsObject[KEY_ITUNESID];
#elif STEAMWORKS_ENABLED
                var storeId = ((int)priceData[KEY_PRODUCTID]).ToString();
#else
                var storeId = "";
                try
                {
                    // TODO add more store integration here [SMRJ]
                    storeId = (string)priceData[KEY_ANDROIDID];
                }
                catch (Exception)
                {
                    continue;
                }
#endif
                var braincloudID = (string)product[KEY_PRODUCTID];
                var category = (string)product[KEY_CATEGORY];
                var title = (string)product[KEY_TITLE];
                var description = (string)product[KEY_DESCRIPTION];
                var imageUrl = (string)product[KEY_IMAGEURL];
                var referencePrice = Convert.ToDecimal(priceData[KEY_REFERENCEPRICE]);
                var price = referencePrice / 100;
                var isPromotion = (bool)priceData[KEY_ISPROMOTION];

                //get the value of the currency with the same name as the category
                currencyRewards = (Dictionary<string, object>)product[KEY_CURRENCY];

                // merge the peer / parent currencies within this dictionary as well
                if (product.ContainsKey(KEY_PARENT_CURRENCY))
                {
                    foreach (var item in product[KEY_PARENT_CURRENCY] as Dictionary<string, object>)
                    {
                        foreach (var currencyReward in item.Value as Dictionary<string, object>)
                        {
                            currencyRewards.Add(currencyReward.Key, currencyReward.Value);
                        }
                    }
                }

                if (product.ContainsKey(KEY_PEER_CURRENCY))
                {
                    foreach (var item in product[KEY_PEER_CURRENCY] as Dictionary<string, object>)
                    {
                        foreach (var currencyReward in item.Value as Dictionary<string, object>)
                        {
                            currencyRewards.Add(currencyReward.Key, currencyReward.Value);
                        }
                    }
                }

                object currencyVal;
                currencyRewards.TryGetValue(category, out currencyVal);

                var currencyValue = Convert.ToInt32(currencyVal);
                var type = (string)product[KEY_TYPE];

#if BUY_CURRENCY_ENABLED
                var productType = ProductType.Consumable;

                switch (type.ToLower())
                {
                    case PRODUCT_TYPE_SUBSCRIPTION:
                        productType = ProductType.Subscription;
                        break;
                    case PRODUCT_TYPE_NON_CONSUMABLE:
                        productType = ProductType.NonConsumable;
                        break;
                    case PRODUCT_TYPE_CONSUMABLE:
                    default:
                        //productType = ProductType.Consumable; -> Already set as default value
                        break;
                }
#endif

                Dictionary<string, object> packRewards = (Dictionary<string, object>)product[BrainCloudConsts.JSON_DATA];

#if BUY_CURRENCY_ENABLED
                iapProduct = new IAPProduct(braincloudID, storeId, productType, category, title, description, imageUrl, referencePrice, price, isPromotion, currencyValue, currencyRewards, packRewards);
#else
                iapProduct = new IAPProduct(braincloudID, storeId, category, title, description, imageUrl, referencePrice, price, isPromotion, currencyValue, currencyRewards, packRewards);
#endif

                iapProduct.PriceString = String.Format("{0:c}", referencePrice / 100);

                //update the regular price on sale items
                if (isPromotion && product.ContainsKey(KEY_DEFAULTPRICEDATA))
                {
                    var defaultPriceData = ((Dictionary<string, object>)product[KEY_DEFAULTPRICEDATA]);
                    var refPrice = Convert.ToDecimal(defaultPriceData[KEY_REFERENCEPRICE]);
#if UNITY_ANDROID
                    var regularId = (string)defaultPriceData[KEY_ANDROIDID];
#elif UNITY_WEBGL
                    var regularId = (string)product[KEY_PRODUCT_FBURL];
#elif STEAMWORKS_ENABLED
                    var regularId = ((int)defaultPriceData[KEY_PRODUCTID]).ToString();
#else
                    var ids = defaultPriceData[KEY_IDS] as Array;
                    var regIdVal = ((Dictionary<string, object>)ids.GetValue(0));
                    var regularId = (string)regIdVal[KEY_ITUNESID];
#endif

                    iapProduct.RegularPriceID = regularId;
                    iapProduct.RegularPrice = refPrice / 100;
                    iapProduct.RegularPriceString = String.Format("{0:c}", refPrice / 100);
#if BUY_CURRENCY_ENABLED && !STEAMWORKS_ENABLED // unity does not support steam works iap
                    builder.AddProduct(regularId, productType); //add this as a reference to the regular priced item
#endif
                }
#if BUY_CURRENCY_ENABLED && !STEAMWORKS_ENABLED // unity does not support steam works iap
                builder.AddProduct(iapProduct.StoreProductId, productType); //add to ConfigurationBuilder
#endif
                m_productInventory.Add(iapProduct); //add to List of products
            }
#if BUY_CURRENCY_ENABLED && !STEAMWORKS_ENABLED // unity does not support steam works iap
            UnityPurchasing.Initialize(this, builder);
#else
            GEventManager.TriggerEvent(GEventManager.ON_IAP_PRODUCTS_UPDATED);
#endif

        }

        /// <summary>
        /// returns a product that has the requested id.
        /// </summary>
        /// <param name="in_id"></param>
        /// <returns></returns>
        public IAPProduct GetIAPProductByStoreId(string in_id)
        {
            IAPProduct product = null;

            for (int index = 0; index < m_productInventory.Count; index++)
            {
                if (string.Equals(m_productInventory[index].StoreProductId, in_id))
                {
                    product = m_productInventory[index];
                }
            }
            return product;
        }

        /// <summary>
        /// returns a product that has the requested braincloud id.
        /// </summary>
        /// <param name="in_id"></param>
        /// <returns></returns>
        public IAPProduct GetIAPProductByBrainCloudID(string in_id)
        {
            IAPProduct product = null;

            for (int index = 0; index < m_productInventory.Count; index++)
            {
                if (string.Equals(m_productInventory[index].BrainCloudProductID, in_id))
                {
                    product = m_productInventory[index];
                }
            }
            return product;
        }


#if BUY_CURRENCY_ENABLED
        public Product GetProduct(string in_id)
        {
            Product product = m_StoreController.products.WithID(in_id);
            return product;
        }
#endif

        /// <summary>
        /// returns a list of products that have the requested category.
        /// </summary>
        /// <param name="in_category"></param>
        /// <returns></returns>
        public List<IAPProduct> GetIAPProductsByCategory(string in_category)
        {
            List<IAPProduct> list = new List<IAPProduct>();

            for (int index = 0; index < m_productInventory.Count; index++)
            {
                if (string.Equals(m_productInventory[index].Category, in_category))
                {
                    list.Add(m_productInventory[index]);
                }
            }

            return list;
        }

        /// <summary>
        /// returns list of products that have the requested ProductType (Subscriptions, Consumables or Non-Consumables)
        /// </summary>
        /// <param name="in_type"></param>
        /// <returns></returns>
        /// 
#if BUY_CURRENCY_ENABLED
        public List<IAPProduct> GetIAPProductsByType(ProductType in_type)
        {
            List<IAPProduct> list = new List<IAPProduct>();

            for (int index = 0; index < m_productInventory.Count; index++)
            {
                if (m_productInventory[index].Type == in_type)
                {
                    list.Add(m_productInventory[index]);
                }
            }

            return list;
        }
#endif

#if STEAMWORKS_ENABLED
        public void FinalizeSteamPurchase()
        {
            Dictionary<string, object> transactionData = new Dictionary<string, object>();
            transactionData[BrainCloudConsts.JSON_TRANS_ID] = m_delayedTransactionId;
            GCore.Wrapper.AppStoreService.FinalizePurchase("steam", m_delayedTransactionId, JsonWriter.Serialize(transactionData), m_successCallback, m_failureCallback);
            m_delayedTransactionId = "";
        }
#endif
        #endregion

        #region Private
        private void onSteamStartPurchaseSuccess(string in_json, object obj)
        {
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_json);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];

            m_delayedTransactionId = (string)jsonData[BrainCloudConsts.JSON_TRANSACTION_ID];
        }

        private void handlePurchase(string storeProductId, SuccessCallback in_success = null, FailureCallback in_failure = null)
        {
#if BUY_CURRENCY_ENABLED
            // dont process while purchasing
            if (IsPurchasing) return;

            m_successCallback = in_success;
            m_failureCallback = in_failure;

            // If Purchasing has been initialized ...
            if (IsInitialized())
            {
#if !STEAMWORKS_ENABLED
                
                // ... look up the Product reference with the general product identifier and the Purchasing 
                // system's products collection.

                Product product = m_StoreController.products.WithID(storeProductId);

                // If the look up found a product for this device's store and that product is ready to be sold ... 
                if (product != null && product.availableToPurchase)
                {
                    if (m_enableDebug)
                    {
                        GDebug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                    }

                    // ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed 
                    // asynchronously.
                    GStateManager.Instance.EnableLoadingSpinner(true);
                    IsPurchasing = true;
                    m_StoreController.InitiatePurchase(product);

                }
                // Otherwise ...
                else
                {
                    // ... report the product look-up failure situation  
                    if (m_enableDebug)
                    {
                        GDebug.LogError("BuyProductIDBuyProductID: FAIL. Not purchasing product, either it's not found or it's not available for purchase.");
                    }

                    if (m_failureCallback != null)
                    {
                        m_failureCallback.Invoke(-1, -1, "{'reason':'BuyProductID: FAIL. Not purchasing product, either it's not found or it's not available for purchase.'}", null);
                    }
                }
#else
                // simulate a successful initiate purchase 
                handleProcessPurchase(storeProductId, "");
#endif
    }
            // Otherwise ...
            else
            {
                // ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or 
                // retrying initiailization.
                if (m_enableDebug)
                {
                    GDebug.LogError("BuyProductID FAIL. Not initialized.");
                }

                if (m_failureCallback != null)
                {
                    m_failureCallback.Invoke(-1, -1, "{'reason':'BuyProductID FAIL. Not initialized.'}", null);
                }
            }
#endif
        }

        /// <summary>
        /// Update title, description and price based on the store's data on the product.
        /// </summary>
        private void updateStoreProductInfo()
        {
#if BUY_CURRENCY_ENABLED
            for (int index = 0; index < m_productInventory.Count; index++)
            {
                var iapProduct = m_productInventory[index];

                Product product = m_StoreController.products.WithID(iapProduct.StoreProductId);
                if (product != null)
                {
                    var meta = product.metadata;
                    iapProduct.Price = meta.localizedPrice;
                    iapProduct.Description = meta.localizedDescription;
                    iapProduct.PriceString = meta.localizedPriceString;

#if DEBUG_IAP_ENABLED
                    iapProduct.PriceString += " (" + meta.isoCurrencyCode + ")"; // show currency code when debugging
#endif
                    iapProduct.Title = meta.localizedTitle;

                    //update the regular price on sale items
                    if (iapProduct.IsPromotion)
                    {
                        Product regProduct = m_StoreController.products.WithID(iapProduct.RegularPriceID);
                        if (regProduct != null)
                        {
                            iapProduct.RegularPrice = regProduct.metadata.localizedPrice;
                            iapProduct.RegularPriceString = regProduct.metadata.localizedPriceString;
                        }
                    }
                }
            }
#endif
        }

#if BUY_CURRENCY_ENABLED
        private bool IsInitialized()
        {
#if !STEAMWORKS_ENABLED
            // Only say we are initialized if both the Purchasing references are set.
            return m_StoreController != null && m_StoreExtensionProvider != null;
#else
            return true;
#endif
        }
        // these may not be used in all platforms, lets disable these warnings
#pragma warning disable 414
        private static IStoreController m_StoreController;          // The Unity Purchasing system.
        private static IExtensionProvider m_StoreExtensionProvider; // The store-specific Purchasing subsystems.
#pragma warning restore 414

        private bool m_enableDebug = false;
#endif
        private List<IAPProduct> m_productInventory = new List<IAPProduct>();

#pragma warning disable 649
        private SuccessCallback m_successCallback;
        private FailureCallback m_failureCallback;

        private string m_delayedTransactionId = "";
#pragma warning restore 649
        private bool m_bIsPurchasing = false;
        #endregion

        #region IAP Debug

#if DEBUG_IAP_ENABLED
        public void ForceSuccess(IAPProduct in_product, SuccessCallback in_success, FailureCallback in_fail)
        {
            var itemsCount = in_product.CurrencyRewards.Count;
            // handle currecies
            foreach (KeyValuePair<string, object> entry in in_product.CurrencyRewards)
            {
                itemsCount--;
                var isLast = itemsCount == 0;

                Dictionary<string, object> currency = new Dictionary<string, object>();
                currency[BrainCloudConsts.JSON_CURRENCY_TYPE] = entry.Key;
                currency[BrainCloudConsts.JSON_CURRENCY_AMOUNT] = entry.Value;

                var cbSuccess = isLast ? in_success : null;
                // TODO!! do we actually want to do this.. maybe only allow is Testers to call this 
                GCore.Wrapper.Client.ScriptService.RunScript("AwardCurrency", JsonWriter.Serialize(currency), cbSuccess);
            }

            //check if there's a SuccessCallback and that there's no currencies
            if (in_success != null && in_product.CurrencyRewards.Count == 0)
            {
                //just handle rewards... currencies have been handled above
                Dictionary<string, object> jsonData = new Dictionary<string, object>();
                Dictionary<string, object> data = new Dictionary<string, object>();
                Dictionary<string, object> rewards = new Dictionary<string, object>();
                jsonData[BrainCloudConsts.JSON_DATA] = data;

                rewards[BrainCloudConsts.JSON_TOURNAMENT_REWARDS] = in_product.Rewards;
                data[BrainCloudConsts.JSON_TOURNAMENT_REWARDS] = rewards;

                in_success.Invoke(JsonWriter.Serialize(jsonData), null);
            }
        }
#endif
        #endregion
    }
}