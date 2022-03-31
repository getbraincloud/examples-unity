using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SpawnParameters
{
    public EnemyTypes TroopType;
    public BaseTroop TroopToSpawn;
}

[CreateAssetMenu(fileName = "SpawnData", menuName = "ScriptableObjects/SpawnData", order = 1)]
public class SpawnData : ScriptableObject
{
    public List<SpawnParameters> SpawnParametersList = new List<SpawnParameters>();

    public BaseTroop GetTroop(EnemyTypes typeToGet)
    {
        foreach (SpawnParameters spawnParameters in SpawnParametersList)
        {
            if (spawnParameters.TroopType == typeToGet)
            {
                return spawnParameters.TroopToSpawn;
            }
        }
        Debug.LogWarning($"Troop Type doesn't exist in spawn data!! Trying to spawn: {typeToGet.ToString()}");
        return null;
    }
}