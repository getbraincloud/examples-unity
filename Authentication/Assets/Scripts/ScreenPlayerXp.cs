﻿using UnityEngine;
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

    private class Currency
    {
        public string currencyType;
        public int purchased;
        public int balance;
        public int consumed;
        public int awarded;

        public string award = "0";
        public string consume = "0";
    }
    IDictionary<string, Currency> m_currencies;

    //private string[] m_currencyTypes =
    //{
    //    "gems",
    //    "gold",
    //    "gems"
    //};

    private List<string> m_currencyTypes; 

    public ScreenPlayerXp(BrainCloudWrapper bc) : base(bc) { }

    public override void Activate(BrainCloudWrapper bc)
    {
        _bc = bc;
        m_currencies = new Dictionary<string, Currency>();
        m_currencyTypes = new List<string>();
        m_currencyTypes.Add("gems");
        m_currencyTypes.Add("gold");
        m_currencyTypes.Add("gems");
        _bc.PlayerStateService.ReadUserState(ReadPlayerState_Success, Failure_Callback);
        m_mainScene.AddLogNoLn("[ReadPlayerState]... ");
    }


    //*************** UI Event Methods ***************
    public void OnIncrementXP()
    {
        int valueAsInt = 0;

        if (int.TryParse(m_incrementXp, out valueAsInt))
        {
            _bc.PlayerStatisticsService.IncrementExperiencePoints(valueAsInt, IncrementXp_Success, Failure_Callback);
            m_mainScene.AddLogNoLn("[IncrementXp]... ");
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

        SuccessCallback successCallback = (response, cbObject) => { Debug.Log(string.Format("Success | {0}", response)); };
        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.ScriptService.RunScript(scriptName, jsonScriptData, successCallback, failureCallback);
        _bc.VirtualCurrencyService.GetCurrency("gems", GetPlayerVC_Success, Failure_Callback);
    }

    public void OnConsumeCurrency()
    {
        string scriptName = "ConsumeCurrency";
        string jsonScriptData = "{\"vcID\": \"gems\", \"vcAmount\": " + m_currencyToConsume + "}";

        SuccessCallback successCallback = (response, cbObject) => { Debug.Log(string.Format("Success | {0}", response)); };
        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.ScriptService.RunScript(scriptName, jsonScriptData, successCallback, failureCallback);
        _bc.VirtualCurrencyService.GetCurrency("gems", GetPlayerVC_Success, Failure_Callback);
    }

    public void OnResetCurrency()
    {
        string scriptName = "ResetCurrency";
        string jsonScriptData = "{}";

        SuccessCallback successCallback = (response, cbObject) => { Debug.Log(string.Format("Success | {0}", response)); };
        FailureCallback failureCallback = (status, code, error, cbObject) => { Debug.Log(string.Format("Failed | {0}  {1}  {2}", status, code, error)); };

        _bc.ScriptService.RunScript(scriptName, jsonScriptData, successCallback, failureCallback);
        _bc.VirtualCurrencyService.GetCurrency("gems", GetPlayerVC_Success, Failure_Callback);
    }


    //*************** Success Callbacks ***************
    private void ReadPlayerState_Success(string json, object cb)
    {
        m_mainScene.AddLog("SUCCESS");
        m_mainScene.AddLogJson(json);
        m_mainScene.AddLog("");

        JsonData jObj = JsonMapper.ToObject(json);
        m_playerLevel = (int) jObj["data"]["experienceLevel"];
        m_playerXp = (int) jObj["data"]["experiencePoints"];

        //AnthonyTODO: adding UI stuff
        playerLevelText.text = m_playerLevel.ToString();
        playerXPText.text = m_playerXp.ToString();

        _bc.VirtualCurrencyService.GetCurrency("gems", GetPlayerVC_Success, Failure_Callback);

        //// now grab our currencies
        //foreach (string curType in m_currencyTypes)
        //{
        //    _bc.VirtualCurrencyService.GetCurrency(curType, GetPlayerVC_Success, Failure_Callback);
        //    m_mainScene.AddLogNoLn("[GetPlayerVC (" + curType +")]... ");
        //}
    }

    private void GetPlayerVC_Success(string json, object cb)
    {
        /*
        "data"   : {
            "updatedAt" : 1392919197588,
            "currencyMap" : {
                "gold" : {
                    "purchased" : 0,
                    "balance"   : 10,
                    "consumed"  : 0,
                    "awarded"   : 10
                }
            }
        */

        m_mainScene.AddLog("SUCCESS");
        m_mainScene.AddLogJson(json);
        m_mainScene.AddLog("");

        JsonData jObj = JsonMapper.ToObject(json);
        JsonData jCurMap = jObj ["data"] ["currencyMap"];
        System.Collections.IDictionary dCurMap = jCurMap as System.Collections.IDictionary;

        foreach (string key in dCurMap.Keys)
        {
            Currency c = null;
            if (m_currencies.ContainsKey(key))
            {
                c = m_currencies[key];
            }
            else
            {
                c = new Currency();
                m_currencies[key] = c;
            }
            c.currencyType = key;
            c.purchased = (int) jCurMap[key]["purchased"];
            c.balance = (int) jCurMap[key]["balance"];
            c.consumed = (int) jCurMap[key]["consumed"];
            c.awarded = (int) jCurMap[key]["awarded"];

            int purchasedAsInt = (int)jCurMap[key]["purchased"];
            int balanceAsInt = (int)jCurMap[key]["balance"];
            int consumeddAsInt = (int)jCurMap[key]["consumed"];
            int awardedAsInt = (int)jCurMap[key]["awarded"];

            currencyPurchasedText.text = purchasedAsInt.ToString();
            currencyBalanceText.text = balanceAsInt.ToString();
            currencyConsumedText.text = consumeddAsInt.ToString();
            currencyAwardedText.text = awardedAsInt.ToString(); 
        }
    }

    public void IncrementXp_Success(string json, object cbObject)
    {
        base.Success_Callback(json, cbObject);

        //{"status":200,"data":{"statisticsExceptions":{},"milestones":{},"experiencePoints":0,"quests":{},"experienceLevel":0,"statistics":{"wood":75}}}
        JsonData jObj = JsonMapper.ToObject(json);
        m_playerLevel = (int) jObj["data"]["experienceLevel"];
        m_playerXp = (int) jObj["data"]["experiencePoints"];

        playerLevelText.text = m_playerLevel.ToString();
        playerXPText.text = m_playerXp.ToString(); 

        // rewards?
    }

    public void ResetPlayerVC_Success(string json, object cbObject)
    {
        m_currencies.Clear();

        _bc.PlayerStateService.ReadUserState(ReadPlayerState_Success, Failure_Callback);
        m_mainScene.AddLogNoLn("[ReadPlayerState]... ");
    }

    #region Stuff To Remove
    public override void OnScreenGUI()
    {
        int minLeftWidth = 120;

        // player level
        GUILayout.BeginHorizontal();
        GUILayout.Box("Player Level", GUILayout.MinWidth(minLeftWidth));
        GUILayout.Box(m_playerLevel.ToString());
        GUILayout.EndHorizontal();

        // player xp
        GUILayout.BeginHorizontal();
        GUILayout.Box("Player Xp", GUILayout.MinWidth(minLeftWidth));
        GUILayout.Box(m_playerXp.ToString());
        GUILayout.EndHorizontal();

        // increment xp
        GUILayout.BeginHorizontal();
        GUILayout.Space(minLeftWidth);
        m_incrementXp = GUILayout.TextField(m_incrementXp, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("IncrementXp"))
        {
            int valueAsInt = 0;
            if (int.TryParse(m_incrementXp, out valueAsInt))
            {
                _bc.PlayerStatisticsService.IncrementExperiencePoints(valueAsInt, IncrementXp_Success, Failure_Callback);
                m_mainScene.AddLogNoLn("[IncrementXp]... ");
            }
        }
        GUILayout.EndHorizontal();

        foreach (Currency c in m_currencies.Values)
        {
            // currency values
            GUILayout.BeginHorizontal();
            GUILayout.Box("Currency " + c.currencyType , GUILayout.MinWidth(minLeftWidth));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Box("Balance", GUILayout.MinWidth(minLeftWidth));
            GUILayout.Box(c.balance.ToString());

            GUILayout.Box("Awarded", GUILayout.MinWidth(minLeftWidth));
            GUILayout.Box(c.awarded.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Box("Consumed", GUILayout.MinWidth(minLeftWidth));
            GUILayout.Box(c.consumed.ToString());

            GUILayout.Box("Purchased", GUILayout.MinWidth(minLeftWidth));
            GUILayout.Box(c.purchased.ToString());
            GUILayout.EndHorizontal();

            // award currency
            GUILayout.BeginHorizontal();
            GUILayout.Space(minLeftWidth);
            c.award = GUILayout.TextField(c.award, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Award"))
            {
                ulong valueAsULong = 0;
                if (ulong.TryParse(c.award, out valueAsULong))
                {
#pragma warning disable 618
                    _bc.VirtualCurrencyService.AwardCurrency(c.currencyType, valueAsULong, GetPlayerVC_Success, Failure_Callback);
#pragma warning restore 618
                    m_mainScene.AddLogNoLn("[AwardPlayerVC " + c.currencyType +"]... ");
                }
            }
            GUILayout.EndHorizontal();

            // consume currency
            GUILayout.BeginHorizontal();
            GUILayout.Space(minLeftWidth);
            c.consume = GUILayout.TextField(c.consume, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Consume"))
            {
                ulong valueAsULong = 0;
                if (ulong.TryParse(c.consume, out valueAsULong))
                {
#pragma warning disable 618
                    _bc.VirtualCurrencyService.ConsumeCurrency(c.currencyType, valueAsULong, GetPlayerVC_Success, Failure_Callback);
#pragma warning restore 618
                    m_mainScene.AddLogNoLn("[ConsumePlayerVC " + c.currencyType +"]... ");
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset All Currencies"))
        {
#pragma warning disable 618
            _bc.VirtualCurrencyService.ResetCurrency(ResetPlayerVC_Success, Failure_Callback);
#pragma warning restore 618
            m_mainScene.AddLogNoLn("[ResetPlayerVC]... ");
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
    #endregion
}
