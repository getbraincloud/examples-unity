using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.LitJson;
using UnityEngine.UI; 

public class ScreenPlayerXp : BCScreen
{
    string m_incrementXp = "0";
    string m_currencyToAward = "0";
    string m_currencyToConsume = "0";

    //UI Elements
    [SerializeField] InputField xpField;
    [SerializeField] InputField awardField;
    [SerializeField] InputField consumeField;
    [SerializeField] Text playerXPText;
    [SerializeField] Text playerLevelText;
    [SerializeField] Text currencyBalanceText;
    [SerializeField] Text currencyConsumedText;
    [SerializeField] Text currencyAwardedText;
    [SerializeField] Text currencyPurchasedText; 

    private List<string> m_currencyTypes;

    private void Awake()
    {
        if (HelpMessage == null)
        {
            HelpMessage =   "The XP portion of the XP/Currency screen allows you to increase the player level by incrementing player XP by a provided amount. " +
                            "XP levels are also capable of interacting with specified user statistics. " +
                            "Player levels are defined under the \"XP Levels\" page within the \"Gamification\" tab of the portal. " +
                            "XP and player level can be monitored through the \"User Summary\" page of the \"User Monitoring\" tab.\n\n" +
                            "The virtual currency portion of the XP/Currency screen retreives a virtual currency called \"gems\". " +
                            "Gems must be defined on the \"Virtual Currencies\" page under the \"Marketplace\" tab. " +
                            "User's virtual currency balance can be monitored on the \"Virtual Currency\" Page of \"User Monitoring.\"";
        }

        if (HelpURL == null)
        {
            HelpURL = "https://getbraincloud.com/apidocs/apiref/?cloudcode#capi-virtualcurrency";
        }
    }

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

        if(dataManager.Currencies.ContainsKey("gems"))
        {
            currencyPurchasedText.text = dataManager.Currencies["gems"].Purchased.ToString();
            currencyBalanceText.text = dataManager.Currencies["gems"].Balance.ToString();
            currencyConsumedText.text = dataManager.Currencies["gems"].Consumed.ToString();
            currencyAwardedText.text = dataManager.Currencies["gems"].Awarded.ToString();
            return;
        }

        TextLogger.instance.AddLog("Ensure that \"gems\" was created in Virtual Currencies under the Marketplace tab.");
        Debug.LogWarning("Ensure that \"gems\" was created in Virtual Currencies under the Marketplace tab.");
    }

    //*************** UI Subscribed Methods ***************
    public void OnIncrementXP()
    {
        m_incrementXp = xpField.text; 

        int valueAsInt = 0;

        if (int.TryParse(m_incrementXp, out valueAsInt))
        {
            BrainCloudInterface.instance.IncrementExperiencePoints(valueAsInt);
            BrainCloudInterface.instance.GetVirtualCurrency("gems"); 
        }
    }

    public void OnAwardCurrency()
    {
        m_currencyToAward = awardField.text; 

        string scriptName = "AwardCurrency";
        string jsonScriptData = "{\"vcID\": \"gems\", \"vcAmount\": " + m_currencyToAward + "}";
        BrainCloudInterface.instance.RunCloudCodeScript(scriptName, jsonScriptData);

        BrainCloudInterface.instance.GetVirtualCurrency("gems");
    }

    public void OnConsumeCurrency()
    {
        m_currencyToConsume = consumeField.text;

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
