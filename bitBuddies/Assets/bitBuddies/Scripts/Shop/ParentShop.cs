using System.Collections.Generic;
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
    
    public void SetupShop()
    {
        var shopItems = GameManager.Instance.ParentShopInfos;
        foreach (var shopItem in shopItems)
        {
            var parentShopItem = Instantiate(ParentShopItemPrefab, ItemSpawnPoint.transform);
            parentShopItem.Init(shopItem);
        }
        var userInfo = BrainCloudManager.Instance.CurrentUserInfo;
        if(userInfo.FakeMoney > 0)
        {
            FakeMoneyBalanceText.text = $"${BrainCloudManager.Instance.CurrentUserInfo.FakeMoney.ToString("#,#")}";
        }
        else
        {
            FakeMoneyBalanceText.text = "$ 0";
        }

        GetMoreFakeMoneyButton.onClick.AddListener(GetMoreFakeMoney);
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
            BrainCloudManager.HandleSuccess("Awarded money successful", OnGetMoreMoneySuccess),
            BrainCloudManager.HandleFailure("Failed to award money", OnGetMoreMoneyFailure)
        );
    }
    
    private void OnGetMoreMoneySuccess(string jsonResponse)
    {
        /*
         * {"packetId":3,"responses":[{"data":{"runTimeData":{"hasIncludes":false,"compileTime":1663,"scriptSize":290,
         * "renderTime":4,"executeTime":12105},"response":{"getResult":{"data":{"currencyMap":{"gems":{"consumed":0,"balance":20,
         * "purchased":0,"awarded":20,"revoked":0},"coins":{"consumed":156000,"balance":1778,"purchased":0,"awarded":157778,"revoked":0},
         * "fakeDollars":{"consumed":0,"balance":10,"purchased":0,"awarded":10,"revoked":0}}},"status":200}},
         * "success":true,"reasonCode":null},"status":200}]}
         */
    }
    
    private void OnGetMoreMoneyFailure()
    {
        
    }

}
