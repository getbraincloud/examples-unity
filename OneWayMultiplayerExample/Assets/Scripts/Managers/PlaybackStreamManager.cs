using System.Collections;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using UnityEngine;

public class PlaybackStreamManager : MonoBehaviour
{
    private static PlaybackStreamManager _instance;

    public static PlaybackStreamManager Instance => _instance;

    public List<BaseHealthBehavior> InvadersList = new List<BaseHealthBehavior>();
    public List<BaseHealthBehavior> DefendersList = new List<BaseHealthBehavior>();
    public List<BaseHealthBehavior> StructuresList = new List<BaseHealthBehavior>();
    private Coroutine _replayCoroutine;
    
    private SpawnData _invaderSpawnData;
    private SpawnController _spawnController;
    
    //Time specific
    private float _startTime;
    private float _time;
    private float _value;
    private int _frameId;
    private bool _replayMode;

    void Awake()
    {
        if (!_instance)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        _spawnController = FindObjectOfType<SpawnController>();
        _invaderSpawnData = _spawnController.SpawnData;
    }

    public void StopStream()
    {
        GameManager.Instance.SessionManager.GameOverScreen.gameObject.SetActive(true);
        StopCoroutine(_replayCoroutine);
    }

    //Specifically for a button in the Game over screen to replay the game that just finished
    public void LoadStreamThenStart()
    {
        BrainCloudManager.Instance.ReadStream();
    }

    //
    public void StartStream()
    {
        _frameId = 0;
        InvadersList.Clear();
        DefendersList.Clear();
        GameManager.Instance.PrepareGameForPlayback();
        GameManager.Instance.SessionManager.GameOverScreen.gameObject.SetActive(false);
        _replayCoroutine = StartCoroutine(StartPlayBack());
    }

    IEnumerator StartPlayBack()
    {
        int replayIndex = 0;
        var _actionReplayRecords = GameManager.Instance.ReplayRecords;
        while (replayIndex < _actionReplayRecords.Count)
        {
            if (_frameId >= _actionReplayRecords[replayIndex].frameID)
            {
                switch (_actionReplayRecords[replayIndex].eventID)
                {
                    //Any spawn event is automatically an invader because defenders are spawned earlier. 
                    case EventId.Spawn:
                        TroopAI prefab = _invaderSpawnData.GetTroop(_actionReplayRecords[replayIndex].troopType);
                        TroopAI troop = Instantiate(prefab, _actionReplayRecords[replayIndex].position, Quaternion.identity);
                        troop.IsInPlaybackMode = true;
                        troop.TargetIsHostile = true;
                        troop.EntityID = _actionReplayRecords[replayIndex].entityID;
                        troop.AssignToTeam(0);
                        InvadersList.Add(troop);
                        break;
                    case EventId.Ids:
                        //This event is handled when stream is read and then calls ReadIDs
                        break;
                    case EventId.Destroy:
                        DestroyTarget(_actionReplayRecords[replayIndex]);
                        break;
                    case EventId.Target:
                        AssignTarget(_actionReplayRecords[replayIndex]);
                        break;
                }
                replayIndex++;
            }
            yield return new WaitForFixedUpdate();
            _frameId++;
        }
        yield return null;
    }

    private void AssignTarget(ActionReplayRecord in_record)
    {
        BaseHealthBehavior target = null;
        //Determine if target is a troop or structure
        // Troops will have a big negative number whereas structures will have an ID from 0-10
        //Troops
        if (in_record.targetID < -1)
        {
            //Determine what team this troop is on.
            //Invader
            if (in_record.targetTeamID == 0)
            {
                target = GetTargetFromList(InvadersList, in_record);
            }
            //Defender
            else
            {
                target = GetTargetFromList(DefendersList, in_record);
            }
        }
        //Structures
        else
        {
            target = GetTargetFromList(StructuresList, in_record);
        }

        if (target == null)
        {
            Debug.LogWarning("Couldn't find target..");
        }

        TroopAI troop = null;
        //Now determine which troop to assign this target to
        //Invader
        if (in_record.teamID == 0)
        {
            troop = (TroopAI) GetObjectFromList(InvadersList, in_record);
        }
        //Defender
        else
        {
            troop =  (TroopAI) GetObjectFromList(DefendersList, in_record);
        }

        if (troop != null && target != null)
        {
            troop.Target = target.gameObject;    
        }
        else
        {
            Debug.LogWarning("Troop or target couldn't be found....");
        }
    }

    private void DestroyTarget(ActionReplayRecord in_record)
    {
        //Trooper
        if (in_record.targetID < -1)
        {
            BaseHealthBehavior troop = null;
            //Invaders
            if (in_record.teamID == 0)
            {
                troop = GetObjectFromList(InvadersList, in_record);
            }
            //Defenders
            else
            {
                troop = GetObjectFromList(DefendersList, in_record);
            }
            
            if (troop)
            {
                DefendersList.Remove(troop);
                Destroy(troop.gameObject);
            }
        }
        //Structures
        else
        {
            var house = GetObjectFromList(StructuresList, in_record);
            if (house)
            {
                StructuresList.Remove(house);
                Destroy(house.gameObject);
            }
        }
    }

    private BaseHealthBehavior GetObjectFromList(List<BaseHealthBehavior> in_listToSearch, ActionReplayRecord in_record)
    {
        BaseHealthBehavior value = null;
        for (int i = 0; i < in_listToSearch.Count; i++)
        {
            if (in_listToSearch[i].EntityID == in_record.entityID)
            {
                if (in_listToSearch[i] != null)
                {
                    value = in_listToSearch[i];
                    break;
                }
            }
        }
        return value;
    }

    private BaseHealthBehavior GetTargetFromList(List<BaseHealthBehavior> in_listToSearch, ActionReplayRecord in_record)
    {
        BaseHealthBehavior value = null;
        for (int i = 0; i < in_listToSearch.Count; i++)
        {
            if (in_listToSearch[i].EntityID == in_record.targetID)
            {
                if (in_listToSearch[i] != null)
                {
                    value = in_listToSearch[i];
                    
                }
                else
                {
                    Debug.LogWarning("Target is missing from list");
                }
                break;
            }
        }
        return value;
    }
}
