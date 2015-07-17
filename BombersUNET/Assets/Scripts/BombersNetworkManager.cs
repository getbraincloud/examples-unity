using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using BrainCloudUNETExample.Game.PlayerInput;

public class BombersNetworkManager : NetworkManager
{

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        m_localConnection = conn;
        GameObject player = (GameObject)Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        //player.GetComponent<BombersPlayerController>();
        m_localPlayer = player.GetComponent<BombersPlayerController>();
        NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
    }

    public static Dictionary<string, string> m_matchOptions;
    public static NetworkConnection m_localConnection;
    public static BombersPlayerController m_localPlayer;
    
    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        if (Application.loadedLevelName == "Game")
        {
            GameObject.Find("GameInfo").GetComponent<GameInfo>().Initialize(m_matchOptions);
            m_matchOptions.Clear();
            UnityEngine.Networking.ClientScene.AddPlayer(conn, 0);
        }
    }
}
