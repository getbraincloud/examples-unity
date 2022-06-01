using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.LitJson;
using UnityEngine.UI; 

public class ScreenPlayerXp : BCScreen
{
    int m_playerXp = 0;
    int m_playerLevel = 0;
    string m_incrementXp = "0";
    string m_currencyToAward = "0";
    string m_currencyToConsume = "0";

    //UI Elements
    [SerializeField] Text playerXPText;
    [SerializeField] Text playerLevelText;
    [SerializeField] Text currencyBalanceText;
    [SerializeField] Text currencyConsumedText;
    [SerializeField] Text currencyAwardedText;
    [SerializeField] Text currencyPurchasedText; 

    private List<string> m_currencyTypes; 

    public ScreenPlayerXp(BrainCloudWrapper bc) : base(bc) { }

    private void Start()
    {
        GameEvents.instance.OnUpdateLevelAndXP += UpdateLevelAndXP;
        GameEvents.instance.onGetVirtualCurrency += GetVirtualCurrency;
    }

    public override void Activate(BrainCloudWrapper bc)
    {
        _bc = bc;

        m_currencyTypes = new List<string>();
        m_currencyTypes.Add("gems");
        m_currencyTypes.Add("gold");
        m_currencyTypes.Add("gems");

        BrainCloudInterface.instance.ReadUserState(this);
        BrainCloudInterface.instance.GetVirtualCurrency("gems");
    }

    //*************** GameEvents ***************

    void UpdateLevelAndXP()
    {
        playerLevelText.text = DataManager.instance.PlayerLevel.ToString();
        playerXPText.text = DataManager.instance.PlayerXP.ToString();
    }

    void GetVirtualCurrency()
    {
        DataManager dataManager = DataManager.instance;

        currencyPurchasedText.text = dataManager.Currencies["gems"].purchased.ToString();
        currencyBalanceText.text = dataManager.Currencies["gems"].balance.ToString();
        currencyConsumedText.text = dataManager.Currencies["gems"].consumed.ToString();
        currencyAwardedText.text = dataManager.Currencies["gems"].awarded.ToString();
    }

    //*************** UI Subscribed Methods ***************
    public void OnIncrementXP()
    {
        int valueAsInt = 0;

        if (int.TryParse(m_incrementXp, out valueAsInt))
        {
            BrainCloudInterface.instance.IncrementExperiencePoints(valueAsInt);
        }
    }
    public void OnIncrementXPEndEdit(string xpToIncrement)
    {
        int valueAsInt = 0;

        if (int.TryParse(xpToIncrement, out valueAsInt))
        {
            m_incrementXp = xpToIncrement;
        }
        else
        {
            Debug.Log("Value entered must be a number!"); 
        }
    }

    public void OnAwardCurrencyFieldEndEdit(string currency)
    {
        ulong valueAsUlong = 0; 

        if(ulong.TryParse(currency, out valueAsUlong))
        {
            m_currencyToAward = currency; 
        }
        else
        {
            Debug.Log("Value entered must be a number!"); 
        }
    }

    public void OnConsumeCurrencyFieldEndEdit(string currency)
    {
        ulong valueAsUlong = 0; 

        if(ulong.TryParse(currency, out valueAsUlong))
        {
            m_currencyToConsume = currency; 
        }
        else
        {
            Debug.Log("Value entered must be a number!");
        }
    }

    public void OnAwardCurrency()
    {
        string scriptName = "AwardCurrency";
        string jsonScriptData = "{\"vcID\": \"gems\", \"vcAmount\": " + m_currencyToAward + "}";
        BrainCloudInterface.instance.RunCloudCodeScript(scriptName, jsonScriptData);

        BrainCloudInterface.instance.GetVirtualCurrency("gems");
    }

    public void OnConsumeCurrency()
    {
        string scriptName = "ConsumeCurrency";
        string jsonScriptData = "{\"vcID\": \"gems\", \"vcAmount\": " + m_currencyToConsume + "}";
        BrainCloudInterface.instance.RunCloudCodeScript(scriptName, jsonScriptData);

        BrainCloudInterface.instance.GetVirtualCurrency("gems");
    }

    public void OnResetCurrency()
    {
        string scriptName = "ResetCurrency";
        string jsonScriptData = "{}";
        BrainCloudInterface.instance.RunCloudCodeScript(scriptName, jsonScriptData);

        BrainCloudInterface.instance.GetVirtualCurrency("gems");
    }


    #region Stuff To Remove
//    public override void OnScreenGUI()
//    {
//        int minLeftWidth = 120;

//        // player level
//        GUILayout.BeginHorizontal();
//        GUILayout.Box("Player Level", GUILayout.MinWidth(minLeftWidth));
//        GUILayout.Box(m_playerLevel.ToString());
//        GUILayout.EndHorizontal();

//        // player xp
//        GUILayout.BeginHorizontal();
//        GUILayout.Box("Player Xp", GUILayout.MinWidth(minLeftWidth));
//        GUILayout.Box(m_playerXp.ToString());
//        GUILayout.EndHorizontal();

//        // increment xp
//        GUILayout.BeginHorizontal();
//        GUILayout.Space(minLeftWidth);
//        m_incrementXp = GUILayout.TextField(m_incrementXp, GUILayout.ExpandWidth(true));
//        if (GUILayout.Button("IncrementXp"))
//        {
//            int valueAsInt = 0;
//            if (int.TryParse(m_incrementXp, out valueAsInt))
//            {
//                _bc.PlayerStatisticsService.IncrementExperiencePoints(valueAsInt, IncrementXp_Success, Failure_Callback);
//                m_mainScene.AddLogNoLn("[IncrementXp]... ");
//            }
//        }
//        GUILayout.EndHorizontal();

//        foreach (Currency c in m_currencies.Values)
//        {
//            // currency values
//            GUILayout.BeginHorizontal();
//            GUILayout.Box("Currency " + c.currencyType , GUILayout.MinWidth(minLeftWidth));
//            GUILayout.FlexibleSpace();
//            GUILayout.EndHorizontal();

//            GUILayout.BeginHorizontal();
//            GUILayout.Box("Balance", GUILayout.MinWidth(minLeftWidth));
//            GUILayout.Box(c.balance.ToString());

//            GUILayout.Box("Awarded", GUILayout.MinWidth(minLeftWidth));
//            GUILayout.Box(c.awarded.ToString());
//            GUILayout.EndHorizontal();

//            GUILayout.BeginHorizontal();
//            GUILayout.Box("Consumed", GUILayout.MinWidth(minLeftWidth));
//            GUILayout.Box(c.consumed.ToString());

//            GUILayout.Box("Purchased", GUILayout.MinWidth(minLeftWidth));
//            GUILayout.Box(c.purchased.ToString());
//            GUILayout.EndHorizontal();

//            // award currency
//            GUILayout.BeginHorizontal();
//            GUILayout.Space(minLeftWidth);
//            c.award = GUILayout.TextField(c.award, GUILayout.ExpandWidth(true));
//            if (GUILayout.Button("Award"))
//            {
//                ulong valueAsULong = 0;
//                if (ulong.TryParse(c.award, out valueAsULong))
//                {
//#pragma warning disable 618
//                    _bc.VirtualCurrencyService.AwardCurrency(c.currencyType, valueAsULong, GetPlayerVC_Success, Failure_Callback);
//#pragma warning restore 618
//                    m_mainScene.AddLogNoLn("[AwardPlayerVC " + c.currencyType +"]... ");
//                }
//            }
//            GUILayout.EndHorizontal();

//            // consume currency
//            GUILayout.BeginHorizontal();
//            GUILayout.Space(minLeftWidth);
//            c.consume = GUILayout.TextField(c.consume, GUILayout.ExpandWidth(true));
//            if (GUILayout.Button("Consume"))
//            {
//                ulong valueAsULong = 0;
//                if (ulong.TryParse(c.consume, out valueAsULong))
//                {
//#pragma warning disable 618
//                    _bc.VirtualCurrencyService.ConsumeCurrency(c.currencyType, valueAsULong, GetPlayerVC_Success, Failure_Callback);
//#pragma warning restore 618
//                    m_mainScene.AddLogNoLn("[ConsumePlayerVC " + c.currencyType +"]... ");
//                }
//            }
//            GUILayout.EndHorizontal();
//        }

//        GUILayout.BeginHorizontal();
//        if (GUILayout.Button("Reset All Currencies"))
//        {
//#pragma warning disable 618
//            _bc.VirtualCurrencyService.ResetCurrency(ResetPlayerVC_Success, Failure_Callback);
//#pragma warning restore 618
//            m_mainScene.AddLogNoLn("[ResetPlayerVC]... ");
//        }
//        GUILayout.FlexibleSpace();
//        GUILayout.EndHorizontal();
//    }
    #endregion
}
