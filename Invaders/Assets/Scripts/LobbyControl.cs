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

        GenerateUserStatsForLobby();
        ServerStatusText.gameObject.SetActive(false);
        ReadyButton.gameObject.SetActive(true);
        ErrorPanel.SetActive(false);
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
}
