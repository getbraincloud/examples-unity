using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ParentMenu : ContentUIBehaviour
{
    [SerializeField] private Button OpenSettingsButton;
    [SerializeField] private TMP_Text UsernameText;
    [SerializeField] private TMP_Text LevelText;
    [SerializeField] private TMP_Text CoinsText;
    [SerializeField] private TMP_Text GemsText;
    [SerializeField] private TMP_Text ChildCountText;
    [SerializeField] private Transform BuddySpawnTransform;
    [SerializeField] private BuddyHouseInfo BuddyPrefab;
    [SerializeField] private GameObject MoveInPrefab;
    [SerializeField] private MysteryBoxPanelUI MysteryBoxPanelPrefab;
    
    private List<AppChildrenInfo> _appChildrenInfos;
    private AppChildrenInfo _newAppChildrenInfo;
    public AppChildrenInfo NewAppChildrenInfo
    {
        get { return _newAppChildrenInfo; }
        set { _newAppChildrenInfo = value; }
    }

    private const string CHILD_COUNT_TEXT = "Buddy Count: ";
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        InitializeUI();
        OpenSettingsButton.onClick.AddListener(OpenSettingsButtonOnClick);
        base.Awake();
    }

    protected override void InitializeUI()
    {
        UserInfo userInfo = BrainCloudManager.Instance.UserInfo;
        UsernameText.text = userInfo.Username.IsNullOrEmpty() ? "New User" : userInfo.Username;
        LevelText.text = userInfo.Level.ToString();
        CoinsText.text = userInfo.Coins.ToString();
        GemsText.text = userInfo.Gems.ToString();

        _appChildrenInfos = GameManager.Instance.AppChildrenInfos;
        ChildCountText.text = CHILD_COUNT_TEXT + _appChildrenInfos.Count;
        SetupHouses();
    }
    
    public void SetupHouses()
    {
        // Clear existing houses...
        for (int i = 0; i < BuddySpawnTransform.transform.childCount; i++)
        {
            Destroy(BuddySpawnTransform.transform.GetChild(i).gameObject);
        }

        foreach (AppChildrenInfo buddyHouse in _appChildrenInfos)
        {
            BuddyHouseInfo buddyHouseInfo = Instantiate(BuddyPrefab, BuddySpawnTransform);
            buddyHouseInfo.HouseInfo = buddyHouse;
            buddyHouseInfo.SetUpHouse();
        }
        
        Instantiate(MoveInPrefab, BuddySpawnTransform);
    }
    
    private void OpenSettingsButtonOnClick()
    {
        // what do other than open ?
        // Settings page shows: Volume slider, connect an email (attach email to anonymous account), about the app and log out
    }
    
    public void OpenMysteryBoxPanel()
    {
        Instantiate(MysteryBoxPanelPrefab, transform);
    }
    
    public void OpenConfirmDemolishPanel()
    {
        
    }
}
