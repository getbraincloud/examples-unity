using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class demonstrates how to execute a playback stream with specific events once the events have been read
/// from BrainCloud.
///
/// During gameplay of the stream, this system will only read Spawn, Destroy and Target Reassigned to execute.
/// Before the stream, we will look through the events and grab the defender set selection and a list of ID's to
/// use during playback.  
///
/// </summary>

public class PlaybackStreamManager : MonoBehaviour
{
    private static PlaybackStreamManager _instance;

    public static PlaybackStreamManager Instance => _instance;

    public List<BaseHealthBehavior> InvadersList = new List<BaseHealthBehavior>();
    public List<BaseHealthBehavior> DefendersList = new List<BaseHealthBehavior>();
    public List<BaseHealthBehavior> StructuresList = new List<BaseHealthBehavior>();
    private int replayIndex;
    private SpawnData _invaderSpawnData;
    private SpawnController _spawnController;
    
    //Time specific
    private float _startTime;
    private float _time;
    private float _value;
    private int _frameId;

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
        
        _spawnController = FindObjectOfType<SpawnController>();
        _invaderSpawnData = _spawnController.SpawnData;
    }

    private void Start()
    {
        if(GameManager.Instance.IsInPlaybackMode)
        {
            GameManager.Instance.ResetGameSceneForStream();
            StartStream();
        }
    }

    //Specifically for a button in the Game over screen to replay the game that just finished
    public void LoadStreamThenStart()
    {
        _frameId = 0;
        InvadersList.Clear();
        DefendersList.Clear();
        GameManager.Instance.ClearGameobjects();
        NetworkManager.Instance.ReadStream();
    }

    public void StartStream()
    {
        StartCoroutine(StartPlayBack());
    }

    private IEnumerator StartPlayBack()
    {
        replayIndex = 0;
        var _actionReplayRecords = GameManager.Instance.ReplayRecords;
        while (replayIndex < _actionReplayRecords.Count)
        {
            if (_frameId >= _actionReplayRecords[replayIndex].frameID)
            {
                switch (_actionReplayRecords[replayIndex].eventID)
                {
                    //Any spawn event is automatically an invader because defenders are spawned earlier. 
                    case EventId.Spawn:
                        SpawnTroop(_actionReplayRecords[replayIndex]);
                        break;
                    case EventId.Destroy:
                        DestroyTarget(_actionReplayRecords[replayIndex]);
                        break;
                    case EventId.Target:
                        AssignTarget(_actionReplayRecords[replayIndex]);
                        break;
                }

                if (replayIndex < _actionReplayRecords.Count)
                {
                    replayIndex++;    
                }
            }
            //Break out of loop if we're done going through list
            if (replayIndex >= _actionReplayRecords.Count)
            {
                break;
            }
            //Continue incrementing frames until we have a frame to do stuff
            if (_frameId != _actionReplayRecords[replayIndex].frameID)
            {
                yield return new WaitForFixedUpdate();    
            }
            _frameId++;
        }

        while (!GameManager.Instance.CheckIfGameOver())
        {
            yield return new WaitForFixedUpdate();
        }
        //Set up gameover screen
        GameManager.Instance.SessionManager.GameOverScreen.gameObject.SetActive(true);
    }

    private void SpawnTroop(PlaybackStreamRecord in_record)
    {
        TroopAI prefab = _invaderSpawnData.GetTroop(in_record.troopType);
        TroopAI troop = Instantiate(prefab, in_record.position, Quaternion.identity);
        troop.IsInPlaybackMode = true;
        troop.TargetIsHostile = true;
        troop.EntityID = in_record.entityID;
        troop.AssignToTeam(0);
        InvadersList.Add(troop);
    }

    /// <summary>
    /// Determine if target is a troop or a structure
    /// Troops will have a big negative number whereas structures will have an ID from 0-10
    /// </summary>
    /// <param name="in_record"></param>
    private void AssignTarget(PlaybackStreamRecord in_record)
    {
        BaseHealthBehavior target = null;
        
        //Troops
        if (in_record.targetID < -1)
        {
            //Determine what team this troop is on.
            //Invader
            if (in_record.targetTeamID == 0)
            {
                target = GetObjectFromList(InvadersList, in_record.targetID);
            }
            //Defender
            else
            {
                target = GetObjectFromList(DefendersList, in_record.targetID);
            }
        }
        //Structures
        else
        {
            target = GetObjectFromList(StructuresList, in_record.targetID);
        }

        if (!target)
        {
            return;
        }

        TroopAI troop = null;
        //Now determine which troop to assign this target to
        //Invader
        if (in_record.teamID == 0)
        {
            troop = (TroopAI) GetObjectFromList(InvadersList, in_record.entityID);
        }
        //Defender
        else
        {
            troop =  (TroopAI) GetObjectFromList(DefendersList, in_record.entityID);
        }

        if (troop)
        {
            troop.Target = target.gameObject;    
        }
    }

    private void DestroyTarget(PlaybackStreamRecord in_record)
    {
        //Structures
        BaseHealthBehavior target = null;
        if (in_record.teamID <= -1)
        {
            target = GetObjectFromList(StructuresList, in_record.entityID);
            if (target)
            {
                StructuresList.Remove(target);
                Destroy(target.gameObject);
            }
        }
        //Invaders
        else if (in_record.teamID == 0)
        {
            target = GetObjectFromList(InvadersList, in_record.entityID);
            if (target)
            {
                InvadersList.Remove(target);
                Destroy(target.gameObject);
            }
        }
        //Defenders
        else
        {
            target = GetObjectFromList(DefendersList, in_record.entityID);
            if (target)
            {
                DefendersList.Remove(target);
                Destroy(target.gameObject);
            }
        }
    }

    private BaseHealthBehavior GetObjectFromList(List<BaseHealthBehavior> in_listToSearch, int in_idToMatch)
    {
        BaseHealthBehavior value = null;
        for (int i = 0; i < in_listToSearch.Count; i++)
        {
            if (in_listToSearch[i].EntityID == in_idToMatch)
            {
                if (in_listToSearch[i] != null)
                {
                    value = in_listToSearch[i];
                }
                else
                {
                    Debug.LogWarning("Object is destroyed from list but not removed");
                }
                break;
            }
        }

        if (value == null)
        {
            Debug.LogWarning("Object is missing from list");
        }
        return value;
    }
}
