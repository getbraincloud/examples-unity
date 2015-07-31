using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using BrainCloudSlots.Connection;
using LitJson;

namespace BrainCloudSlots.Lobby
{
    public class GameLobby : MonoBehaviour
    {

        [SerializeField]
        private GameObject m_profileWindow;
        [SerializeField]
        private GameObject m_gamesWindow;
        [SerializeField]
        private GameObject m_offersWindow;

        [SerializeField]
        private Text m_jackpotBalance;

        [SerializeField]
        private Text m_playerName;

        [SerializeField]
        private Text m_playerBalance;

        [SerializeField]
        private GameObject[] m_productButtons;

        private Dictionary<int, int> m_productValues;

        public void LoadGame(string aGameName)
        {
            m_specialOfferWindow.SetActive(false);
            if (BrainCloudStats.Instance.m_readyToPlay)
                Application.LoadLevel(aGameName);
        }

        IEnumerator UpdateJackpot()
        {
            while (true)
            {
                BrainCloudStats.Instance.ReadJackpotData();
                yield return new WaitForSeconds(15);
            }

        }

        public void UpdateCredits(string aResponse, object cbObject)
        {
            JsonData response = JsonMapper.ToObject(aResponse);
            BrainCloudStats.Instance.m_credits = int.Parse(response["data"]["currencyMap"]["Credits"]["balance"].ToString());
        }


        public void ShowProfile()
        {
            m_gamesWindow.SetActive(false);
            m_profileWindow.SetActive(true);
            m_offersWindow.SetActive(false);
            m_specialOfferWindow.SetActive(false);
            for (int i = 0; i < m_profileViews.Length; i++)
            {
                m_profileViews[i].SetActive(false);
            }
        }

        public void ShowSlotGames()
        {
            m_gamesWindow.SetActive(true);
            m_profileWindow.SetActive(false);
            m_offersWindow.SetActive(false);
            m_specialOfferWindow.SetActive(false);
            for (int i = 0; i < m_profileViews.Length; i++)
            {
                m_profileViews[i].SetActive(false);
            }
        }

        public void ShowOffers()
        {
            m_gamesWindow.SetActive(false);
            m_profileWindow.SetActive(false);
            m_offersWindow.SetActive(true);
            m_specialOfferWindow.SetActive(false);
            for (int i = 0; i < m_profileViews.Length; i++)
            {
                m_profileViews[i].SetActive(false);
            }
        }

        // Use this for initialization
        void Start()
        {
            if (BrainCloudStats.Instance.m_showOffersPage)
            {
                m_gamesWindow.SetActive(false);
                m_profileWindow.SetActive(false);
                m_offersWindow.SetActive(true);
                m_productValues = new Dictionary<int, int>() { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };
                UpdateProductButtons(BrainCloudStats.Instance.m_productData);
                BrainCloudStats.Instance.m_showOffersPage = false;
            }
            else
            {
                m_productValues = new Dictionary<int, int>() { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 } };
            }

            StartCoroutine(UpdateJackpot());

        }

        [SerializeField]
        private GameObject m_specialOfferButton;

        [SerializeField]
        private GameObject m_specialOfferWindow;

        public void ShowSpecialOfferConfirmation()
        {
            m_specialOfferWindow.SetActive(true);
        }

        public void HideSpecialOfferConfirmation(bool aConfirm = false)
        {
            if (aConfirm)
            {
                BuyCredits(6);
            }
            m_specialOfferWindow.SetActive(false);
        }

        public void UpdateProductButtons(JsonData aData)
        {
            m_productPrices = new Dictionary<int, string>();
            for (int i = 0; i < m_productButtons.Length; i++)
            {
                m_productValues[int.Parse(aData[i]["itemId"].ToString())] = int.Parse(aData[i]["currency"]["Credits"].ToString());
                m_productButtons[i].transform.GetChild(0).GetComponent<Text>().text = aData[i]["title"].ToString() + " -- " + float.Parse(aData[i]["priceData"]["price"].ToString()).ToString("C", new System.Globalization.CultureInfo("en-us"));
                m_productPrices.Add(int.Parse(aData[i]["itemId"].ToString()), float.Parse(aData[i]["priceData"]["price"].ToString()).ToString("C", new System.Globalization.CultureInfo("en-us")));
            }
            m_productValues[int.Parse(aData[5]["itemId"].ToString())] = int.Parse(aData[5]["currency"]["Credits"].ToString());
            m_specialOfferButton.transform.GetChild(0).GetComponent<Text>().text = aData[5]["title"].ToString();
            m_specialOfferWindow.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = float.Parse(aData[5]["priceData"]["price"].ToString()).ToString("C", new System.Globalization.CultureInfo("en-us"));
            m_productPrices.Add(int.Parse(aData[5]["itemId"].ToString()), float.Parse(aData[5]["priceData"]["price"].ToString()).ToString("C", new System.Globalization.CultureInfo("en-us")));

        }

        private Dictionary<int, string> m_productPrices;

        //NOTE:
        //This is not an actual implementation for microtransactions, this is just a placeholder
        public void BuyCredits(int aProduct)
        {
            BrainCloudWrapper.GetBC().ProductService.AwardCurrency("Credits", (ulong)m_productValues[aProduct], UpdateCredits, null, null);
            JsonData userData = BrainCloudStats.Instance.m_userData["data"];
            string purchaseString = "{\"date\" : \"" + System.DateTime.Now.ToShortDateString() + "\",\"time\" : \"" + System.DateTime.Now.ToShortTimeString() + "\", \"amount\" : " + m_productValues[aProduct] + ", \"price\" : \"" + m_productPrices[aProduct] + "\"}";
            JsonData purchase = JsonMapper.ToObject(purchaseString);
            userData["purchaseHistory"].Add(purchase);
            BrainCloudStats.Instance.m_userData["data"] = userData;
            BrainCloudWrapper.GetBC().EntityService.UpdateEntity(BrainCloudStats.Instance.m_userData["entityId"].ToString(), "userData", userData.ToJson(),null, -1, ProfileUpdated, ProfileUpdateFailed, null);
        }

        [SerializeField]
        private InputField[] m_nameFields;

        public void UpdateNameFields()
        {
            JsonData userData = BrainCloudStats.Instance.m_userData["data"];
            userData["firstName"] = m_nameFields[0].text;
            userData["lastName"] = m_nameFields[1].text;
            userData["email"] = m_nameFields[2].text;
            BrainCloudStats.Instance.m_userData["data"] = userData;
            BrainCloudWrapper.GetBC().EntityService.UpdateEntity(BrainCloudStats.Instance.m_userData["entityId"].ToString(), "userData", userData.ToJson(), null, -1, ProfileUpdated, ProfileUpdateFailed, null);
        }

        public void ProfileUpdated(string responseData, object cbObject)
        {
            JsonData response = JsonMapper.ToObject(responseData);
            BrainCloudStats.Instance.m_userData = response["data"];
        }

        public void ProfileUpdateFailed(int a, int b, string responseData, object cbObject)
        {
            Debug.Log(a);
            Debug.Log(b);
            Debug.Log(responseData);
        }

        [SerializeField]
        public GameObject[] m_profileViews;

        public void EnableProfileView(int aView)
        {
            for (int i = 0; i < m_profileViews.Length; i++)
            {
                if (i != aView)
                    m_profileViews[i].SetActive(false);
                else
                    m_profileViews[i].SetActive(true);
            }
        }

        public void HideProfileView()
        {
            for (int i = 0; i < m_profileViews.Length; i++)
            {
                m_profileViews[i].SetActive(false);
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            m_playerName.text = BrainCloudStats.Instance.m_userName;
            m_playerBalance.text = BrainCloudStats.Instance.m_credits.ToString("C0", new System.Globalization.CultureInfo("en-us"));
            m_jackpotBalance.text = BrainCloudStats.Instance.m_progressiveJackpot.ToString("C0", new System.Globalization.CultureInfo("en-us"));
            if (m_profileWindow.activeSelf)
            {
                int totalWinnings = int.Parse(BrainCloudStats.Instance.m_userData["data"]["lifetimeWins"].ToString());
                string joinDate = BrainCloudStats.Instance.m_userData["data"]["joinDateTime"]["date"].ToString();
                string temp = "";
                int[] date = new int[3];
                int count = 0;
                for (int i = 0; i < joinDate.Length; i++)
                {
                    if (joinDate[i] != '/')
                        temp += joinDate[i];
                    else
                    {
                        date[count] = int.Parse(temp);
                        temp = "";
                        count++;
                    }
                }
                date[count] = int.Parse(temp);
                System.DateTime daysPassed = new System.DateTime(date[2], date[0], date[1]);
                System.TimeSpan days = System.DateTime.Now - daysPassed;

                m_profileWindow.transform.GetChild(1).FindChild("Username").GetComponent<Text>().text = BrainCloudStats.Instance.m_userName;
                m_profileWindow.transform.GetChild(1).FindChild("Data").GetComponent<Text>().text =
                    BrainCloudStats.Instance.m_credits.ToString("C0", new System.Globalization.CultureInfo("en-us")) + "\n" +
                    totalWinnings.ToString("C0", new System.Globalization.CultureInfo("en-us")) + "\n" +
                    int.Parse(BrainCloudStats.Instance.m_userData["data"]["biggestWin"]["amount"].ToString()).ToString("C0", new System.Globalization.CultureInfo("en-us")) + "     " + BrainCloudStats.Instance.m_userData["data"]["biggestWin"]["date"].ToString() + "  " + BrainCloudStats.Instance.m_userData["data"]["biggestWin"]["time"].ToString() + "\n" +
                    (totalWinnings / ((days.Days < 1) ? 1 : days.Days)).ToString("C0", new System.Globalization.CultureInfo("en-us")) + "\n" +
                    "\n" +
                    BrainCloudStats.Instance.m_userData["data"]["joinDateTime"]["date"].ToString() + "  " + BrainCloudStats.Instance.m_userData["data"]["joinDateTime"]["time"].ToString();


            }
            else
            {
                try
                {
                    m_nameFields[0].text = BrainCloudStats.Instance.m_userData["data"]["firstName"].ToString();
                    m_nameFields[1].text = BrainCloudStats.Instance.m_userData["data"]["lastName"].ToString();
                    m_nameFields[2].text = BrainCloudStats.Instance.m_userData["data"]["email"].ToString();
                }
                catch (System.NullReferenceException e)
                {

                }

                m_profileWindow.transform.GetChild(2).FindChild("EntryNumber").GetComponent<Text>().text = "";
                m_profileWindow.transform.GetChild(2).FindChild("Date").GetComponent<Text>().text = "";
                m_profileWindow.transform.GetChild(2).FindChild("Amount").GetComponent<Text>().text = "";
                m_profileWindow.transform.GetChild(2).FindChild("Price").GetComponent<Text>().text = "";

                try
                {
                    
                    JsonData purchases = BrainCloudStats.Instance.m_userData["data"]["purchaseHistory"];
                    JsonData[] orderedPurchases = new JsonData[purchases.Count];
                    for (int i=0;i<purchases.Count;i++)
                    {
                        orderedPurchases[i] = purchases[(purchases.Count - 1) - i];
                    }

                    for (int i=0;i<orderedPurchases.Length;i++)
                    {
                        m_profileWindow.transform.GetChild(2).FindChild("EntryNumber").GetComponent<Text>().text += (orderedPurchases.Length-i).ToString() + "\n";
                        m_profileWindow.transform.GetChild(2).FindChild("Date").GetComponent<Text>().text += orderedPurchases[i]["date"].ToString() + " " + orderedPurchases[i]["time"] + "\n";
                        m_profileWindow.transform.GetChild(2).FindChild("Amount").GetComponent<Text>().text += orderedPurchases[i]["amount"].ToString() + "\n";
                        m_profileWindow.transform.GetChild(2).FindChild("Price").GetComponent<Text>().text += orderedPurchases[i]["price"].ToString() + "\n";
                    }
                    
                }
                catch (System.NullReferenceException e)
                {

                }

                try
                {
                    m_profileWindow.transform.GetChild(5).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = BrainCloudStats.Instance.m_termsConditionsString;
                }
                catch (System.NullReferenceException e)
                {

                }

            }
        }
    }
}