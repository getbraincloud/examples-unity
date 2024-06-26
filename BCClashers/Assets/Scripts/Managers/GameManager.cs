using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This class is mainly used to connect the data from the different managers so data
/// from Menu meets with in Game. Examples of this is as follows:
///     - Invader & Defender Spawn Data
///     - Class References
///     - Holds list of playback records when reading a playback stream
///     - Local and opponent user info (Username, ProfileId, Matches played, etc)
///     - Set up Game scene (Including Defenders & Structures)
/// 
/// </summary>

public enum ArmyDivisionRank{Easy,Medium,Hard,None,Test}
public enum ArmyType {Invader,Defense}

public class GameManager : MonoBehaviour
{
    public SpawnData DefenderSpawnData;
    public SpawnData InvaderSpawnData;

    public bool IsInPlaybackMode;
    private bool _isGameActive;

    public bool GameActive
    {
        get => _isGameActive;
        set => _isGameActive = value;
    }
    private GameSessionManager _sessionManagerRef;
    public GameSessionManager SessionManager => GetSessionManager();

    private readonly List<PlaybackStreamRecord> _replayRecords = new List<PlaybackStreamRecord>();
    public List<PlaybackStreamRecord> ReplayRecords => _replayRecords;

    private GameOverScreen _gameOverScreenRef;
    private int _startingDefenderCount;
    private int _startingInvaderCount;

    //Transform parent of structure sets for defender user
    private Transform _defenderStructParent;

    //Local User Info
    private UserInfo _currentUserInfo;
    public UserInfo CurrentUserInfo
    {
        get => _currentUserInfo;
        set => _currentUserInfo = value;
    }

    private UserInfo _opponentUserInfo;
    public UserInfo OpponentUserInfo
    {
        get => _opponentUserInfo;
        set => _opponentUserInfo = value;
    }
    
    private static GameManager _instance;
    public static GameManager Instance => _instance;
    
    private int _invaderTroopCount;

    public int InvaderTroopCount
    {
        get => _invaderTroopCount;
        set => _invaderTroopCount = value;
    }
    
    private List<int> _invaderIDs = new List<int>();

    public List<int> InvaderIDs
    {
        get => _invaderIDs;
        set => _invaderIDs = value;
    }

    private List<SpawnInfo> _invaderSpawnInfo;
    public List<SpawnInfo> InvaderSpawnInfo
    {
        get => _invaderSpawnInfo;
        set => _invaderSpawnInfo = value;
    }
    
    private int _defenderTroopCount;
    public int DefenderTroopCount
    {
        get => _defenderTroopCount;
        set => _defenderTroopCount = value;
    }
    
    private List<SpawnInfo> _defenderSpawnInfo;
    public List<SpawnInfo> DefenderSpawnInfo
    {
        get => _defenderSpawnInfo;
        set => _defenderSpawnInfo = value;
    }
    private ArmyDivisionRank _defenderRank = ArmyDivisionRank.None;
    public ArmyDivisionRank DefenderRank
    {
        get => _defenderRank;
        set => _defenderRank = value;
    }
    //Data to send for playback
    private List<int> _defenderIDs = new List<int>();
    public List<int> DefenderIDs
    {
        get => _defenderIDs;
        set => _defenderIDs = value;
    }

    private List<GameObject> _projectiles = new List<GameObject>();
    public List<GameObject> Projectiles
    {
        get => _projectiles;
        set => _projectiles = value;
    }

    private List<TroopAI> _troops = new List<TroopAI>();
    public List<TroopAI> Troops
    {
        get => _troops;
        set => _troops = value;
    }

    public StreamInfo InvadedStreamInfo;
    
    private void Awake()
    {
        InvadedStreamInfo = new StreamInfo();
        if (!_instance)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
        _currentUserInfo = Settings.LoadPlayerInfo();
    }

    public bool IsEntityIdValid()
    {
        return !_currentUserInfo.EntityId.IsNullOrEmpty();
    }

    public void UpdateEntityId(string in_id)
    {
        _currentUserInfo.EntityId = in_id;
        Settings.SaveEntityId(in_id);
    }

    public void UpdateLocalArmySelection(int in_defenderSelection, int in_invaderSelection)
    {
        _currentUserInfo.InvaderSelected = (ArmyDivisionRank) in_invaderSelection;
        _currentUserInfo.DefendersSelected = (ArmyDivisionRank) in_defenderSelection;
        InvaderSpawnInfo = InvaderSpawnData.GetSpawnList(_currentUserInfo.InvaderSelected);
    }

    public void UpdateOpponentInfo(ArmyDivisionRank in_rank, string in_entityId)
    {
        _opponentUserInfo.EntityId = in_entityId;
        _opponentUserInfo.DefendersSelected = in_rank;
        _defenderRank = in_rank;
        DefenderSpawnInfo = DefenderSpawnData.GetSpawnList(in_rank);
    }

    public void UpdateFromReadResponse(string in_entityId, int in_defenderSelection, int in_invaderSelection)
    {
        UpdateEntityId(in_entityId);
        UpdateLocalArmySelection(in_defenderSelection, in_invaderSelection);
    }

    public void UpdateSpawnInvaderList()
    {
        InvaderSpawnInfo = InvaderSpawnData.GetSpawnList(_currentUserInfo.InvaderSelected);
    }

    public void LoadToGame()
    {
        InvaderSpawnInfo = InvaderSpawnData.GetSpawnList(_currentUserInfo.InvaderSelected);
        SceneManager.LoadScene("Game");
    }

    public void LoadToPlaybackScene()
    {
        IsInPlaybackMode = true;
        SceneManager.LoadScene("Game");
    }

    public void ResetGameSceneForStream()
    {
        IsInPlaybackMode = true;
        GetSessionManager().StopStreamButton.SetActive(true);
        GameSetup();
        SessionManager.GameOverScreen.gameObject.SetActive(false);
    }

    private GameSessionManager GetSessionManager()
    {
        if(_sessionManagerRef == null)
        {
            _sessionManagerRef = FindObjectOfType<GameSessionManager>();
        }

        return _sessionManagerRef;
    }

    public void GameSetup()
    {
        SetUpSpawners();
        
        PlaybackStreamManager.Instance.StructuresList.Clear();
        for (int i = 0; i < _defenderStructParent.GetChild(0).childCount; i++)
        {
            GameObject structure = _defenderStructParent.GetChild(0).GetChild(i).gameObject;
            BaseHealthBehavior healthScript = structure.GetComponent<BaseHealthBehavior>();
            healthScript.EntityID = i;
            PlaybackStreamManager.Instance.StructuresList.Add(healthScript);
        }

        if (!_gameOverScreenRef)
        {
            _gameOverScreenRef = FindObjectOfType<GameOverScreen>();    
        }

        if (_gameOverScreenRef)
        {
            _gameOverScreenRef.gameObject.SetActive(false);
        }
        
        _isGameActive = true;
        _startingDefenderCount = _defenderTroopCount;
        _startingInvaderCount = _invaderTroopCount;
    }

    public void GameOver(bool in_didInvaderWin, bool in_didTimeExpire = false)
    {
        if (!_isGameActive) return;
        _isGameActive = false;
        int structureKillCount = 0;
        var slayCount = 0;
        var counterAttackCount = 0;
        if (!IsInPlaybackMode)
        {
            //Fill in defender winning stuff here
            slayCount = _startingDefenderCount - _defenderTroopCount;
            counterAttackCount = _startingInvaderCount - _invaderTroopCount;

            //Figure out who won
            if (in_didTimeExpire)
            {
                _gameOverScreenRef.WinStatusText.text = "Time Expired";
            }
            else if(in_didInvaderWin)
            {
                _gameOverScreenRef.WinStatusText.text = "Victory !";
            }
            else
            {
                _gameOverScreenRef.WinStatusText.text = "Defeated...";
            }

            if (NetworkManager.Instance)
            {
                structureKillCount = NetworkManager.Instance.StructureKillCount;
                NetworkManager.Instance.IncreaseGoldFromGameStats(slayCount, _invaderTroopCount);
                NetworkManager.Instance.SummaryInfo(slayCount, counterAttackCount, GetSessionManager().GameSessionTimer);
                NetworkManager.Instance.GameCompleted(in_didInvaderWin);
            }
        }
        else
        {
            slayCount = NetworkManager.Instance.SlayCount;
            counterAttackCount = NetworkManager.Instance.DefeatedTroops;
            GetSessionManager().GameSessionTimer = NetworkManager.Instance.TimeLeft;

            _gameOverScreenRef.WinStatusText.text = "Recording finished !";
        }

        _gameOverScreenRef.StructuresDefeatedText.text = $"Structures Destroyed: {structureKillCount}";
        _gameOverScreenRef.InvadersDefeatedText.text = $"Slain Troops: {slayCount}";
        _gameOverScreenRef.DefendersDefeatedText.text = $"Troops Lost: {counterAttackCount}";

        int goldGained = (slayCount * 10000) + (_invaderTroopCount * 10000) + (structureKillCount * 10000);
        _gameOverScreenRef.GoldEarnedText.text = "Gold earned: " + goldGained.ToString("#,#");
        
        //clean up projectiles
        if (_projectiles.Count > 0)
        {
            foreach (GameObject projectile in _projectiles)
            {
                Destroy(projectile);
            }
        }
        _projectiles.Clear();
        
        //clean up projectiles
        if (_troops.Count > 0)
        {
            foreach (TroopAI troop in _troops)
            {
                if (troop != null)
                {
                    troop.Dead();    
                }
            }
        }
        _troops.Clear();

        //Do Game over things
        GetSessionManager().StopTimer();
        
        FindObjectOfType<SpawnController>().enabled = false;
        
        
        _gameOverScreenRef.gameObject.SetActive(true);
    }

    public bool CheckIfGameOver()
    {
        if (!_isGameActive) return true;
        if (_defenderStructParent.GetChild(0).childCount == 0 ||
            _invaderTroopCount == 0)
        {
            GameOver(_invaderTroopCount > 0);
            return true;
        }

        return false;
    }

    public void ClearGameobjects()
    {
        _isGameActive = true;
        IsInPlaybackMode = true;
        GetSessionManager().SetupGameSession();
        var troopsToDestroy = FindObjectsOfType<TroopAI>();
        foreach (var troopAI in troopsToDestroy)
        {
            Destroy(troopAI.gameObject);
        }

        var housesToDestroy = FindObjectsOfType<BaseHealthBehavior>();
        foreach (var house in housesToDestroy)
        {
            Destroy(house.gameObject);
        }
    }
    
    //Sets up defender and invader spawner logic
    private void SetUpSpawners()
    {
        var _invaderSpawner = FindObjectOfType<SpawnController>();
        if (_invaderSpawner)
        {
            _invaderSpawner.SetUpInvaders();
        }

        var _defenderSpawner = FindObjectOfType<DefenderSpawner>();
        if (_defenderSpawner)
        {
            if (!IsInPlaybackMode)
            {
                _defenderIDs.Clear();
            }
            //Set up defenders
            _defenderSpawner.SpawnDefenderSetup();
            _defenderStructParent = _defenderSpawner.DefenderParent;
        }
    }
    
    public void ReadIDs(Dictionary<string, object> events)
    {
        _invaderIDs.Clear();
        _defenderIDs.Clear();
        
        Dictionary<string, object> invadersList = events["invadersList"] as Dictionary<string, object>;
        for (int i = 0; i < invadersList.Count; i++)
        {
            _invaderIDs.Add((int) invadersList[i.ToString()]);    
        }
        
        Dictionary<string, object> defendersList = events["defendersList"] as Dictionary<string, object>;
        for (int i = 0; i < defendersList.Count; i++)
        {
            _defenderIDs.Add((int) defendersList[i.ToString()]);
        }
    }

    public void OnReadSetDefenderList(ArmyDivisionRank in_rank)
    {
        _defenderRank = in_rank;
        DefenderSpawnInfo = DefenderSpawnData.GetSpawnList(in_rank);
    }
    
    public void OnReadSetInvaderList(ArmyDivisionRank in_rank)
    {
        _currentUserInfo.InvaderSelected = in_rank;
        InvaderSpawnInfo = InvaderSpawnData.GetSpawnList(in_rank);
    }

    public int RemainingStructures() => _defenderStructParent.childCount;
}
