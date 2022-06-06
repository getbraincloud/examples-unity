using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.LitJson;

public class ScreenPlayerStats : BCScreen
{

    [SerializeField] PlayerStat playerStatPrefab;
    [SerializeField] Transform pStatPrefabParent;

    Dictionary<string, PlayerStat> m_playerStats; 
    
    public ScreenPlayerStats(BrainCloudWrapper bc) : base(bc) { }

    public override void Activate(BrainCloudWrapper bc)
    {
        GameEvents.instance.onIncrementUserStat += IncrementUserStats;
        GameEvents.instance.onInstantiatePlayerStats += InstantiatePlayerStats;

        _bc = bc;
        m_playerStats = new Dictionary<string, PlayerStat>();
        
        BrainCloudInterface.instance.ReadUserState(this);
    }

    public void IncrementUserStats(string statName)
    {
        if (!m_playerStats.ContainsKey(statName))
            return;

        DataManager dataManager = DataManager.instance;

        m_playerStats[statName].SetStatValue(dataManager.PlayerStats[statName]);
    }

    public void InstantiatePlayerStats()
    {
        DataManager dataManager = DataManager.instance; 

        foreach (string key in dataManager.PlayerStats.Keys)
        {
            PlayerStat newStat = Instantiate(playerStatPrefab, pStatPrefabParent);
            newStat.SetStatName(key);
            m_playerStats[newStat.GetStatName()] = newStat;
            newStat.SetStatValue(dataManager.PlayerStats[key]);
        }
    }

    protected override void OnDisable()
    {
        if (m_playerStats != null)
        {
            foreach (string key in m_playerStats.Keys)
            {
                Destroy(m_playerStats[key].gameObject);
            }

            m_playerStats.Clear();
            m_playerStats = null; 
        }

        GameEvents.instance.onIncrementUserStat -= IncrementUserStats;
        GameEvents.instance.onInstantiatePlayerStats -= InstantiatePlayerStats;
    }
}
