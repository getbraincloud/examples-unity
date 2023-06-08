using BrainCloud;
using System.Collections.Generic;

#if BUY_CURRENCY_ENABLED && !UNITY_WEBGL
using UnityEngine.Purchasing;
#endif

namespace Gameframework
{
    /// <summary>
    /// Container for IAP Product is built from the product_inventory of the Product->GetInventory call
    /// </summary>
    /// <remarks>
    /// Sample product from itunes platform
    /// {
    ///		"gameId": "11381",
    ///		"itemId": "simbuxPack1",
    ///		"type": "Consumable",
    ///		"iTunesSubscriptionType": null,
    ///		"category": "SimBux",
    ///		"title": "Fistful of SimBux",
    ///		"description": null,
    ///		"imageUrl": null,
    ///		"currency": {
    ///			"SimBux": 80
    ///		},
    ///		"parentCurrency": {},
    ///		"peerCurrency": {},
    ///		"data": null,
    ///		"priceData": {
    ///			"referencePrice": 199,
    ///			"isPromotion": false,
    ///			"ids": [{
    ///				"appId": "iphone",
    ///				"itunesId": "simbuxPack1"
    ///			}]
    ///		}
    ///	}
    /// </remarks>
    public class IAPProduct
    {
#region Public
        public string BrainCloudProductID { get; private set; }
        public string StoreProductId { get; private set; }

#if BUY_CURRENCY_ENABLED && !UNITY_WEBGL
        public ProductType Type { get; private set; }
#endif
        public string Category { get; private set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public string LongDescription { get; set; }
        public string ImageUrl { get; private set; }
        public decimal ReferencePrice { get; private set; }
        public decimal Price { get; set; }
        public string PriceString { get; set; }
        public string RegularPriceID { get; set; }
        public decimal RegularPrice { get; set; }
        public string RegularPriceString { get; set; }
        public bool IsPromotion { get; private set; }
        public int CurrencyValue { get; private set; }
        public Dictionary<string, object> CurrencyRewards { get; private set; }
        public Dictionary<string, object> ExtraRewards { get; private set; }
        public Dictionary<string, object> Rewards { get { return m_rewards; } }

        public IAPProduct(
            string in_bcID,
            string in_productId,

#if BUY_CURRENCY_ENABLED && !UNITY_WEBGL
            ProductType in_type,
#endif
            string in_category,
            string in_title,
            string in_description,
            string in_imageUrl,
            decimal in_referencePrice,
            decimal in_price,
            bool in_isPromotion,
            int in_currencyValue,
            Dictionary<string, object> in_currencyRewards,
            Dictionary<string, object> in_packRewards)
        {
            this.BrainCloudProductID = in_bcID;
            this.StoreProductId = in_productId;

#if BUY_CURRENCY_ENABLED && !UNITY_WEBGL
            this.Type = in_type;
#endif
            this.Category = in_category;
            this.Title = in_title;
            this.Description = in_description;
            this.ImageUrl = in_imageUrl;
            this.ReferencePrice = in_referencePrice;
            this.Price = in_price;
            this.IsPromotion = in_isPromotion;
            this.CurrencyValue = in_currencyValue;
            this.CurrencyRewards = in_currencyRewards;
            this.ExtraRewards = in_packRewards;

            string shortDescription = "";
            string longDescription = "";
            HudHelper.ParseJsonDescription(in_description, ref shortDescription, ref longDescription);
            this.ShortDescription = shortDescription;
            this.LongDescription = longDescription;

            setRewards();
        }

        public void Buy(SuccessCallback in_success, FailureCallback in_failure)
        {
            GIAPManager.Instance.BuyProductID(BrainCloudProductID, in_success, in_failure);
        }
#endregion

#region Private
        private Dictionary<string, object> m_rewards;

        private void setRewards()
        {
            m_rewards = new Dictionary<string, object>(this.CurrencyValue);

            if (this.ExtraRewards != null && ExtraRewards.Count > 0)
            {
                foreach (var reward in this.ExtraRewards)
                    m_rewards.Add(reward.Key, reward.Value);
            }
        }
#endregion
    }
}