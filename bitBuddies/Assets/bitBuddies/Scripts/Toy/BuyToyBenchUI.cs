using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuyToyBenchUI : ContentUIBehaviour
{
    [Header("Main UI")]
    [SerializeField] private TMP_Text ToyNameText;
    [SerializeField] private TMP_Text UnlockAmountText;
    [SerializeField] private TMP_Text LevelRequirementText;
    [SerializeField] private GameObject LockImage;
    [SerializeField] private Button BuyButton;
    [SerializeField] private GameObject PurchasedObject;

    private ToyBenchInfo _toyBenchInfo;
    private BuddysRoom _buddysRoom;
    public void Init(ToyBenchInfo in_toyBenchInfo)
    {
        _toyBenchInfo = in_toyBenchInfo;
        BuyButton.onClick.AddListener(OnBuyButton);
        _buddysRoom = FindFirstObjectByType<BuddysRoom>();
        InitializeUI();
    }

    protected override void InitializeUI()
    {
        SetupToyBenchesUI();

    }
    
    public void SetupToyBenchesUI()
    {
        var benchInfoList = GameManager.Instance.ToyBenchInfos;
        foreach(var benchInfo in benchInfoList)
        {
            if(benchInfo.BenchId.Equals(_toyBenchInfo.BenchId, StringComparison.OrdinalIgnoreCase))
            {
                _toyBenchInfo = benchInfo;
                break;
            }
        }
        
        ToyNameText.text = _toyBenchInfo.BenchId;
        if(_toyBenchInfo.LevelRequirement == 0)
        {
            LevelRequirementText.text = "";
        }
        else
        {
            LevelRequirementText.text = "Lv. " + _toyBenchInfo.LevelRequirement;
        }
        if(_toyBenchInfo.UnlockCost == 0)
        {
            UnlockAmountText.text = "Free";
        }
        else
        {
            UnlockAmountText.text = _toyBenchInfo.UnlockCost.ToString("#,#");
        }

        var childInfo = GameManager.Instance.SelectedAppChildrenInfo;
        var parentInfo = BrainCloudManager.Instance.UserInfo;
        if(childInfo.buddyLevel >= _toyBenchInfo.LevelRequirement &&
           parentInfo.Coins >= _toyBenchInfo.UnlockCost)
        {
            LockImage.SetActive(false);
            BuyButton.interactable = true;
        }
        else
        {
            LockImage.SetActive(true);
            BuyButton.interactable = false;
        }
        PurchasedObject.SetActive(false);
        var listOfOwnedToys = GameManager.Instance.SelectedAppChildrenInfo.ownedToys;
        foreach (var value in listOfOwnedToys)
        {
            if(value.Equals(_toyBenchInfo.BenchId, StringComparison.OrdinalIgnoreCase))
            {
                PurchasedObject.SetActive(true);
                BuyButton.interactable = false;
                break;
            }
        }
        if(listOfOwnedToys.Count == 0)
        {
            PurchasedObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        BuyButton.onClick.RemoveAllListeners();
    }

    private void OnBuyButton()
    {
        if(_toyBenchInfo.UnlockCost == 0)
        {
            StateManager.Instance.OpenConfirmPopUp("Are you sure?", $"Get the {_toyBenchInfo.BenchId} for free?", GiveToyToChild);
        }
        else
        {
            StateManager.Instance.OpenConfirmPopUp("Are you sure?", $"Buy {_toyBenchInfo.BenchId} for {_toyBenchInfo.UnlockCost}?", GiveToyToChild);
        }
    }
    
    private void GiveToyToChild()
    {
        ToyManager.Instance.ObtainToy(_toyBenchInfo.BenchId, BenchIsPurchased);
    }
    
    public void BenchIsPurchased()
    {
        PurchasedObject.SetActive(true);
        BuyButton.interactable = false;
        StateManager.Instance.RefreshScreen();
    }
}
