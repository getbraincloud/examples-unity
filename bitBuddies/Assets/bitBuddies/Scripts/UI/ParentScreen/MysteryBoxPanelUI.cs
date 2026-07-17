using BrainCloud.JsonFx.Json;
using BrainCloud.JSONHelper;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class does 2 things
/// Displays mystery boxes for user to select if they have the funds
/// After a box is selected, this class handles 3 pages of dialog for a user opening a mystery box
/// </summary>
public class MysteryBoxPanelUI : ContentUIBehaviour
{
    [SerializeField] private TMP_Text TitleText;
    [SerializeField] private Button CloseButton;
    [SerializeField] private Transform MysteryBoxSpawnPoint;
    [SerializeField] private MysteryBoxUI MysteryBoxPrefab;
    [SerializeField] private Button DoneButton; //for page 3, closes the whole panel but captures the data input
    [SerializeField] private TMP_InputField NameBuddyInput;	// for page 3
    [SerializeField] private Button OpenBoxButton;	// for page 2
    [SerializeField] private List<GameObject> _mysteryScreens;   //0 = selection, 1 = open box, 2 = name buddy,display stats etc

    [SerializeField] private TMP_Text CoinMultiplierText;
    [SerializeField] private TMP_Text CoinPerHourText;
    [SerializeField] private TMP_Text CoinCapacityText;
    [SerializeField] private TMP_Text RarityText;
    [SerializeField] private TMP_Text BuddyTypeNameText;
    [SerializeField] private Image BuddyImage;

    private MysteryBoxInfo _mysteryBoxInfo;
    public MysteryBoxInfo MysteryBoxInfo
    {
        set { _mysteryBoxInfo = value; }
    }

    private List<MysteryBoxInfo> _mysteryBoxes;
    private int _screenIndex;
    private ParentMenu _parentMenu;
    private string _buddyName;
    private RectTransform _rectTransform;
    private bool _receivedResponse;

    //Screen Titles
    private const string LIST_BOXES_TEXT_TITLE = "Pick a mystery box";
    private const string OPEN_BOX_TEXT_TITLE = "Open your Mystery Box";
    private const string NEW_BUDDY_TEXT_TITLE = "New bitBuddy!";

    //screen 3 text presets
    private const string COIN_PAYOUT_TEXT = "Coin Payouts ";
    private const string COIN_GAIN_TEXT = "Idle Coin Gains ";
    private const string COIN_PER_HOUR_TEXT = "/hr";
    private const string COIN_CAPACITY_TEXT = "Idle Coins Capacity ";
    private bool isOpeningBox = false;

    protected override void Awake()
    {
        InitializeUI();
        foreach (MysteryBoxInfo mysteryBoxInfo in _mysteryBoxes)
        {
            var box = Instantiate(MysteryBoxPrefab, MysteryBoxSpawnPoint);
            box.Init(mysteryBoxInfo);
        }
        for (int i = 0; i < _mysteryScreens.Count; i++)
        {
            _mysteryScreens[i].SetActive(false);
        }
        _rectTransform = OpenBoxButton.GetComponent<RectTransform>();
        _parentMenu = FindAnyObjectByType<ParentMenu>();
        _mysteryScreens[0].SetActive(true);
        TitleText.text = LIST_BOXES_TEXT_TITLE;
        _screenIndex = 0;
        OpenBoxButton.onClick.AddListener(OnOpenBox);
        CloseButton.onClick.AddListener(OnCloseButton);
        DoneButton.onClick.AddListener(OnDoneButton);
        base.Awake();
    }

    protected override void InitializeUI()
    {
        _mysteryBoxes = GameManager.Instance.MysteryBoxes;
    }

    private void OnOpenBox()
    {
        //Open another screen where we Animate the box opening
        // After box is opened, we show another screen where the user 
        // picks the name of buddy

        if (isOpeningBox) return;
        isOpeningBox = true;
        _receivedResponse = false;
        string scriptName = "";
        switch (_mysteryBoxInfo.RarityEnum)
        {
            case Rarity.starter:
                scriptName = BitBuddiesConsts.AWARD_STARTER_BUDDY_SCRIPT_NAME;
                break;
            case Rarity.basic:
                scriptName = BitBuddiesConsts.AWARD_BASIC_LOOTBOX_SCRIPT_NAME;
                break;
            case Rarity.rare:
                scriptName = BitBuddiesConsts.AWARD_RARE_LOOTBOX_SCRIPT_NAME;
                break;
            case Rarity.superRare:
                scriptName = BitBuddiesConsts.AWARD_SUPER_RARE_LOOTBOX_SCRIPT_NAME;
                break;
            case Rarity.legendary:
                scriptName = BitBuddiesConsts.AWARD_LEGENDARY_LOOTBOX_SCRIPT_NAME;
                break;
        }

        Dictionary<string, object> scriptData = new Dictionary<string, object>
        {
            {"childAppId", BitBuddiesConsts.APP_CHILD_ID}
        };
        BrainCloudManager.Wrapper.ScriptService.RunScript
        (
            scriptName,
            scriptData.Serialize(),
            BrainCloudManager.HandleSuccess("Award new buddy Success", OnGetLootboxInfo),
            BrainCloudManager.HandleFailure("Award new buddy Failure", OnFailureCallback)
        );

        StartCoroutine(Shake());
    }

    private void OnGetLootboxInfo(string jsonResponse)
    {
        BrainCloudManager.Instance.OnAddChildProfile(jsonResponse);

        _receivedResponse = true;
        var listOfBuddies = GameManager.Instance.AppChildrenInfos;

        if (listOfBuddies.Count > 0)
        {
            _parentMenu.NewAppChildrenInfo = new AppChildrenInfo();

            var data = jsonResponse.Deserialize("data", "response");

            BuddyTypeNameText.text = data.GetJSONObject("newBuddy")?.GetString("name");

            // At this point the newly created child profile should not have a name yet and thats what we use to determine the new user.
            if (data.GetJSONArray("children") is var children && children != null && children.Length > 0)
            {
                foreach (var child in children)
                {
                    var childName = child["profileName"] as string;
                    if (childName.IsNullOrEmpty())
                    {
                        _parentMenu.NewAppChildrenInfo.profileId = child.GetString("profileId");

                        var buddyInfo = child.GetJSONObject("buddyInfo");

                        _parentMenu.NewAppChildrenInfo.rarity = buddyInfo.GetValue<Rarity>("rarity");
                        _parentMenu.NewAppChildrenInfo.coinPerHour = buddyInfo.GetValue<int>("coinPerHour");
                        _parentMenu.NewAppChildrenInfo.maxCoinCapacity = buddyInfo.GetValue<int>("maxCoinCapacity");
                        _parentMenu.NewAppChildrenInfo.buddySpritePath = buddyInfo.GetString("buddySpritePath") is string path && !string.IsNullOrWhiteSpace(path) ? path : BitBuddiesConsts.DEFAULT_SPRITE_PATH_FOR_BUDDY;
                        _parentMenu.NewAppChildrenInfo.coinMultiplier = buddyInfo.GetValue<double>("coinMultiplier") is double mult && mult > 0.0 ? (float)mult : 1.0f;
                        _parentMenu.NewAppChildrenInfo.lastIdleTimestamp = buddyInfo.GetDateTime("lastIdleTimestamp");

                        SetupBuddyDataDisplay();
                        break;
                    }
                }
            }
        }
    }

    private void OnFailureCallback()
    {
        // TODO: ???
    }

    IEnumerator Shake()
    {
        var duration = 0.5f;
        var magnitude = 1f;
        var range = 10f;
        Vector3 originalPos = _rectTransform.anchoredPosition;
        float elapsed = 0f;

        //Wait for the response from the server for the lootbox info
        while (!_receivedResponse)
        {
            float x = UnityEngine.Random.Range(-range, range) * magnitude;
            float y = UnityEngine.Random.Range(-range, range) * magnitude;
            _rectTransform.anchoredPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            yield return new WaitForFixedUpdate();
        }

        //Shake abit more after the response incase its a quick one.
        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-range, range) * magnitude;
            float y = UnityEngine.Random.Range(-range, range) * magnitude;
            _rectTransform.anchoredPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        _rectTransform.anchoredPosition = originalPos;
        NextPage();
    }

    public void NextPage()
    {
        if (_screenIndex < _mysteryScreens.Count - 1)
        {
            _mysteryScreens[_screenIndex].SetActive(false);
            _screenIndex++;
            _mysteryScreens[_screenIndex].SetActive(true);
            if (_screenIndex == 1)
            {
                TitleText.text = OPEN_BOX_TEXT_TITLE;
            }
            else
            {
                TitleText.text = NEW_BUDDY_TEXT_TITLE;
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDoneButton()
    {
        if (NameBuddyInput.text.IsNullOrEmpty())
        {
            _buddyName = BitBuddiesConsts.DEFAULT_BUDDY_NAME + UnityEngine.Random.Range(1, 1000);
            PopUpUI.Show("Name is empty", false)
               .AddBodyText($"Are you sure you want to give your buddy a generated name? ({_buddyName})")
               .AddButton("Close", PopUpUI.ButtonColor.Blue, null)
               .AddButton("Confirm", PopUpUI.ButtonColor.Green, OnConfirmEmptyName);
            return;
        }

        BrainCloudManager.Instance.UpdateChildProfileName(NameBuddyInput.text, _parentMenu.NewAppChildrenInfo.profileId, DestroySelf);
    }

    //If the name is empty, this is the callback for that to send a generated name instead of one assigned from user
    private void OnConfirmEmptyName()
    {
        Destroy(gameObject);
        BrainCloudManager.Instance.UpdateChildProfileName(_buddyName, _parentMenu.NewAppChildrenInfo.profileId, DestroySelf);
        StateManager.Instance.RefreshScreen();
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void SetupBuddyDataDisplay()
    {
        var childAppInfo = _parentMenu.NewAppChildrenInfo;
        CoinMultiplierText.text = COIN_PAYOUT_TEXT + childAppInfo.coinMultiplier + "x";
        CoinPerHourText.text = COIN_GAIN_TEXT + childAppInfo.coinPerHour + COIN_PER_HOUR_TEXT;
        CoinCapacityText.text = COIN_CAPACITY_TEXT + childAppInfo.maxCoinCapacity;
        RarityText.text = FormatCamelCase(childAppInfo.rarity.ToString());
        //BuddyTypeNameText.text = childAppInfo.buddySpritePath.ToString();
        BuddyImage.sprite = AssetLoader.LoadBuddySprite(childAppInfo.buddySpritePath);
        if (childAppInfo.buddySpritePath.IsNullOrEmpty())
        {
            Debug.LogWarning("Buddy sprite was missing for: " + childAppInfo.profileName + " child");
        }
    }

    private string FormatCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Insert a space before each uppercase letter (except the first)
        string result = System.Text.RegularExpressions.Regex.Replace(input, "(?<!^)([A-Z])", " $1");

        // Capitalize the first letter
        return char.ToUpper(result[0]) + result.Substring(1);
    }

}
