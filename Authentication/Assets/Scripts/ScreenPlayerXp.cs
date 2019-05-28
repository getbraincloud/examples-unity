using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.LitJson;

public class ScreenPlayerXp : BCScreen
{
    int m_playerXp = 0;
    int m_playerLevel = 0;
    string m_incrementXp = "0";

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
    IDictionary<string, Currency> m_currencies = new Dictionary<string, Currency>();

    private string[] m_currencyTypes =
    {
        "gems",
        "gold",
        "gems"
    };

    public ScreenPlayerXp(BrainCloudWrapper bc) : base(bc) { }

    public override void Activate()
    {
        _bc.PlayerStateService.ReadUserState(ReadPlayerState_Success, Failure_Callback);
        m_mainScene.AddLogNoLn("[ReadPlayerState]... ");
    }

    private void ReadPlayerState_Success(string json, object cb)
    {
        m_mainScene.AddLog("SUCCESS");
        m_mainScene.AddLogJson(json);
        m_mainScene.AddLog("");

        JsonData jObj = JsonMapper.ToObject(json);
        m_playerLevel = (int) jObj["data"]["experienceLevel"];
        m_playerXp = (int) jObj["data"]["experiencePoints"];

        // now grab our currencies
        foreach (string curType in m_currencyTypes)
        {
            _bc.ProductService.GetCurrency(curType, GetPlayerVC_Success, Failure_Callback);
            m_mainScene.AddLogNoLn("[GetPlayerVC (" + curType +")]... ");
        }
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
        }
    }

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
                    _bc.ProductService.AwardCurrency(c.currencyType, valueAsULong, GetPlayerVC_Success, Failure_Callback);
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
                    _bc.ProductService.ConsumeCurrency(c.currencyType, valueAsULong, GetPlayerVC_Success, Failure_Callback);
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
            _bc.ProductService.ResetCurrency(ResetPlayerVC_Success, Failure_Callback);
#pragma warning restore 618
            m_mainScene.AddLogNoLn("[ResetPlayerVC]... ");
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    public void IncrementXp_Success(string json, object cbObject)
    {
        base.Success_Callback(json, cbObject);

        //{"status":200,"data":{"statisticsExceptions":{},"milestones":{},"experiencePoints":0,"quests":{},"experienceLevel":0,"statistics":{"wood":75}}}
        JsonData jObj = JsonMapper.ToObject(json);
        m_playerLevel = (int) jObj["data"]["experienceLevel"];
        m_playerXp = (int) jObj["data"]["experiencePoints"];

        // rewards?
    }

    public void ResetPlayerVC_Success(string json, object cbObject)
    {
        m_currencies.Clear();

        _bc.PlayerStateService.ReadUserState(ReadPlayerState_Success, Failure_Callback);
        m_mainScene.AddLogNoLn("[ReadPlayerState]... ");
    }
}
