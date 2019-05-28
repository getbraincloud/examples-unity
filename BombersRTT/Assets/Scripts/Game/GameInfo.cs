using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using Gameframework;

public class GameInfo : BaseBehaviour //  NetworkBehaviour
{
    public static GameInfo s_instance;

    [SerializeField]
    //[SyncVar]
    private int m_team1Score = 0;

    [SerializeField]
    //[SyncVar]
    private int m_team2Score = 0;

    [SerializeField]
    //[SyncVar]
    private float m_gameTime = 0;

    [SerializeField]
    //[SyncVar]
    private int m_mapLayout = 0;

    [SerializeField]
    //[SyncVar]
    private int m_mapSize = 0;

    [SerializeField]
    //[SyncVar]
    private int m_lightPosition = 0;

    [SerializeField]
    //[SyncVar]
    private int m_isPlaying = 0;

    [SerializeField]
    //[SyncVar]
    private string m_gameName = "";

    [SerializeField]
    //[SyncVar]
    private int m_maxPlayers = 0;

    //[SyncVar]
    private float m_originalGameTime = 0;

    void Awake()
    {
        s_instance = this;
    }

    //[Server]
    public void Initialize()
    {
        m_team1Score = 0;
        m_team2Score = 0;
        m_gameTime = GConfigManager.GetIntValue("DefaultGameTime");
        m_originalGameTime = m_gameTime;
        m_mapLayout = 0;
        m_mapSize = 1;
        m_lightPosition = 0;
        m_maxPlayers = 8;
        m_gameName = "testGame";
        m_isPlaying = 0;
    }

    public void Initialize(Dictionary<string, object> aOptions)
    {
        m_team1Score = 0;
        m_team2Score = 0;
        m_gameTime = (int)aOptions["gameTime"];
        m_originalGameTime = m_gameTime;
        m_mapLayout = (int)aOptions["mapLayout"];
        m_mapSize = (int)aOptions["mapSize"];
        m_lightPosition = (int)aOptions["lightPosition"];
        m_maxPlayers = 8;// (int)(uint)aOptions["maxPlayers"];
        m_gameName = (string)aOptions["gameName"];
        m_isPlaying = (int)aOptions["isPlaying"];
    }

    //[Server]
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
