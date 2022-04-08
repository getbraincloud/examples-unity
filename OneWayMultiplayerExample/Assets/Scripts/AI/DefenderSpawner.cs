using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefenderSpawner : MonoBehaviour
{
    public GameObject[] SpawnPoints;
    public BaseTroop[] TroopList;
    public float OffsetY;
    
    private void Awake()
    {
        if (SpawnPoints.Length != TroopList.Length)
        {
            Debug.LogWarning("Need to match Troop List and Spawn Points in DefenderSpawner");
        }
        
        BaseTroop troop;
        for (int i = 0; i < SpawnPoints.Length; i++)
        {
            var spawnPoint = SpawnPoints[i].transform.position;
            spawnPoint.y = OffsetY;
            troop = Instantiate(TroopList[i], SpawnPoints[i].transform.position, SpawnPoints[i].transform.rotation);
            troop.AssignToTeam(1);
            
        }
    }
}
