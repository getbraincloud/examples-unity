using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing.Extension;

public class BrainCloudStore : IStore
{
    private static BrainCloudStore store = null;

    private IStoreCallback callback;

    public void Initialize(IStoreCallback callback)
    {
        this.callback = callback;
    }

    public void RetrieveProducts(System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.Purchasing.ProductDefinition> products)
    {
        // Fetch product information and invoke callback.OnProductsRetrieved();
    }

    public void Purchase(UnityEngine.Purchasing.ProductDefinition product, string developerPayload)
    {
        // Start the purchase flow and call either callback.OnPurchaseSucceeded() or callback.OnPurchaseFailed()
    }

    public void FinishTransaction(UnityEngine.Purchasing.ProductDefinition product, string transactionId)
    {
        // Perform transaction related housekeeping 
    }
}
