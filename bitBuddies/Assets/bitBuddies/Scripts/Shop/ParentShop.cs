using BrainCloud.JSONHelper;
using Gameframework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParentShop : Shop
{
    [SerializeField] private ShopItem shopItemPrefab;
    [SerializeField] private TMP_Text FakeMoneyBalanceText;
    [SerializeField] private Button GetMoreFakeMoneyButton;

    private void OnEnable()
    {
        GetMoreFakeMoneyButton.onClick.AddListener(OnGetMoreFakeMoney);
    }

    public override void SetupShop()
    {
        var userInfo = BrainCloudManager.Instance.CurrentUserInfo;
        if (userInfo.FakeMoney > 0)
        {
            FakeMoneyBalanceText.text = $"${BrainCloudManager.Instance.CurrentUserInfo.FakeMoney:#,#}";
        }
        else
        {
            FakeMoneyBalanceText.text = "$ 0";
        }

        if (ItemSpawnPoint.transform.childCount > 0)
            return;

        List<ShopInfo> shopItems = GameManager.Instance.ParentShopInfos;
        foreach (ShopInfo shopItem in shopItems)
        {
            var parentShopItem = Instantiate(shopItemPrefab, ItemSpawnPoint.transform);
            parentShopItem.Init(shopItem);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GetMoreFakeMoneyButton.onClick.RemoveAllListeners();
    }

    private void OnGetMoreFakeMoney()
    {
        var scriptData = new Dictionary<string, object>
        {
            { "increaseAmount", 10 }
        };

        BrainCloudManager.Client.ScriptService.RunScript(BitBuddiesConsts.AWARD_MONEY_SCRIPT_NAME,
                                                         scriptData.Serialize(),
                                                         BrainCloudManager.HandleSuccess("Awarded money successful", OnGetMoreMoneySuccess));
    }

    private void OnGetMoreMoneySuccess(string jsonResponse)
    {
        // Update fake money balance
        var data = jsonResponse.Deserialize("data", "response", "fakeDollarsMap");

        BrainCloudManager.Instance.CurrentUserInfo.UpdateFakeMoney(data.GetValue<int>("balance"));

        StateManager.Instance.RefreshScreen();
    }
}
