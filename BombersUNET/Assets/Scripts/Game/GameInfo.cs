using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

public class GameInfo : NetworkBehaviour {

    public static GameInfo s_instance;

    [SerializeField]
    [SyncVar]
    private int m_team1Score = 0;

    [SerializeField]
    [SyncVar]
    private int m_team2Score = 0;

    [SerializeField]
    [SyncVar]
    private float m_gameTime = 0;

    [SerializeField]
    [SyncVar]
    private int m_mapLayout = 0;

    [SerializeField]
    [SyncVar]
    private int m_mapSize = 0;

    [SerializeField]
    [SyncVar]
    private int m_lightPosition = 0;

    [SerializeField]
    [SyncVar]
    private int m_isPlaying = 0;

    [SerializeField]
    [SyncVar]
    private int m_team1Players = 0;

    [SerializeField]
    [SyncVar]
    private int m_team2Players = 0;

    [SerializeField]
    [SyncVar]
    private string m_gameName = "";

    [SerializeField]
    [SyncVar]
    private int m_maxPlayers = 0;

    //private Dictionary<string, string> m_matchOptions;

    [SyncVar]
    private float m_originalGameTime = 0;

    void Awake()
    {
        s_instance = this;
    }

    [Server]
    public void Initialize(Dictionary<string, string> aOptions)
    {
        m_team1Score = 0;
        m_team2Score = 0;
        m_gameTime = int.Parse(aOptions["gameTime"]);
        m_originalGameTime = m_gameTime;
        m_mapLayout = int.Parse(aOptions["mapLayout"]);
        m_mapSize = int.Parse(aOptions["mapSize"]);
        m_lightPosition = int.Parse(aOptions["lightPosition"]);
        m_maxPlayers = int.Parse(aOptions["maxPlayers"]);
        m_gameName = aOptions["gameName"];
        m_isPlaying = int.Parse(aOptions["isPlaying"]);
        m_team1Players = 0;
        m_team2Players = 0;
    }

    [Server]
    public void Reinitialize()
    {
        m_team1Score = 0;
        m_team2Score = 0;
        m_gameTime = m_originalGameTime;
        m_isPlaying = 0;
    }


    public int GetMaxPlayers()
    {
        return s_instance.m_maxPlayers;
    }

    public string GetGameName()
    {
        return s_instance.m_gameName;
    }

    public int GetTeamPlayers(int aTeam)
    {
        if (aTeam == 1)
        {
            return s_instance.m_team1Players;
        }
        else
        {
            return s_instance.m_team2Players;
        }
    }

    public void SetTeamPlayers(int aTeam, int aPlayers)
    {
        if (aTeam == 1)
        {
            m_team1Players = aPlayers;
        }
        else
        {
            m_team2Players = aPlayers;
        }
    }

    public int GetPlaying()
    {
        return s_instance.m_isPlaying;
    }

    public void SetPlaying(int aPlaying)
    {
        m_isPlaying = aPlaying;
    }

    public int GetLightPosition()
    {
        return s_instance.m_lightPosition;
    }

    public void SetLightPosition(int aPosition)
    {
        m_lightPosition = aPosition;
    }

    public int GetMapSize()
    {
        return s_instance.m_mapSize;
    }

    public void SetMapSize(int aSize)
    {
        m_mapSize = aSize;
    }

    public int GetMapLayout()
    {
        return s_instance.m_mapLayout;
    }

    public void SetMapLayout(int aLayout)
    {
        m_mapLayout = aLayout;
    }

    public float GetGameTime()
    {
        return s_instance.m_gameTime;
    }

    public void SetGameTime(float aTime)
    {
        m_gameTime = aTime;
    }

    public int GetTeamScore(int aTeam)
    {
        if (aTeam == 1)
        {
            return s_instance.m_team1Score;
        }
        else
        {
            return s_instance.m_team2Score;
        }
    }

    public void SetTeamScore(int aTeam, int aScore)
    {
        if (aTeam == 1)
        {
            m_team1Score = aScore;
        }
        else
        {
            m_team2Score = aScore;
        }
    }
	


}
