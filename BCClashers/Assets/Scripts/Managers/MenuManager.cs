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
    public TMP_InputField UsernameInputField;
    public TMP_InputField PasswordInputField;

    [Header("UI References")]
    public RectTransform InvaderButtonBorder;
    public RectTransform DefenderButtonBorder;
    public PlayerCardLobby PlayerCardRef;
    public GameObject LobbyListParent;

    private UserInfo _opponent;
    private readonly List<PlayerCardLobby> _listOfPlayers = new List<PlayerCardLobby>();
    private EventSystem _eventSystem;
    private readonly List<float> _selectionXPlacement = new List<float> {-169,-3.7f, 160};
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
                NetworkManager.Instance.LookForPlayers();
                LoadingMenuState.ConnectStatesWithLoading(LOOKING_FOR_PLAYERS_MESSAGE, true, MenuStates.Lobby);
                break;
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
        LoggedInNameText.text = $"User: {username}";
        
        int defenderIndex = (int)GameManager.Instance.CurrentUserInfo.DefendersSelected;
        Vector2 posI = DefenderButtonBorder.anchoredPosition; 
        posI.x = _selectionXPlacement[defenderIndex];
        DefenderButtonBorder.anchoredPosition = posI;
        
        int invaderIndex = (int) GameManager.Instance.CurrentUserInfo.InvaderSelected;
        Vector2 posD = InvaderButtonBorder.anchoredPosition; 
        posD.x = _selectionXPlacement[invaderIndex];
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
                posD.x = _selectionXPlacement[invaderIndex];
                InvaderButtonBorder.anchoredPosition = posD;
                break;
            }
            case ArmyType.Defense:
            {
                int defenderIndex = (int) GameManager.Instance.CurrentUserInfo.DefendersSelected;
                Vector2 posI = DefenderButtonBorder.anchoredPosition;
                posI.x = _selectionXPlacement[defenderIndex];
                DefenderButtonBorder.anchoredPosition = posI;
                break;
            }
        }
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
