using System.Collections;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using UnityEngine;

public class PlaybackStreamManager : MonoBehaviour
{
    private static PlaybackStreamManager _instance;

    public static PlaybackStreamManager Instance => _instance;

    public List<TroopAI> InvadersList = new List<TroopAI>();
    public List<TroopAI> DefendersList = new List<TroopAI>();
    public List<StructureHealthBehavior> StructuresList = new List<StructureHealthBehavior>();
    private Coroutine _replayCoroutine;
    
    private SpawnData _invaderSpawnData;
    private SpawnController _spawnController;
    
    //Time specific
    private float _startTime;
    private float _time;
    private float _value;
    private int _frameId;
    private bool _replayMode;
    
    public int FrameID
    {
        get => _frameId;
    }
    
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
                        troop.TroopID = _actionReplayRecords[replayIndex].entityID;
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
        GameObject target = null;
        //Determine if target is a troop or structure
        // Troops will have a big negative number whereas structures will have an ID from 0-10
        //Troops
        if (in_record.targetID < -1)
        {
            //Determine what team this troop is on.
            //Invader
            if (in_record.targetTeamID == 0)
            {
                for (int i = 0; i < InvadersList.Count; i++)
                {
                    if (InvadersList[i].TroopID == in_record.targetID)
                    {
                        if (InvadersList[i])
                        {
                            target = InvadersList[i].gameObject;
                            break;
                        }
                    }
                }
            }
            //Defender
            else
            {
                for (int i = 0; i < DefendersList.Count; i++)
                {
                    if (DefendersList[i].TroopID == in_record.targetID)
                    {
                        if (DefendersList[i])
                        {
                            target = DefendersList[i].gameObject;
                            break;
                        }
                    }
                }
            }
        }
        //Structures
        else
        {
            for (int i = 0; i < StructuresList.Count; i++)
            {
                if (StructuresList[i].StructureID == in_record.targetID)
                {
                    if (StructuresList[i])
                    {
                        target = StructuresList[i].gameObject;
                        break;    
                    }
                }
            }
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
            for (int i = 0; i < InvadersList.Count; i++)
            {
                if (InvadersList[i].TroopID == in_record.entityID)
                {
                    troop = InvadersList[i];
                }
            }
        }
        //Defender
        else
        {
            for (int i = 0; i < DefendersList.Count; i++)
            {
                if (DefendersList[i].TroopID == in_record.entityID)
                {
                    troop = DefendersList[i];
                }
            }
        }

        if (troop != null)
        {
            troop.Target = target;    
        }
        else
        {
            Debug.LogWarning("Troop couldn't be found....");
        }
    }

    private void DestroyTarget(ActionReplayRecord in_record)
    {
        //Trooper
        if (in_record.targetID < -1)
        {
            //Invaders
            if (in_record.teamID == 0)
            {
                for (int i = 0; i < InvadersList.Count; i++)
                {
                    if (InvadersList[i].TroopID == in_record.entityID)
                    {
                        var person = InvadersList[i];
                        if (person)
                        {
                            InvadersList.Remove(person);
                            Destroy(person);
                            break;
                        }
                    }
                }
            }
            //Defenders
            else
            {
                for (int i = 0; i < DefendersList.Count; i++)
                {
                    if (DefendersList[i].TroopID == in_record.entityID)
                    {
                        var person = DefendersList[i];
                        if (person)
                        {
                            DefendersList.Remove(person);
                            Destroy(person);
                            break;
                        }
                    }
                }
            }
        }
        //Structures
        else
        {
            for (int i = 0; i < StructuresList.Count; i++)
            {
                if (StructuresList[i].StructureID == in_record.entityID)
                {
                    var house = StructuresList[i];
                    if (house)
                    {
                        StructuresList.Remove(house);
                        Destroy(house);
                        break;
                    }
                }
            }
        }
    }
}
