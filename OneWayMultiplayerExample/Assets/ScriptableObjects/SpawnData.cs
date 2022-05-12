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
    public BaseTroop TroopPrefab;
}

[CreateAssetMenu(fileName = "SpawnData", menuName = "ScriptableObjects/SpawnData", order = 1)]
public class SpawnData : ScriptableObject
{
    public List<TroopInfo> TroopInfo = new List<TroopInfo>();
    
    private List<SpawnInfo> _spawnParametersList = new List<SpawnInfo>();

    private ArmyDivisionRank _rank = ArmyDivisionRank.None;

    public ArmyDivisionRank Rank
    {
        get => _rank;
        set => _rank = value;
    }
    public List<SpawnInfo> SpawnList
    {
        get => _spawnParametersList;
        set => _spawnParametersList = value;
    }

    public List<SpawnInfo> EasyParameterList = new List<SpawnInfo>();
    public List<SpawnInfo> MediumParameterList = new List<SpawnInfo>();
    public List<SpawnInfo> HardParameterList = new List<SpawnInfo>();
    
    public void AssignSpawnList(ArmyDivisionRank in_rank)
    {
        _rank = in_rank;
        switch (in_rank)
        {
            case ArmyDivisionRank.Easy:
                _spawnParametersList = EasyParameterList;
                break;
            case ArmyDivisionRank.Medium:
                _spawnParametersList = MediumParameterList;
                break;
            case ArmyDivisionRank.Hard:
                _spawnParametersList = HardParameterList;
                break;
        }
    }

    public int ReturnSpawnLimit(EnemyTypes typeToLimit)
    {
        foreach (SpawnInfo spawnParameters in _spawnParametersList)
        {
            if (spawnParameters.TroopType == typeToLimit)
            {
                return spawnParameters.SpawnLimit;
            }
        }
        Debug.LogWarning($"Troop Type doesn't exist in spawn data!! Trying to spawn: {typeToLimit.ToString()}");
        return 1;
    }

    public BaseTroop GetTroop(EnemyTypes typeToGet)
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
