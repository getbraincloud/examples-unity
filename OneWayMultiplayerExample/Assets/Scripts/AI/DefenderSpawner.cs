using System.Collections.Generic;
using UnityEngine;

public class DefenderSpawner : MonoBehaviour
{
    public Transform[] SpawnPoints;
    public Transform StructureSpawnPoint;
    public SpawnData DefenderSpawnData;
    //0 = Easy, 1 = Medium, 2 = Large
    public GameObject[] Sets;
    private int _spawnPointIndex;
    private bool _addOffset;
    private int _offsetRangeZ = 6;
    private int _offsetRangeX = 6;
    public bool TestingMode;
    private Transform _defenderParent;
    public ArmyDivisionRank TestRank;

    public Transform DefenderParent
    {
        get => _defenderParent;
    }

    private void Start()
    {
        if (NetworkManager.Instance == null)
        {
            TestingMode = true;
            GameManager.Instance.DefenderRank = TestRank;
            GameManager.Instance.DefenderSpawnInfo = DefenderSpawnData.TestParameterList;
        }
    }

    public void SpawnDefenderSetup()
    {
        TroopAI troopToSpawn;
        _addOffset = false;
        _spawnPointIndex = 0;
        GameManager.Instance.DefenderTroopCount = 0;
        List<SpawnInfo> spawnList = GameManager.Instance.DefenderSpawnInfo;
        //Spawn in troops based on spawner data
        foreach (SpawnInfo spawnInfo in spawnList)
        {
            troopToSpawn = DefenderSpawnData.GetTroop(spawnInfo.TroopType);
            for (int i = 0; i < spawnInfo.SpawnLimit; i++)
            {
                var spawnPoint = SpawnPoints[_spawnPointIndex].position; 
                if (_addOffset)
                {
                    float x = spawnPoint.x + _offsetRangeX;
                    float z = spawnPoint.z + _offsetRangeZ;
                    
                    var newSpawnPoint = new Vector3(x,1, z);
                    spawnPoint = newSpawnPoint;
                }
                
                TroopAI troop = Instantiate(troopToSpawn, spawnPoint, SpawnPoints[_spawnPointIndex].rotation);
                troop.AssignToTeam(1);
                if (GameManager.Instance.IsInPlaybackMode)
                {
                    //Assign the ID
                    troop.EntityID = GameManager.Instance.DefenderIDs[_spawnPointIndex];
                    troop.IsInPlaybackMode = true;
                    PlaybackStreamManager.Instance.DefendersList.Add(troop);
                }
                else
                {
                    //Get the ID then Add it to the list and troop
                    troop.EntityID = troop.GetInstanceID();
                    GameManager.Instance.DefenderIDs.Add(troop.EntityID);
                }
                _spawnPointIndex++;
                GameManager.Instance.DefenderTroopCount++;
                if (_spawnPointIndex >= SpawnPoints.Length)
                {
                    _spawnPointIndex = 0;
                    if (_addOffset)
                    {
                        _offsetRangeX -= _offsetRangeX * 2;
                        _offsetRangeZ += _offsetRangeZ;
                    }
                    else
                    {
                        _addOffset = true;    
                    }
                }
            }
        }
        GameObject structureSet = Instantiate(Sets[(int) GameManager.Instance.DefenderRank], StructureSpawnPoint.position, Quaternion.identity, StructureSpawnPoint);
        _defenderParent = structureSet.transform;
    }
}
