using System.Collections.Generic;
using UnityEngine;
using Gameframework;
namespace BrainCloudUNETExample
{
    public class StoreSubState : BaseSubState
    {
        public static string STATE_NAME = "store";

        #region BaseState
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();
            // fetch products
            List<IAPProduct> products = GIAPManager.Instance.GetIAPProductsByCategory("Cosmetic");

            Transform content = this.transform.FindDeepChild("Content");
            for (int i = 0; i < content.childCount; ++i)
            {
                Destroy(content.GetChild(i).gameObject);
            }

            GameObject tempObj;
            IAPProduct tempData;
            StoreProductCard tempCard;
            for (int i = 0; i < products.Count; ++i)
            {
                tempData = products[i];
                tempObj = GEntityFactory.Instance.CreateResourceAtPath("Prefabs/UI/StoreProductCard", content);
                tempCard = tempObj.GetComponent<StoreProductCard>();
                tempCard.LateInit(tempData);
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        #endregion

        #region Public 
        public void OnRestorePurchases()
        {
            //TODO: Implement Restore Purchases
            //https://docs.unity3d.com/Manual/UnityIAPRestoringTransactions.html
        }
        #endregion

        #region Private
        #endregion
    }
}
