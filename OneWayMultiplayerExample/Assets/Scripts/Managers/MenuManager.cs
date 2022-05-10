using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MenuStates {SignIn,MainMenu,Lobby,Game,Connecting}

public class MenuManager : MonoBehaviour
{
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
    public TMP_InputField UsernameInputField;
    public TMP_InputField PasswordInputField;

    [Header("UI References")] 
    //ToDo: Dont make this public and save the data
    public List<float> SelectionList = new List<float>();
    public RectTransform InvaderButtonBorder;
    public RectTransform DefenderButtonBorder;
    public PlayerCardLobby PlayerCardRef;
    public GameObject LobbyListParent;

    private UserInfo _opponent;
    private List<PlayerCardLobby> _listOfPlayers = new List<PlayerCardLobby>();
    private EventSystem _eventSystem;

    private const string LOGGING_IN_MESSAGE = "Logging in...";
    private const string LOOKING_FOR_PLAYERS_MESSAGE = "Looking for players...";
    private const string JOINING_MATCH_MESSAGE = "Joining Match...";
    
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
        ChangeState(MenuStates.SignIn);
    }

    private void Update()
    {
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
                BrainCloudManager.Instance.Login();
                LoadingMenuState.ConnectStatesWithLoading(LOGGING_IN_MESSAGE, false, MenuStates.MainMenu);
                break;
            //Looking for players...
            case MenuStates.MainMenu:
                CurrentMenuState = MenuStates.Lobby;
                BrainCloudManager.Instance.LookForPlayers();
                LoadingMenuState.ConnectStatesWithLoading(LOOKING_FOR_PLAYERS_MESSAGE, true, MenuStates.Lobby);
                
                break;
            //Loading up game to start invading...
            case MenuStates.Lobby:
                CurrentMenuState = MenuStates.Game;
                //ToDo: Braincloud get player info to load game with
                //ToDo: Loading transition for scene to scene. 
                break;
        }
    }

    public void UpdateLobbyList(List<UserInfo> in_listOfPlayers)
    {
        if (_listOfPlayers.Count > 0)
        {
            for (int i = _listOfPlayers.Count-1; i < 0; i--)
            {
                Destroy(_listOfPlayers[i].gameObject);
            }    
        }
        
        for (int i = 0; i < in_listOfPlayers.Count; i++)
        {
            PlayerCardLobby user = Instantiate(PlayerCardRef, LobbyListParent.transform);
            //Apply relevant user data to text
            user.PlayerNameText.text = in_listOfPlayers[i].Username;
            user.PlayerRatingText.text = in_listOfPlayers[i].Rating.ToString();
            user.PlayerDifficulty.text = in_listOfPlayers[i].DefendersSelected.ToString();

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
        posI.x = SelectionList[defenderIndex];
        DefenderButtonBorder.anchoredPosition = posI;
        
        int invaderIndex = (int) GameManager.Instance.CurrentUserInfo.InvaderSelected;
        Vector2 posD = InvaderButtonBorder.anchoredPosition; 
        posD.x = SelectionList[invaderIndex];
        InvaderButtonBorder.anchoredPosition = posD;
    }

    public void UpdateMatchMakingInfo()
    {
        UserInfo user = GameManager.Instance.CurrentUserInfo;
        RatingText.text = $"Rating: {user.Rating}";
        MatchesPlayedText.text = $"Matches Played: {user.MatchesPlayed}";
        ShieldTimerText.text = $"Shield Timer: {user.ShieldTime}";
    }

    public void UpdateButtonSelectorPosition(ArmyType in_type)
    {
        if (in_type == ArmyType.Invader)
        {
            int invaderIndex = (int) GameManager.Instance.CurrentUserInfo.InvaderSelected;
            Vector2 posD = InvaderButtonBorder.anchoredPosition; 
            posD.x = SelectionList[invaderIndex];
            InvaderButtonBorder.anchoredPosition = posD;    
        }
        else
        {
            int defenderIndex = (int)GameManager.Instance.CurrentUserInfo.DefendersSelected;
            Vector2 posI = DefenderButtonBorder.anchoredPosition; 
            posI.x = SelectionList[defenderIndex];
            DefenderButtonBorder.anchoredPosition = posI;    
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
        BrainCloudManager.Instance.SignOut();
        ChangeState(MenuStates.SignIn);
    }
}
