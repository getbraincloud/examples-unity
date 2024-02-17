using System;
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
    
    public TMP_Text LobbyText;
    private bool m_AllPlayersInLobby;

    private Dictionary<ulong, bool> m_ClientsInLobby;
    private string m_UserLobbyStatusText;

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
        BrainCloudManager.Singleton.UpdateReady();
        GenerateUserStatsForLobby();
    }
}
