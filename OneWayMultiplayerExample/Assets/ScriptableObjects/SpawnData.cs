using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SpawnInfo
{
    public EnemyTypes TroopType;
    public int SpawnLimit;
}

[Serializable]
public struct TroopInfo
{
    public EnemyTypes TroopType;
    public TroopAI TroopPrefab;
}

[CreateAssetMenu(fileName = "SpawnData", menuName = "ScriptableObjects/SpawnData", order = 1)]
public class SpawnData : ScriptableObject
{
    public List<TroopInfo> TroopInfo = new List<TroopInfo>();
    public List<SpawnInfo> EasyParameterList = new List<SpawnInfo>();
    public List<SpawnInfo> MediumParameterList = new List<SpawnInfo>();
    public List<SpawnInfo> HardParameterList = new List<SpawnInfo>();
    public List<SpawnInfo> TestParameterList = new List<SpawnInfo>();
    
    public List<SpawnInfo> GetSpawnList(ArmyDivisionRank in_rank)
    {
        switch (in_rank)
        {
            case ArmyDivisionRank.Easy:
                return EasyParameterList;
            case ArmyDivisionRank.Medium:
                return MediumParameterList;
            case ArmyDivisionRank.Hard:
                return HardParameterList;
            case ArmyDivisionRank.Test:
            default:
                return TestParameterList;
        }
    }

    public TroopAI GetTroop(EnemyTypes typeToGet)
    {
        foreach (TroopInfo spawnParameters in TroopInfo)
        {
            if (spawnParameters.TroopType == typeToGet)
            {
                return spawnParameters.TroopPrefab;
            }
        }
        Debug.LogWarning($"Troop Type doesn't exist in spawn data!! Trying to spawn: {typeToGet.ToString()}");
        return null;
    }
}
