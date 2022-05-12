using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public enum ArmyDivisionRank{Easy,Medium,Hard,None}
public enum ArmyType {Invader,Defense}

public class GameManager : MonoBehaviour
{
    public SpawnData DefenderSpawnData;
    public SpawnData InvaderSpawnData;
    
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
    
    //Singleton Pattern
    private static GameManager _instance;
    public static GameManager Instance => _instance;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (!_instance)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        _currentUserInfo = Settings.LoadPlayerInfo();
        InvaderSpawnData.AssignSpawnList(_currentUserInfo.InvaderSelected);
        MenuManager.Instance.UsernameInputField.text = _currentUserInfo.Username;
        MenuManager.Instance.PasswordInputField.text = PlayerPrefs.GetString(Settings.PasswordKey);
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
}
