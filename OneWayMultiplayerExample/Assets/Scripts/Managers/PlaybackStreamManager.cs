using System.Collections;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using UnityEngine;

public class PlaybackStreamManager : MonoBehaviour
{
    private static PlaybackStreamManager _instance;

    public static PlaybackStreamManager Instance => _instance;
    
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

    public List<TroopAI> InvadersList = new List<TroopAI>();
    public List<TroopAI> DefendersList = new List<TroopAI>();
    public List<StructureHealthBehavior> StructuresList = new List<StructureHealthBehavior>();
    private Coroutine _replayCoroutine;
    private DefenderSpawner _defenderSpawner;
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
        
        _defenderSpawner = FindObjectOfType<DefenderSpawner>();
        _spawnController = FindObjectOfType<SpawnController>();
        _invaderSpawnData = _spawnController.SpawnData;
    }
    
    public void ReadIDs(string in_jsonResponse)
    {
        _invaderIDs.Clear();
        _defenderIDs.Clear();
        
        Dictionary<string, object> response = JsonReader.Deserialize(in_jsonResponse) as Dictionary<string, object>;
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        
        Dictionary<string, object> invadersList = data["invadersList"] as Dictionary<string, object>;
        for (int i = 0; i < invadersList.Count; i++)
        {
            _invaderIDs.Add((int) invadersList[i.ToString()]);    
        }
        
        Dictionary<string, object> defendersList = data["defendersList"] as Dictionary<string, object>;
        for (int i = 0; i < defendersList.Count; i++)
        {
            _defenderIDs.Add((int) defendersList[i.ToString()]);
        }
    }

    //
    public void StartStream()
    {
        InvadersList.Clear();
        DefendersList.Clear();
        _spawnController.SpawnCount = 0;
        GameManager.Instance.PrepareGameForPlayback();
        GameManager.Instance.SessionManager.GameOverScreen.gameObject.SetActive(false);
        //Start Stream
        
        _frameId = 0;
        _replayCoroutine = StartCoroutine(StartPlayBack());
    }

    IEnumerator StartPlayBack()
    {
        int replayIndex = 0;
        var _actionReplayRecords = GameManager.Instance.ReplayRecords;
        while (replayIndex < _actionReplayRecords.Count)
        {
            if (_frameId == _actionReplayRecords[replayIndex].frameID)
            {
                switch (_actionReplayRecords[replayIndex].eventID)
                {
                    //Any spawn event is automatically an invader because defenders are spawned earlier. 
                    case EventId.Spawn:
                        TroopAI prefab = _invaderSpawnData.GetTroop(_actionReplayRecords[replayIndex].troopType);
                        TroopAI troop = Instantiate(prefab, _actionReplayRecords[replayIndex].position, Quaternion.identity);
                        troop.IsInPlaybackMode = true;
                        troop.AssignToTeam(0);
                        InvadersList.Add(troop);
                        break;
                    case EventId.Ids:
                        //This event is handled when stream is read and then calls ReadIDs
                        break;
                    case EventId.Target:
                        //Troop specific target
                        if (_actionReplayRecords[replayIndex].targetID > 10)
                        {
                            TroopAI trooper;
                            //Check if attacker is on invader or defenders team then get the trooper.
                            if (_actionReplayRecords[replayIndex].teamID == 0)
                            {
                                trooper = InvadersList.Find((ai => ai.TroopID == _actionReplayRecords[replayIndex].troopID));   
                            }
                            else
                            {
                                trooper = DefendersList.Find((ai => ai.TroopID == _actionReplayRecords[replayIndex].troopID));
                            }
                            
                            //Then check the opposite team of the trooper we just found to get the target.
                            if (trooper.TeamID == 0)
                            {
                                trooper.Target = DefendersList.Find(ai => ai.TroopID == _actionReplayRecords[replayIndex].targetID).gameObject;
                            }
                            else
                            {
                                trooper.Target = InvadersList.Find(ai => ai.TroopID == _actionReplayRecords[replayIndex].targetID).gameObject;
                            }
                        }
                        //Structure specific target
                        else
                        {
                            //Find troop with TroopID
                            TroopAI invaderTroop = InvadersList.Find((ai => ai.TroopID == _actionReplayRecords[replayIndex].troopID));
                            //Get structure based off targetID
                            GameObject target = StructuresList.Find((structure => structure.StructureID == _actionReplayRecords[replayIndex].targetID)).gameObject;
                            invaderTroop.Target = target;
                        }
                        break;
                }
                replayIndex++;
            }

            yield return new WaitForFixedUpdate();
        }
        yield return null;
    }
}
