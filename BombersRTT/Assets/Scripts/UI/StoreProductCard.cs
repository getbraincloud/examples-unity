using Gameframework;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BrainCloudUNETExample
{
    public class StoreProductCard : BaseBehaviour
    {
        public TextMeshProUGUI Title = null;
        public TextMeshProUGUI Label = null;
        public TextMeshProUGUI ValueCost = null;
        public TextMeshProUGUI TimerDisplayLabel = null;
        public TextMeshProUGUI Description = null;
        public Button BuyButton = null;
        public Image UpperImage = null;
        public Image TitleIcon = null;
        public Image Spinner = null;
        public GameObject UpperBG = null;

        // sale container
        public GameObject SaleContainer = null;
        public TextMeshProUGUI SalePercentage = null;
        public TextMeshProUGUI RegularPrice = null;
        public TextMeshProUGUI SalePrice = null;

        public GameObject TicketsNonSale = null;
        public GameObject TicketSale = null;

        #region public
        public IAPProduct ProductData { get { return m_product; } }
        public void LateInit(IAPProduct in_data)
        {
            m_product = in_data;
            updateInfo();

            if (m_product.ImageUrl != null && m_product.ImageUrl != "")
            {
                GStateManager.Instance.CurrentState.StartCoroutine(loadImage(m_product.ImageUrl));
            }
            else
            {
                UpperBG.SetActive(true);
                Spinner.gameObject.SetActive(false);
            }
        }

        // Custom LateInit for the XP Boosts cards
        public void LateInit(string in_description, string in_price)
        {
            in_description = in_description.Replace("\\n", "\n");
            Description.text = in_description;
            ValueCost.text = in_price;
        }

        IEnumerator loadImage(string url)
        {
            UpperBG.SetActive(false);
            Spinner.gameObject.SetActive(true);
            Texture2D texture;

            using (UnityWebRequest www = new UnityWebRequest(url))
            {
                www.downloadHandler = new DownloadHandlerTexture();
                yield return www.SendWebRequest();
                texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            }

            if (UpperImage != null) UpperImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
            if (UpperBG != null) UpperBG.SetActive(true);
            if (Spinner != null) Spinner.gameObject.SetActive(false);

            yield return null;
        }

        public void OnBuy()
        {
            if (!m_clickedOnce)
            {
                m_clickedOnce = true;
                Invoke("ReEnableClicks", 2);
                m_product.Buy(OnBuySuccess, OnBuyFailed);
            }
        }

        public void OnBuyFailed(int status, int reasonCode, string jsonError, object cbObject)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
            GPlayerMgr.Instance.HandleFailedPurchaseEvent(status, reasonCode, jsonError, cbObject);
        }

        public void OnBuySuccess(string jsonResponse, object cbObject)
        {
            GStateManager.Instance.EnableLoadingSpinner(false);
#if STEAMWORKS_ENABLED
            if (GPlayerMgr.Instance.HandleFinalizePurchaseSuccess(jsonResponse, cbObject))
            {
                TriggerResultsAndDestroy();
            }
#else
            TriggerResultsAndDestroy();
#endif
        }
        #endregion

        #region private
        // Continue the OnBuySuccess flow after the flourish FX
        private void TriggerResultsAndDestroy()
        {
            GStateManager.Instance.CurrentState.SetUIEnabled(true);
            HudHelper.DisplayMessageDialog("SUCCESS", "YOU JUST PURCHASED " + m_product.Title, "OK");

            GSoundMgr.PlaySound("purchaseSuccess");
            //remove it if its a timed deal card
            if (m_product != null && m_resetTime > 0)
            {
                Destroy(gameObject);
            }
        }

        private void updateInfo()
        {
            // load from IAP?
            if (m_product != null)
            {
                if (TicketsNonSale != null) TicketsNonSale.SetActive(false);
                if (TicketSale != null) TicketSale.SetActive(false);

                long currencyValue = HudHelper.GetLongValue(m_product.CurrencyRewards, "coins");
                if (currencyValue > 0)
                {
                    //TitleIcon.sprite = CoinIconSprite;
                }

                Label.text = HudHelper.ToGUIString(currencyValue) + " " + m_product.Category;

                string desc = m_product.LongDescription;
                desc = desc.Replace("\\n", "\n");
                Description.text = desc;

                Title.text = m_product.Title;
                ValueCost.text = m_product.PriceString;

                SaleContainer.SetActive(m_product.IsPromotion);
                if (m_product.IsPromotion && m_product.RegularPrice > 0)
                {
                    RegularPrice.text = m_product.RegularPriceString;
                    SalePrice.text = m_product.PriceString;
                    SalePercentage.text = "" + HudHelper.QuickRound((1.0f - (float)(m_product.Price / m_product.RegularPrice)) * 100.0f) + "%";
                }

                // Check if this is the Gold Wing product
                // If we have already purchased it, update the button to reflect this.
                if (m_product.BrainCloudProductID.Equals("GoldWings") && GPlayerMgr.Instance.GetCurrencyBalance(GBomberRTTConfigManager.CURRENCY_GOLD_WINGS) > 0)
                {
                    ValueCost.text = "Purchased";
                    BuyButton.interactable = false;
                }
            }
        }

        private void ReEnableClicks()
        {
            m_clickedOnce = false;
        }

        private IEnumerator updateTimerCoroutine()
        {
            GPlayerMgr playerManager = GPlayerMgr.Instance;
            if (m_resetTime > 0)
            {
                long lTime = (long)(m_resetTime - playerManager.CurrentServerTime);
                float fTime = (float)lTime;

                while (fTime > 0.0f)
                {
                    yield return YieldFactory.GetWaitForSeconds(1.0f);
                    fTime -= 1000.0f;
                    if (TimerDisplayLabel != null)
                    {
                        TimerDisplayLabel.transform.parent.gameObject.SetActive(true);
                        TimerDisplayLabel.text = HudHelper.ConvertUnixTimeToGUIString((int)fTime);
                    }
                }

                //remove this from the list
                Destroy(gameObject);
            }
            yield return null;
        }

        private ulong m_resetTime = 0;

        private IAPProduct m_product = null;
        private bool m_clickedOnce = false;
        #endregion
    }
}
