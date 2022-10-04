using System;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ArmyDivisionRank{Easy,Medium,Hard,None,Test}
public enum ArmyType {Invader,Defense}

public class GameManager : MonoBehaviour
{
    public SpawnData DefenderSpawnData;
    public SpawnData InvaderSpawnData;

    public bool IsInPlaybackMode;
    private bool _isGameActive;
    private GameSessionManager _sessionManagerRef;
    public GameSessionManager SessionManager
    {
        get => _sessionManagerRef;
    }
    
    private List<ActionReplayRecord> _replayRecords = new List<ActionReplayRecord>();
    public List<ActionReplayRecord> ReplayRecords
    {
        get => _replayRecords;
    }
    
    private GameOverScreen _gameOverScreenRef;
    private int _startingDefenderCount;
    private int _startingInvaderCount;
    private int _defenderTroopCount;
    public int DefenderTroopCount
    {
        get => _defenderTroopCount;
        set => _defenderTroopCount = value;
    }

    private int _invaderTroopCount;

    public int InvaderTroopCount
    {
        get => _invaderTroopCount;
        set => _invaderTroopCount = value;
    }
    
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
    
    //Data to send for playback
    private List<int> _defenderIDs = new List<int>();
    public List<int> DefenderIDs
    {
        get => _defenderIDs;
        set => _defenderIDs = value;
    }
    private List<int> _invaderIDs = new List<int>();

    public List<int> InvaderIDs
    {
        get => _invaderIDs;
        set => _invaderIDs = value;
    }

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
        DontDestroyOnLoad(gameObject);
        _currentUserInfo = Settings.LoadPlayerInfo();
        InvaderSpawnData.AssignSpawnList(_currentUserInfo.InvaderSelected);
        if(MenuManager.Instance != null)
        {
            MenuManager.Instance.UsernameInputField.text = _currentUserInfo.Username;
            MenuManager.Instance.PasswordInputField.text = PlayerPrefs.GetString(Settings.PasswordKey);
        }
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
        InvaderSpawnData.Rank = (ArmyDivisionRank) in_invaderSelection;
        InvaderSpawnData.AssignSpawnList(InvaderSpawnData.Rank);
    }

    public void UpdateOpponentInfo(ArmyDivisionRank in_rank, string in_entityId)
    {
        UpdateEntityId(in_entityId);
        _opponentUserInfo.DefendersSelected = in_rank;
        DefenderSpawnData.AssignSpawnList(in_rank);
    }

    public void UpdateFromReadResponse(string in_entityId, int in_defenderSelection, int in_invaderSelection)
    {
        UpdateEntityId(in_entityId);
        UpdateLocalArmySelection(in_defenderSelection, in_invaderSelection);
    }

    public void LoadToGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void LoadToPlaybackScene()
    {
        IsInPlaybackMode = true;
        SceneManager.LoadScene("Game");
    }

    public void SetUpGameValues(Transform in_defenderParent)
    {
        _defenderStructParent = in_defenderParent;
        PlaybackStreamManager.Instance.StructuresList.Clear();
        for (int i = 0; i < _defenderStructParent.childCount; i++)
        {
            GameObject structure = _defenderStructParent.GetChild(i).gameObject;
            BaseHealthBehavior healthScript = structure.GetComponent<BaseHealthBehavior>(); 
            healthScript.EntityID = i;
            PlaybackStreamManager.Instance.StructuresList.Add(healthScript);
        }
        
        _sessionManagerRef = FindObjectOfType<GameSessionManager>();
        _gameOverScreenRef = FindObjectOfType<GameOverScreen>();
        _isGameActive = true;
        _startingDefenderCount = _defenderTroopCount;
        _startingInvaderCount = _invaderTroopCount;
    }

    public void GameOver(bool in_didInvaderWin, bool in_didTimeExpire = false)
    {
        if (!_isGameActive) return;
        _isGameActive = false;
        
        //Fill in defender winning stuff here
        var slayCount = _startingDefenderCount - _defenderTroopCount;
        var counterAttackCount = _startingInvaderCount - _invaderTroopCount;

        _gameOverScreenRef.InvadersDefeatedText.text = $"Number of slayed troops: {slayCount}";
        _gameOverScreenRef.DefendersDefeatedText.text = $"Number of defeated troop: {counterAttackCount}";
        
        //Figure out who won
        if (in_didTimeExpire)
        {
            _gameOverScreenRef.WinStatusText.text = "Time Expired";
        }
        else if(in_didInvaderWin)
        {
            _gameOverScreenRef.WinStatusText.text = "You Win !";
        }
        else
        {
            _gameOverScreenRef.WinStatusText.text = "Your troops are defeated";
        }
        
        BrainCloudManager.Instance.GameCompleted(in_didInvaderWin);
        
        //Do Game over things
        _sessionManagerRef.StopTimer();
        
        FindObjectOfType<SpawnController>().enabled = false;
        
        _gameOverScreenRef.gameObject.SetActive(true);
    }

    public void CheckIfGameOver()
    {
        if (!_isGameActive) return;
        if (_defenderStructParent.childCount == 0 ||
            _invaderTroopCount == 0)
        {
            GameOver(_invaderTroopCount > 0);
        }
    }
    
    public void PrepareGameForPlayback()
    {
        IsInPlaybackMode = true;
        _isGameActive = true;
        var troopsToDestroy = FindObjectsOfType<TroopAI>();
        foreach (var troopAI in troopsToDestroy)
        {
            Destroy(troopAI.gameObject);
        }

        var _defenderSpawner = FindObjectOfType<DefenderSpawner>();
        if (_defenderSpawner)
        {
            //Set up defenders
            _defenderSpawner.SpawnDefenderSetup();    
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

    public int RemainingStructures() => _defenderStructParent.childCount;
}
