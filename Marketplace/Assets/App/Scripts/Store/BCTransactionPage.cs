using System;
using System.Collections.Generic;

/// <summary>
/// <see cref="BrainCloudMarketplace.GetTransactionHistory(Action{bool, BCTransactionPage}, int, int, Dictionary{string, object})"/>
/// calls Cloud Code that will retreive JSON from brainCloud for the logged in user's transaction data.
/// This JSON data can be deserialized into this class.
/// 
/// <br><seealso cref="BrainCloud.BrainCloudAppStore"/></br>
/// </summary>
[Serializable]
public class BCTransactionPage
{
    public bool moreAfter;
    public bool moreBefore;
    public int count;
    public int page;
    public BCTransactionItem[] items;

    public BCTransactionPage() { }

    /// <summary>
    /// Each transaction item and its details that is listed within <see cref="items"/>
    /// </summary>
    [Serializable]
    public class BCTransactionItem
    {
        public int refPrice;
        public bool pending;
        public bool sandbox;
        public string type;
        public string title;
        public string itemId;

        public BCTransactionItem() { }
    }
}
