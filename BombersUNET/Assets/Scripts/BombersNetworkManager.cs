using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using BrainCloudUNETExample.Game.PlayerInput;

public class BombersNetworkManager : NetworkManager
{
    public GameObject m_gameManager;
    public GameObject m_gameInfo;

    /*public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        GameObject player = (GameObject)Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        //player.GetComponent<BombersPlayerController>();
        m_localPlayer = player.GetComponent<BombersPlayerController>();
        //m_localConnection = conn;
        NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
        
        //GameObject.Find("GameInfo").GetComponent<GameInfo>().Initialize(m_matchOptions);
       // m_matchOptions.Clear();
    }*/

    public static Dictionary<string, string> m_matchOptions;
    public static NetworkConnection m_localConnection;
    public static BombersPlayerController m_localPlayer;

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        m_localConnection = conn;
        if (Application.loadedLevelName == "Game" && m_matchOptions != null)
        {
            
            StartCoroutine(InitializeGameInfo(m_matchOptions));
            m_matchOptions.Clear();
        }
        base.OnClientSceneChanged(conn);
    }

    IEnumerator InitializeGameInfo(Dictionary<string, string> aMatchOptions)
    {
        Dictionary<string, string> matchOptions = aMatchOptions;

        while (GameObject.Find("GameInfo") == null)
        {
            yield return new WaitForSeconds(0);
        }

        GameObject.Find("GameInfo").GetComponent<GameInfo>().Initialize(matchOptions);
    }
}
