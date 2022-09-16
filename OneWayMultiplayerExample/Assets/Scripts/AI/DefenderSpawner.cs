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

    private void Start()
    {
        if (BrainCloudManager.Instance == null)
        {
            TestingMode = true;
            DefenderSpawnData.Rank = ArmyDivisionRank.Medium;
            DefenderSpawnData.SpawnList = DefenderSpawnData.TestParameterList;
        }
        SpawnDefenderSetup();
    }

    public void SpawnDefenderSetup()
    {
        TroopAI troopToSpawn;
        _addOffset = false;
        _spawnPointIndex = 0;
        GameManager.Instance.DefenderTroopCount = 0;
        //Spawn in troops based on spawner data
        foreach (SpawnInfo spawnInfo in DefenderSpawnData.SpawnList)
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
                
                TroopAI troop = Instantiate(troopToSpawn, spawnPoint, Quaternion.identity);
                troop.AssignToTeam(1);
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
        GameObject structureSet = Instantiate(Sets[(int) DefenderSpawnData.Rank], StructureSpawnPoint.position, Quaternion.identity, StructureSpawnPoint);
        GameManager.Instance.SetUpGameValues(structureSet.transform);
    }
}
