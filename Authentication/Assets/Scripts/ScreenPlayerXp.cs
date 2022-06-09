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

    public override void Activate()
    {
        GameEvents.instance.OnUpdateLevelAndXP += UpdateLevelAndXP;
        GameEvents.instance.onGetVirtualCurrency += GetVirtualCurrency;

        m_currencyTypes = new List<string>();
        m_currencyTypes.Add("gems");
        m_currencyTypes.Add("gold");
        m_currencyTypes.Add("gems");

        BrainCloudInterface.instance.ReadUserState(this);
        BrainCloudInterface.instance.GetVirtualCurrency("gems");

        if (helpMessage == null)
        {
            helpMessage =   "The XP portion of the XP/Currency screen allows you to increase the player level by incrementing player XP by a provided amount. " +
                            "XP levels are also capable of interacting with specified user statistics. " +
                            "Player levels are defined under the \"XP Levels\" page within the \"Gamification\" tab of the portal. " +
                            "XP and player level can be monitored through the \"User Summary\" page of the \"User Monitoring\" tab.\n\n" +
                            "The virtual currency portion of the XP/Currency screen retreives a virtual currency called \"gems\". " +
                            "Gems must be defined on the \"Virtual Currencies\" page under the \"Marketplace\" tab. " +
                            "User's virtual currency balance can be monitored on the \"Virtual Currency\" Page of \"User Monitoring.\"";
        }

        if (helpURL == null)
        {
            helpURL = "https://getbraincloud.com/apidocs/apiref/?cloudcode#capi-virtualcurrency";
        }
    }

    //*************** Game Event Subscribed Methods ***************
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

    protected override void OnDisable()
    {
        GameEvents.instance.OnUpdateLevelAndXP -= UpdateLevelAndXP;
        GameEvents.instance.onGetVirtualCurrency -= GetVirtualCurrency;
    }
}
