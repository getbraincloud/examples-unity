using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

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
    private void Awake()
    {
        BaseTroop troopToSpawn;
        Instantiate(Sets[(int) DefenderSpawnData.Rank], StructureSpawnPoint.position, Quaternion.identity, StructureSpawnPoint);
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
                
                BaseTroop troop = Instantiate(troopToSpawn, spawnPoint, Quaternion.identity);
                troop.AssignToTeam(1);
                _spawnPointIndex++;
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
    }
}
