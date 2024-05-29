using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyControl : NetworkBehaviour
{
    [SerializeField]
    private string m_InGameSceneName = "InGame";
    
    // Minimum player count required to transition to next level
    [SerializeField]
    private int m_MinimumPlayerCount = 1;

    public TMP_Text ServerStatusText;
    public Button ReadyButton;

    public TMP_Text ErrorMessage;
    public GameObject ErrorPanel;
    
    public TMP_Text LobbyText;
    private bool m_AllPlayersInLobby;

    [SerializeField]
    private TMP_Text playbackCounter;
    private int playbackCount = 0;
    [SerializeField]
    private List<PlaybackSelector> leaderBoardSelectors;
    [SerializeField]
    private PlaybackSelector featuredSelector;

    private Dictionary<ulong, bool> m_ClientsInLobby;
    private string m_UserLobbyStatusText;
    private bool _isLoading;
    public bool IsLoading
    {
        set => _isLoading = value;
    }
    private int _numberOfDots;
    private string _loadingIndicatorMessage;
    public string LoadingIndicatorMessage
    {
        set => _loadingIndicatorMessage = value;
    }
    public static LobbyControl Singleton { get; private set; }

    private List<string> addedUserIds = new List<string>();

    private void Awake()
    {
        if(Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Destroy(gameObject);
        }

        UpdatePlaybackCount();
        GenerateUserStatsForLobby();
        ServerStatusText.gameObject.SetActive(false);
        ReadyButton.gameObject.SetActive(true);
        ErrorPanel.SetActive(false);
        BrainCloudManager.Singleton.StartGetFeaturedUser();
        BrainCloudManager.Singleton.GetTopUsers(leaderBoardSelectors.Count);
    }

    private void OnGUI()
    {
        if (LobbyText != null) LobbyText.text = m_UserLobbyStatusText;
    }

    /// <summary>
    ///     GenerateUserStatsForLobby
    ///     Psuedo code for setting player state
    ///     Just updating a text field, this could use a lot of "refactoring"  :)
    /// </summary>
    public void GenerateUserStatsForLobby()
    {
        m_UserLobbyStatusText = string.Empty;
        List<UserInfo> listOfMembers = new List<UserInfo>();
        if(BrainCloudManager.Singleton.CurrentLobby != null)
        {
            listOfMembers = BrainCloudManager.Singleton.CurrentLobby.Members;
        }
        else
        {
            return;
        }

        foreach (var clientInfo in listOfMembers)
        {
            m_UserLobbyStatusText += clientInfo.Username + "          ";
            if(clientInfo.IsReady)
            {
                m_UserLobbyStatusText += "(READY)\n";
            }
            else
            {
                m_UserLobbyStatusText += "(NOT READY)\n";
            }
        }
    }

    /// <summary>
    ///     PlayerIsReady
    ///     Tied to the Ready button in the InvadersLobby scene
    /// </summary>
    public void PlayerIsReady()
    {
        ReadyButton.gameObject.SetActive(false);
        _loadingIndicatorMessage = ServerStatusText.text = "Waiting for server";
        ServerStatusText.gameObject.SetActive(true);
        _isLoading = true;
        BrainCloudManager.Singleton.UpdateReady();
        GenerateUserStatsForLobby();
        StartCoroutine(WaitingForServerRoom());
    }
    
    IEnumerator WaitingForServerRoom()
    {
        while(_isLoading)
        {
            if(_numberOfDots < 3)
            {
                ServerStatusText.text += ".";
                _numberOfDots++;
            }
            else
            {
                _numberOfDots = 0;
                ServerStatusText.text = _loadingIndicatorMessage;
            }

            yield return new WaitForSeconds(0.5f);
        }
        
    }
    
    public void ReturnToMainMenu()
    {
        SceneTransitionHandler.sceneTransitionHandler.SwitchScene("StartMenu");
    }
    
    public void SetupPopupPanel(string errorMessage)
    {
        ReadyButton.enabled = false;
        ErrorMessage.text = errorMessage;
        ErrorPanel.SetActive(true);
    }

    public void AddNewPlayerIdSignal(string newId)
    {
        List<string> userIds = new List<string>(addedUserIds) { newId };
        BrainCloudManager.Singleton.SendNewIdSignal(userIds.ToArray());
    }

    public void UpdatePlaybackCount()
    {
        if (playbackCount == 0) playbackCounter.text = "ADD BACK UP";
        else playbackCounter.text = "BACK UP  +" + playbackCount.ToString();
    }

    public void UpdateFeaturedSelector(string newId, string newName, int newScore)
    {
        featuredSelector.InitValues(newId, newName, newScore);
        featuredSelector.UpdateLabels();
    }

    public void UpdateLeaderBoardSelector(int rank, string newId, string newName, int newScore)
    {
        leaderBoardSelectors[rank - 1].InitValues(newId, newName, newScore);
        leaderBoardSelectors[rank - 1].UpdateLabels();
    }

    public void AddIdToList(string newId)
    {
        if(addedUserIds.Contains(newId)) return;

        HideUsedPlaybacks(newId);
        addedUserIds.Add(newId);
        playbackCount = addedUserIds.Count;
        UpdatePlaybackCount();
    }

    private void HideUsedPlaybacks(string newId)
    {
        foreach(PlaybackSelector ii in leaderBoardSelectors)
        {
            if (ii.playerId == newId) ii.HideButton();
        }
        if(featuredSelector.playerId == newId) featuredSelector.HideButton();
    }

    public void FetchPlaybacks()
    {
        //This prevents every member of a lobby creating from creating copies of the replays
        //Exactly one user should create the replays
        if(BrainCloudManager.Singleton.isLobbyOwner)
            PlaybackFetcher.Singleton.AddRecordsFromUsers(addedUserIds);
    }
}
