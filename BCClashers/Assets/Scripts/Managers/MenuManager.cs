using System.Collections;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// This class is specifically for Main Menu interactions with Unity's UI.
/// </summary>
public enum MenuStates
{
    SignIn,
    MainMenu,
    Lobby,
    Game,
    Connecting,
    Reconnect
}

public class MenuManager : MonoBehaviour
{
    [Header("Button References")]
    public Button ShieldButton;
    public Button PlaybackLastMatchButton;
    public Button InvasionPlaybackButton;

    [Header("Shared UI")]
    [Tooltip("Single shared TopBar instance (prefab) reused by the MainMenu and Lobby screens.")]
    public GameObject SharedTopBar;

    [Header("Redesign - Selector Buttons")]
    [Tooltip("The Line/Cross/Diamond buttons, in Easy/Medium/Hard order. When assigned, the highlight " +
             "border snaps to the selected button, so the row can be laid out vertically OR horizontally " +
             "without touching code. Leave empty to keep the legacy hardcoded offsets.")]
    public List<RectTransform> DefenderSelectorButtons = new List<RectTransform>();
    [Tooltip("The attack-force buttons, in Easy/Medium/Hard order. Same behaviour as DefenderSelectorButtons.")]
    public List<RectTransform> InvaderSelectorButtons = new List<RectTransform>();

    [Header("Redesign - Dashboard Panels")]
    [Tooltip("'My Recent Attacks' list (IsAttackPanel = true).")]
    public MatchHistoryPanel RecentAttacksPanel;
    [Tooltip("'Recent Invasions' list (IsAttackPanel = false).")]
    public MatchHistoryPanel RecentInvasionsPanel;
    [Tooltip("'My Stats' panel. Derived from the same match history the lists use - no extra calls.")]
    public MyStatsPanel MyStatsPanel;
    [Tooltip("'My Rating: N ELO' label above the Find New Opponent / Attack button. Optional.")]
    public TMP_Text FindOpponentRatingText;

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
    public TMP_Text InvasionDurationText;
    public TMP_Text BrainCloudVersionText;
    public TMP_Text GoldAmountText;
    public TMP_Text OpponentSelectedText;
    public TMP_Text LobbyHintText;
    public TMP_Text LobbyAttackButtonText;
    public TMP_InputField UsernameInputField;
    public TMP_InputField PasswordInputField;
    public Toggle RememberMeToggle;
    
    [Header("UI References")]
    public RectTransform InvaderButtonBorder;
    public RectTransform DefenderButtonBorder;
    public PlayerCardLobby PlayerCardRef;
    public GameObject LobbyListParent;
    public Image DefenderPreview;
    [Tooltip("YOUR defense, rendered in blue (your team) - used by the MainMenu 'My Defense' preview.")]
    public List<Sprite> DefenderPreviews;
    [Tooltip("The OPPONENT's defense, rendered in red (enemy team) - used by the Lobby 'Target's Defense' preview.")]
    public List<Sprite> OpponentDefenderPreviews;
    public Image LobbyPlayerDefensePreview;
    public TMP_Text LobbyUsernameText;
    public TMP_Text LobbyGoldText;
    public GameObject LobbyAttackCantAffordGroup;
    public GameObject LobbyAttackSelectTargetGroup;
    public Button LobbyAttackButton;

    private float _tweenTime = 0.001f;
    private UserInfo _opponent;
    private readonly List<PlayerCardLobby> _listOfPlayers = new List<PlayerCardLobby>();
    private EventSystem _eventSystem;
    // Defense selectors are stacked vertically (Line/Cross/Diamond), so the highlight border moves in Y.
    private readonly List<float> _selectionDefenderYPlacement = new List<float> {153f, -1f, -155f};
    // Attack-force selectors sit in a horizontal row, so the highlight border moves in X.
    private readonly List<float> _selectionInvaderXPlacement = new List<float> {-520f, 0f, 520f};
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
        if (PlaybackLastMatchButton)
        {
            PlaybackLastMatchButton.interactable = NetworkManager.Instance.IsPlaybackIDValid();
        }
        if (NetworkManager.Instance.IsSessionValid())
        {
            UpdateMainMenu();
            ChangeState(MenuStates.MainMenu);
            //We got here without logging in - i.e. we just came back from a raid - so the
            //history panels still hold the pre-match reads. Pull them again.
            NetworkManager.Instance.RefreshMatchHistory();
        }
        else if (NetworkManager.Instance.Wrapper.CanReconnect())
        {
            ButtonPressChangeState(MenuStates.Reconnect);
        }
        else
        {
            RememberMeToggle.isOn = true;
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
            //Reconnecting user...
            case MenuStates.Reconnect:
                CurrentMenuState = MenuStates.MainMenu;
                NetworkManager.Instance.Reconnect();
                LoadingMenuState.ConnectStatesWithLoading(LOGGING_IN_MESSAGE, false, MenuStates.MainMenu);
                break;
            //Logging In using input fields..
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
        if (GameManager.Instance.OpponentUserInfo == null || GameManager.Instance.OpponentUserInfo.Username.IsNullOrEmpty()) return;

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

        in_listOfPlayers.Sort(delegate (UserInfo userA, UserInfo userB) { return userA.Rating.CompareTo(userB.Rating); });
        in_listOfPlayers.Reverse();

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
        LobbyUsernameText.text = LoggedInNameText.text = $"{username}";

        UpdateButtonSelectorPosition(ArmyType.Defense);
        UpdateButtonSelectorPosition(ArmyType.Invader);

        UpdateMatchMakingInfo();
        UpdateGoldAmount();
    }

    public void UpdateMatchMakingInfo()
    {
        UserInfo user = GameManager.Instance.CurrentUserInfo;
        if (RatingText) RatingText.text = $"{user.Rating.ToString("#,#")}";
        if (FindOpponentRatingText) FindOpponentRatingText.text = $"My Rating: {user.Rating.ToString("#,0")} ELO";
        if (MatchesPlayedText) MatchesPlayedText.text = $"{user.MatchesPlayed.ToString("#,#")}";

        if (ShieldButton) ShieldButton.interactable = user.ShieldTime <= 0;
        if (user.ShieldTime > 0)
        {
            StartCoroutine(ShieldTimerCountdown(user.ShieldTime * 60));
        }
        else if (ShieldTimerText)
        {
            ShieldTimerText.text = "Off";
        }

        //Everything below is the pre-redesign "last invasion" block. The dashboard's Recent
        //Invasions panel supersedes it (every row has its own Watch button), so these widgets
        //are optional - the null checks let the redesigned scene delete them outright.
        if (PlaybackLastMatchButton)
        {
            PlaybackLastMatchButton.interactable = NetworkManager.Instance.IsPlaybackIDValid();
        }

        StreamInfo invaderInfo = GameManager.Instance.InvadedStreamInfo;
        bool hasInvasion = !invaderInfo.PlaybackStreamID.IsNullOrEmpty();

        if (InvasionPlaybackButton) InvasionPlaybackButton.interactable = hasInvasion;

        if (hasInvasion)
        {
            if (LastInvasionStatusText)
            {
                LastInvasionStatusText.text = invaderInfo.DidInvadersWin ? "Last Invasion: Defeated" : "Last Invasion: Victorious";
            }
            if (SlayCountText) SlayCountText.text = $"You lost {invaderInfo.SlayCount} troops";
            if (DefeatedTroopsText) DefeatedTroopsText.text = $"You killed {invaderInfo.DefeatedTroops} troops";
            if (InvasionDurationText)
            {
                float minuteDuration = Mathf.FloorToInt(invaderInfo.DurationOfInvasion / 60);
                float secondsDuration = Mathf.FloorToInt(invaderInfo.DurationOfInvasion % 60);
                InvasionDurationText.text = $"Duration: {minuteDuration:00}:{secondsDuration:00} / 3:00";
            }
        }
        else
        {
            if (LastInvasionStatusText) LastInvasionStatusText.text = "No recent invasions";
            if (SlayCountText) SlayCountText.text = "";
            if (DefeatedTroopsText) DefeatedTroopsText.text = "";
        }
    }

    /// <summary>
    /// Redesign: refresh both dashboard history lists from the streams read at login.
    /// Safe to call before the panels are wired up - each is optional.
    /// </summary>
    public void UpdateMatchHistoryPanels()
    {
        if (RecentAttacksPanel)
        {
            RecentAttacksPanel.Populate(GameManager.Instance.RecentAttacks);
        }
        if (RecentInvasionsPanel)
        {
            RecentInvasionsPanel.Populate(GameManager.Instance.RecentInvasions);
        }
        if (MyStatsPanel)
        {
            //Same two lists the panels above render, just aggregated - so the stats can never
            //disagree with the cards sitting next to them.
            MyStatsPanel.Bind(MatchHistoryStats.Compute(
                GameManager.Instance.RecentAttacks,
                GameManager.Instance.RecentInvasions,
                GameManager.Instance.UserStatistics));
        }
    }

    /// <summary>
    /// Redesign: "Attack" on a match-history card. We already know the target, so this drops
    /// straight into the Lobby with them pre-selected instead of running matchmaking - the
    /// player still picks their invader force and confirms the raid there.
    /// </summary>
    public void AttackOpponentDirect(MatchSummary in_match)
    {
        if (in_match == null || in_match.OpponentProfileId.IsNullOrEmpty()) return;

        //Resets the lobby widgets AND clears OpponentUserInfo, so it has to run before we
        //install the opponent below.
        SetupLobbyScreenSelections();
        ChangeState(MenuStates.Lobby);

        GameManager.Instance.OpponentUserInfo = new UserInfo
        {
            ProfileId = in_match.OpponentProfileId,
            Username = in_match.OpponentName,
            Rating = in_match.OpponentRating
        };

        //Pulls their CURRENT defense (not the one from the historical match) so the lobby
        //previews what we would actually be raiding.
        NetworkManager.Instance.ReadLobbyUserSelected(in_match.OpponentProfileId);
    }

    public void BeginMatch()
    {
        NetworkManager.Instance.StartMatch();
    }

    public void AwardCurrency()
    {
        NetworkManager.Instance.IncreaseGoldAmount();
    }

    public void ReadInvasionStream()
    {
        NetworkManager.Instance.ReadInvasionStream();
    }

    private IEnumerator ShieldTimerCountdown(float duration)
    {
        float startTime = Time.time;
        float shieldTimer = duration;

        while (Time.time - startTime < duration)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer >= 0)
            {
                float minutes = Mathf.FloorToInt(shieldTimer / 60);
                float seconds = Mathf.FloorToInt(shieldTimer % 60);
                ShieldTimerText.text = $"{minutes:00}:{seconds:00}";
            }
            else
            {
                ShieldTimerText.text = "0:00";
            }

            yield return new WaitForFixedUpdate();
        }

        ShieldTimerText.text = "Off";
    }

    public void UpdateButtonSelectorPosition(ArmyType in_type)
    {
        int index = in_type == ArmyType.Invader
            ? (int) GameManager.Instance.CurrentUserInfo.InvaderSelected
            : (int) GameManager.Instance.CurrentUserInfo.DefendersSelected;

        MoveSelectorBorder(in_type, index);
    }

    /// <summary>
    /// Places the highlight border on the selected army button.
    ///
    /// Preferred path: snap to the button's own RectTransform, which works for ANY layout - the
    /// redesign moves the defense row from vertical to horizontal, and hardcoded offsets cannot
    /// survive that. Falls back to the legacy offset tables while the button lists are unassigned,
    /// so the existing scene keeps working until it is re-laid-out.
    /// </summary>
    private void MoveSelectorBorder(ArmyType in_type, int in_index)
    {
        //ArmyDivisionRank carries None/Test entries that have no button or preview.
        if (in_index < 0) return;

        switch (in_type)
        {
            case ArmyType.Invader:
            {
                if (TrySnapToButton(InvaderButtonBorder, InvaderSelectorButtons, in_index)) return;
                if (in_index >= _selectionInvaderXPlacement.Count) return;

                Vector2 posD = InvaderButtonBorder.anchoredPosition;
                posD.x = _selectionInvaderXPlacement[in_index];
                posD.y = 0f;
                InvaderButtonBorder.anchoredPosition = posD;
                break;
            }
            case ArmyType.Defense:
            {
                if (DefenderPreview && in_index < DefenderPreviews.Count)
                {
                    DefenderPreview.sprite = DefenderPreviews[in_index];
                }

                if (TrySnapToButton(DefenderButtonBorder, DefenderSelectorButtons, in_index)) return;
                if (in_index >= _selectionDefenderYPlacement.Count) return;

                Vector2 posI = DefenderButtonBorder.anchoredPosition;
                posI.x = 0f;
                posI.y = _selectionDefenderYPlacement[in_index];
                DefenderButtonBorder.anchoredPosition = posI;
                break;
            }
        }
    }

    //World position is used so the border does not have to share a parent with the buttons.
    private static bool TrySnapToButton(RectTransform in_border, List<RectTransform> in_buttons, int in_index)
    {
        if (!in_border || in_buttons == null || in_index >= in_buttons.Count || !in_buttons[in_index])
        {
            return false;
        }

        //The selector buttons are placed by a LayoutGroup, and layout does not run until the end
        //of the frame. Reading a button's world position during Start (i.e. every time the menu
        //is shown, including on return from a raid) would otherwise read its PRE-layout position
        //and park the border away from the selected button.
        Canvas.ForceUpdateCanvases();

        in_border.position = in_buttons[in_index].position;
        return true;
    }

    public void UpdateGoldAmount()
    {
        if (GameManager.Instance.CurrentUserInfo.PreviousGoldAmount == 0)
        {
            LobbyGoldText.text = GoldAmountText.text = $"{GameManager.Instance.CurrentUserInfo.GoldAmount.ToString("#,#")}";
        }
        else
        {
            StartCoroutine(TweenGoldText());
        }
    }

    IEnumerator TweenGoldText()
    {
        int previousGoldAmount = GameManager.Instance.CurrentUserInfo.PreviousGoldAmount;
        int goldAmount = GameManager.Instance.CurrentUserInfo.GoldAmount;
        while (previousGoldAmount < goldAmount)
        {
            previousGoldAmount += 100;
            LobbyGoldText.text = GoldAmountText.text = $"{previousGoldAmount.ToString("#,#")}";
            yield return new WaitForSeconds(_tweenTime);
        }

        GameManager.Instance.CurrentUserInfo.PreviousGoldAmount = 0;
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

        //This is the opponent's base, so show the RED (enemy) render, not your blue one.
        List<Sprite> previews = OpponentDefenderPreviews != null && OpponentDefenderPreviews.Count > defenseIndex
            ? OpponentDefenderPreviews
            : DefenderPreviews;
        LobbyPlayerDefensePreview.sprite = previews[defenseIndex];
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

        //The TopBar is a single shared instance, so it lives outside the menu states and is toggled here.
        if (SharedTopBar)
        {
            SharedTopBar.SetActive(newMenuState == MenuStates.MainMenu || newMenuState == MenuStates.Lobby);
        }

        CurrentMenuState = newMenuState;
    }

    public void SignOutPressed()
    {
        UsernameInputField.text = "";
        PasswordInputField.text = "";
        NetworkManager.Instance.SignOut();
        ChangeState(MenuStates.SignIn);
    }
}
