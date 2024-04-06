using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// This class is specifically for Main Menu interactions with Unity's UI.
/// </summary>

public enum MenuStates {SignIn,MainMenu,Lobby,Game,Connecting}

public class MenuManager : MonoBehaviour
{
    [Header("Button References")]
    public Button ShieldButton;
    public Button PlaybackLastMatchButton;
    public Button InvasionPlaybackButton;
    
    [Header("Menu States")]
    public List<MenuState> MenuStatesList = new List<MenuState>();
    public MenuStates CurrentMenuState;
    public LoadingMenuState LoadingMenuState;
    public PopUpMessage errorPopUpMessageState;
    public PopUpMessage confirmPopUpMessageState;
    public bool IsLoading;

    [Header("UI Fields")] 
    public TMP_Text LoggedInNameText;
    public TMP_Text RatingText;
    public TMP_Text MatchesPlayedText;
    public TMP_Text ShieldTimerText;
    public TMP_Text LastInvasionStatusText;
    public TMP_Text SlayCountText;
    public TMP_Text DefeatedTroopsText;
    public TMP_Text BrainCloudVersionText;
    public TMP_Text GoldAmountText;
    public TMP_Text OpponentSelectedText;
    public TMP_Text LobbyHintText;
    public TMP_Text LobbyAttackButtonText;
    public TMP_InputField UsernameInputField;
    public TMP_InputField PasswordInputField;

    [Header("UI References")]
    public RectTransform InvaderButtonBorder;
    public RectTransform DefenderButtonBorder;
    public PlayerCardLobby PlayerCardRef;
    public GameObject LobbyListParent;
    public Image DefenderPreview;
    public List<Sprite> DefenderPreviews;
    public Image LobbyPlayerDefensePreview;
    public TMP_Text LobbyUsernameText;
    public TMP_Text LobbyGoldText;
    public GameObject LobbyAttackCantAffordGroup;
    public GameObject LobbyAttackSelectTargetGroup;
    public Button LobbyAttackButton;

    private UserInfo _opponent;
    private readonly List<PlayerCardLobby> _listOfPlayers = new List<PlayerCardLobby>();
    private EventSystem _eventSystem;
    private readonly List<float> _selectionDefenderXPlacement = new List<float> {-169, -3.7f, 160};
    private readonly List<float> _selectionInvaderYPlacement = new List<float> {192f, 4f, -186f};
    private readonly List<int> _priceOfInvaders = new List<int> {100000, 200000, 400000};

    public List<int> PriceOfInvaders
    {
        get => _priceOfInvaders;
    }
    private const string LOGGING_IN_MESSAGE = "Logging in...";
    private const string LOOKING_FOR_PLAYERS_MESSAGE = "Looking for players...";
    
    private static MenuManager _instance;
    public static MenuManager Instance => _instance;

    private void Awake()
    {
        if (!_instance)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        _eventSystem = EventSystem.current;
        if (LobbyAttackCantAffordGroup)
        {
            LobbyAttackCantAffordGroup.SetActive(false);
        }
    }

    private void Start()
    {
        BrainCloudVersionText.text = $"brainCloud Version: {BrainCloud.Version.GetVersion()}";
        PlaybackLastMatchButton.interactable = NetworkManager.Instance.IsPlaybackIDValid();
        if (NetworkManager.Instance.IsSessionValid())
        {
            UpdateMatchMakingInfo();
            UpdateMainMenu();
            ChangeState(MenuStates.MainMenu);
        }
        else
        {
            ChangeState(MenuStates.SignIn);    
        }
    }

    private void Update()
    {
        //This behavior is just to jump from one input field to another while in menu.
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Selectable next = _eventSystem.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
         
            if (next != null)
            {
                InputField inputfield = next.GetComponent<InputField>();
                if (inputfield != null)
                {
                    //if it's an input field, also set the text caret
                    inputfield.OnPointerClick(new PointerEventData(_eventSystem));
                }
                _eventSystem.SetSelectedGameObject(next.gameObject, new BaseEventData(_eventSystem));
            }
        }
    }

    //Called from a script that is attached to a Unity Button
    public void ButtonPressChangeState(MenuStates newMenuState = MenuStates.Connecting)
    {
        foreach (MenuState currentState in MenuStatesList)
        {
            currentState.gameObject.SetActive(false);
        }

        if (newMenuState != MenuStates.Connecting)
        {
            CurrentMenuState = newMenuState;
        }
        IsLoading = true;
        
        //User is in this state and moving onto the next
        switch (CurrentMenuState)
        {
            //Logging In...
            case MenuStates.SignIn:
                CurrentMenuState = MenuStates.MainMenu;
                NetworkManager.Instance.Login();
                LoadingMenuState.ConnectStatesWithLoading(LOGGING_IN_MESSAGE, false, MenuStates.MainMenu);
                break;
            //Looking for players...
            case MenuStates.MainMenu:
                CurrentMenuState = MenuStates.Lobby;
                SetupLobbyScreenSelections();
                NetworkManager.Instance.LookForPlayers();
                LoadingMenuState.ConnectStatesWithLoading(LOOKING_FOR_PLAYERS_MESSAGE, true, MenuStates.Lobby);
                break;
        }
    }

    private void SetupLobbyScreenSelections()
    {
        LobbyHintText.enabled = true;
        var color = LobbyPlayerDefensePreview.color;
        color.a = 0;
        LobbyPlayerDefensePreview.color = color;
        LobbyUsernameText.text = "";
        GameManager.Instance.OpponentUserInfo = new UserInfo();
        LobbyAttackButton.enabled = false;
        LobbyAttackSelectTargetGroup.SetActive(true);
        LobbyAttackButtonText.gameObject.SetActive(false);
        LobbyAttackCantAffordGroup.SetActive(false);
        GameManager.Instance.CurrentUserInfo.InvaderSelected = ArmyDivisionRank.Easy;
        UpdateButtonSelectorPosition(ArmyType.Invader);
    }

    public void ValidateShieldButton()
    {
        int gold = GameManager.Instance.CurrentUserInfo.GoldAmount;
        UserInfo user = GameManager.Instance.CurrentUserInfo;
        if (gold >= 100000 && user.ShieldTime == 0)
        {
            ShieldButton.enabled = true;
        }
        else
        {
            ShieldButton.enabled = false;
        }
    }

    public void ValidateInvaderSelection()
    {
        if (GameManager.Instance.OpponentUserInfo.Username.IsNullOrEmpty()) return;

        int invaderSelected = (int) GameManager.Instance.CurrentUserInfo.InvaderSelected;
        int gold = GameManager.Instance.CurrentUserInfo.GoldAmount;
        if (gold >= _priceOfInvaders[invaderSelected])
        {
            LobbyAttackCantAffordGroup.SetActive(false);
            LobbyAttackButtonText.gameObject.SetActive(true);
            LobbyAttackButton.enabled = true;
        }
        else
        {
            LobbyAttackCantAffordGroup.SetActive(true);
            LobbyAttackButtonText.gameObject.SetActive(false);
            LobbyAttackButton.enabled = false;
        }
    }

    public void UpdateLobbyList(List<UserInfo> in_listOfPlayers)
    {
        if (_listOfPlayers.Count > 0)
        {
            for (int i = _listOfPlayers.Count - 1; i >= 0; --i)
            {
                Destroy(_listOfPlayers[i].gameObject);
            }
            _listOfPlayers.Clear();
        }

        for (int i = 0; i < in_listOfPlayers.Count; ++i)
        {
            PlayerCardLobby user = Instantiate(PlayerCardRef, LobbyListParent.transform);
            //Apply relevant user data to text
            user.PlayerNameText.text = in_listOfPlayers[i].Username;
            user.PlayerRatingText.text = in_listOfPlayers[i].Rating.ToString();

            //Save Data for later
            user.UserInfo = in_listOfPlayers[i];
            
            _listOfPlayers.Add(user);
        }

        IsLoading = false;
    }

    public void UpdateMainMenu()
    {
        string username = GameManager.Instance.CurrentUserInfo.Username;
        PlayerPrefs.SetString(Settings.UsernameKey, username);
        LobbyUsernameText.text = LoggedInNameText.text = $"User: {username}";

        int defenderIndex = (int)GameManager.Instance.CurrentUserInfo.DefendersSelected;
        Vector2 posI = DefenderButtonBorder.anchoredPosition;
        posI.x = _selectionDefenderXPlacement[defenderIndex];
        DefenderPreview.sprite = DefenderPreviews[defenderIndex];
        DefenderButtonBorder.anchoredPosition = posI;
        
        int invaderIndex = (int) GameManager.Instance.CurrentUserInfo.InvaderSelected;
        Vector2 posD = InvaderButtonBorder.anchoredPosition;
        posD.y = _selectionInvaderYPlacement[invaderIndex];
        InvaderButtonBorder.anchoredPosition = posD;
    }

    public void UpdateMatchMakingInfo()
    {
        UserInfo user = GameManager.Instance.CurrentUserInfo;
        RatingText.text = $"Rating: {user.Rating}";
        MatchesPlayedText.text = $"Matches Played: {user.MatchesPlayed}";
        ShieldButton.interactable = user.ShieldTime <= 0;
        ShieldTimerText.text = user.ShieldTime > 1 ? $"Shield is active for {user.ShieldTime} minutes" : "Shield Timer: Off";

        PlaybackLastMatchButton.interactable = NetworkManager.Instance.IsPlaybackIDValid();

        StreamInfo invaderInfo = GameManager.Instance.InvadedStreamInfo;
        if (!invaderInfo.PlaybackStreamID.IsNullOrEmpty())
        {
            InvasionPlaybackButton.interactable = true;
            LastInvasionStatusText.text = invaderInfo.DidInvadersWin ? "Last Invasion: Defeated" : "Last Invasion: Victorious";
            SlayCountText.text = $"You lost {invaderInfo.SlayCount} troops";
            DefeatedTroopsText.text = $"You killed {invaderInfo.DefeatedTroops} troops";
        }
        else
        {
            InvasionPlaybackButton.interactable = false;
            LastInvasionStatusText.text = "No recent invasions";
            SlayCountText.text = "";
            DefeatedTroopsText.text = "";
        }
    }

    public void UpdateButtonSelectorPosition(ArmyType in_type)
    {
        switch (in_type)
        {
            case ArmyType.Invader:
            {
                int invaderIndex = (int) GameManager.Instance.CurrentUserInfo.InvaderSelected;
                Vector2 posD = InvaderButtonBorder.anchoredPosition;
                posD.y = _selectionInvaderYPlacement[invaderIndex];
                InvaderButtonBorder.anchoredPosition = posD;
                break;
            }
            case ArmyType.Defense:
            {
                int defenderIndex = (int) GameManager.Instance.CurrentUserInfo.DefendersSelected;
                Vector2 posI = DefenderButtonBorder.anchoredPosition;
                posI.x = _selectionDefenderXPlacement[defenderIndex];
                DefenderPreview.sprite = DefenderPreviews[defenderIndex];
                DefenderButtonBorder.anchoredPosition = posI;
                break;
            }
        }
    }

    public void UpdateGoldAmount()
    {
        LobbyGoldText.text = GoldAmountText.text = $"Gold: {GameManager.Instance.CurrentUserInfo.GoldAmount}";
    }

    public void UpdateSelectedPlayerDefense(int defenseIndex)
    {
        if (LobbyPlayerDefensePreview.color.a == 0)
        {
            var color = LobbyPlayerDefensePreview.color;
            color.a = 255;
            LobbyPlayerDefensePreview.color = color;
            LobbyHintText.enabled = false;
        }

        LobbyPlayerDefensePreview.sprite = DefenderPreviews[defenseIndex];
        OpponentSelectedText.text = GameManager.Instance.OpponentUserInfo.Username;
        LobbyAttackSelectTargetGroup.SetActive(false);
        ValidateInvaderSelection();
    }
    
    public void AbortToSignIn(string errorMessage)
    {
        errorPopUpMessageState.SetUpPopUpMessage(errorMessage);
        LoadingMenuState.CancelNextState = true;
        ChangeState(MenuStates.SignIn);
    }

    public void ChangeState(MenuStates newMenuState)
    {
        foreach (MenuState currentState in MenuStatesList)
        {
            currentState.gameObject.SetActive(currentState.AssignedGameState == newMenuState);
        }

        CurrentMenuState = newMenuState;
    }

    public void SignOutPressed()
    {
        NetworkManager.Instance.SignOut();
        ChangeState(MenuStates.SignIn);
    }
}
