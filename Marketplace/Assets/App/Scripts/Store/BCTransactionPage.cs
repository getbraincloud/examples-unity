using System;

/// <summary>
/// 
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
