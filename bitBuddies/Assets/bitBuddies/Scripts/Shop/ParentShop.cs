using BrainCloud.JSONHelper;
using Gameframework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Setup for the parent shop UI and logic.
/// </summary>
public class ParentShop : Shop
{
    [SerializeField] private ShopItem shopItemPrefab;
    [SerializeField] private TMP_Text FakeMoneyBalanceText;
    [SerializeField] private Button GetMoreFakeMoneyButton;

    private void OnEnable()
    {
        GetMoreFakeMoneyButton.onClick.AddListener(OnGetMoreFakeMoney);
    }

    /// <summary>
    /// Setup the shop UI to show fake money balance and listing shop items.
    /// </summary>
    public override void SetupShop()
    {
        var userInfo = BrainCloudManager.Instance.CurrentUserInfo;
        if (userInfo.FakeMoney > 0)
        {
            FakeMoneyBalanceText.text = $"${BrainCloudManager.Instance.CurrentUserInfo.FakeMoney.ToString("#,#")}";
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

    /// <summary>
    /// Button reaction for getting more fake money.
    /// </summary>
    private void OnGetMoreFakeMoney()
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
        //Update fake money balance
        Dictionary<string, object> data = jsonResponse.Deserialize("data");
        Dictionary<string, object> response = data["response"] as Dictionary<string, object>;
        var fakeDollarObject = response["fakeDollarsMap"] as Dictionary<string, object>;
        int fakeDollarBalance = (int)fakeDollarObject["balance"];
        BrainCloudManager.Instance.CurrentUserInfo.UpdateFakeMoney(fakeDollarBalance);
        StateManager.Instance.RefreshScreen();
    }
}
