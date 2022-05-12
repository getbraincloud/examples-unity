using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DefenderSpawner : MonoBehaviour
{
    public Transform[] SpawnPoints;
    public SpawnData DefenderSpawnData;
    private int _spawnPointIndex;
    private bool _addOffset;
    private int _offsetRange = 6;
    private void Awake()
    {
        BaseTroop troopToSpawn;
        //Spawn in troops based on spawner data
        foreach (SpawnInfo spawnInfo in DefenderSpawnData.SpawnList)
        {
            troopToSpawn = DefenderSpawnData.GetTroop(spawnInfo.TroopType);
            for (int i = 0; i < spawnInfo.SpawnLimit; i++)
            {
                var spawnPoint = SpawnPoints[_spawnPointIndex].position; 
                if (_addOffset)
                {
                    float x = spawnPoint.x + _offsetRange;
                    float z = spawnPoint.z + _offsetRange;
                    
                    var newSpawnPoint = new Vector3(x,0, z);
                    spawnPoint = newSpawnPoint;
                }
                
                Instantiate(troopToSpawn, spawnPoint, Quaternion.identity);
                
                _spawnPointIndex++;
                if (_spawnPointIndex >= SpawnPoints.Length)
                {
                    _spawnPointIndex = 0;
                    if (_addOffset)
                    {
                        _offsetRange += _offsetRange;
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
