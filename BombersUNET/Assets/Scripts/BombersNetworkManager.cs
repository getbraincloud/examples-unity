/*
 * UNET doesn't have a clean built-in way to detect that a host has left a match, so a general all-encompasing error and disconnection statement has been made
 * in this network manager to boot player back to the menu should the host leave the game.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using BrainCloudUNETExample.Game.PlayerInput;
using UnityEngine.SceneManagement;

public class BombersNetworkManager : NetworkManager
{
    public GameObject m_gameManager;
    public GameObject m_gameInfo;

    public static Dictionary<string, string> m_matchOptions;
    public static NetworkConnection m_localConnection;
    public static BombersPlayerController m_localPlayer;

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        m_localConnection = conn;
        if (SceneManager.GetActiveScene().name == "Game" && m_matchOptions != null)
        {
            StartCoroutine(InitializeGameInfo(m_matchOptions));
            m_matchOptions = null;
        }
        base.OnClientSceneChanged(conn);
    }

    IEnumerator InitializeGameInfo(Dictionary<string, string> aMatchOptions)
    {
        Dictionary<string, string> matchOptions = aMatchOptions;

        while (GameObject.Find("GameInfo") == null)
        {
            yield return null;
        }

        GameObject.Find("GameInfo").GetComponent<GameInfo>().Initialize(matchOptions);
    }

    public override void OnClientError(NetworkConnection conn, int errorCode)
    {
        Debug.LogWarning("HitError");
        StopMatchMaker();
        StopClient();
        StartMatchMaker();
        if (GameObject.Find("GameManager") != null)
        {
            GameObject.Find("GameManager").GetComponent<BrainCloudUNETExample.Game.GameManager>().LeaveRoom();
            GameObject.Find("DialogDisplay").GetComponent<BrainCloudUNETExample.Connection.DialogDisplay>().HostLeft();
        } 
        
            
        base.OnClientError(conn, errorCode);
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        Debug.LogWarning("HitDisconnect");
        if (m_localConnection != null && conn != null && conn == m_localConnection || conn == null || m_localConnection == null)
        {
            StopMatchMaker();
            StopClient();
            StartMatchMaker();
            if (GameObject.Find("GameManager") != null)
                GameObject.Find("GameManager").GetComponent<BrainCloudUNETExample.Game.GameManager>().LeaveRoom();
        }
        
        base.OnClientDisconnect(conn);
    }
}
