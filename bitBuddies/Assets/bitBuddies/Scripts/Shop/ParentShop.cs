using System;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParentShop : Shop
{
    [SerializeField] private ParentShopItem ParentShopItemPrefab;
    [SerializeField] private TMP_Text FakeMoneyBalanceText; 
    [SerializeField] private Button GetMoreFakeMoneyButton;

    private void OnEnable()
    {
        GetMoreFakeMoneyButton.onClick.AddListener(GetMoreFakeMoney);
    }
    
    public override void RefreshShopScreen()
    {
        base.RefreshShopScreen();
        SetupShop();
    }

    public void SetupShop()
    {
        var userInfo = BrainCloudManager.Instance.CurrentUserInfo;
        if(userInfo.FakeMoney > 0)
        {
            FakeMoneyBalanceText.text = $"${BrainCloudManager.Instance.CurrentUserInfo.FakeMoney.ToString("#,#")}";
        }
        else
        {
            FakeMoneyBalanceText.text = "$ 0";
        }
        
        if (ItemSpawnPoint.transform.childCount > 0)
            return;
            
        var shopItems = GameManager.Instance.ParentShopInfos;
        foreach (var shopItem in shopItems)
        {
            var parentShopItem = Instantiate(ParentShopItemPrefab, ItemSpawnPoint.transform);
            parentShopItem.Init(shopItem);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GetMoreFakeMoneyButton.onClick.RemoveAllListeners();
    }

    private void GetMoreFakeMoney()
    {
        Dictionary<string, object> scriptData = new Dictionary<string, object>();
        scriptData.Add("increaseAmount", 10);
        BrainCloudManager.Client.ScriptService.RunScript(
            BitBuddiesConsts.AWARD_MONEY_SCRIPT_NAME, 
            scriptData.Serialize(), 
            BrainCloudManager.HandleSuccess("Awarded money successful", OnGetMoreMoneySuccess)
        );
    }
    
    private void OnGetMoreMoneySuccess(string jsonResponse)
    {
        /*
         * {"packetId":3,"responses":[{"data":{"runTimeData":{"hasIncludes":false,"compileTime":2929,"scriptSize":324,
         * "renderTime":3,"executeTime":13047},"response":{"fakeDollarsMap":{"consumed":0,"balance":50,"purchased":0,"awarded":50,"revoked":0}},
         * "success":true,"reasonCode":null},"status":200}]}
         */
        Dictionary<string, object> data = jsonResponse.Deserialize("data");
        Dictionary<string, object> response = data["response"] as Dictionary<string, object>;
        var fakeDollarObject = response["fakeDollarsMap"] as Dictionary<string, object>;
        int fakeDollarBalance = (int) fakeDollarObject["balance"];
        BrainCloudManager.Instance.CurrentUserInfo.UpdateFakeMoney(fakeDollarBalance);
        StateManager.Instance.RefreshScreen();
    }
}
