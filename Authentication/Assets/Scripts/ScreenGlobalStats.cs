using UnityEngine;
using System.Collections;
using BrainCloud.LitJson;
using System.Collections.Generic;

public class ScreenGlobalStats : BCScreen 
{

    Dictionary<string, GlobalStat> m_globalStats;

    [SerializeField] GlobalStat globalStatPrefab; 
    [SerializeField] Transform gStatPefabParent;

    public ScreenGlobalStats(BrainCloudWrapper bc) : base(bc) { }

    public override void Activate(BrainCloudWrapper bc)
    {
        GameEvents.instance.onIncrementGlobalStat += IncrementGlobalStat;
        GameEvents.instance.onInstantiateGlobalStats += InstantiateGlobalStats; 

        _bc = bc; 
        m_globalStats = new Dictionary<string, GlobalStat>();
        BrainCloudInterface.instance.ReadAllGlobalStats();
    }

    public void InstantiateGlobalStats()
    {
        DataManager dataManager = DataManager.instance; 

        foreach(string key in dataManager.GlobalStats.Keys)
        {
            GlobalStat newStat = Instantiate(globalStatPrefab, gStatPefabParent);

            if(newStat != null)
            {
                m_globalStats[key] = newStat;
                m_globalStats[key].SetStatName(key);
                m_globalStats[key].SetStatValue(dataManager.GlobalStats[key]);
            }
        }
    }

    public void IncrementGlobalStat(string globalStatName)
    {
        if (!m_globalStats.ContainsKey(globalStatName))
            return;

        DataManager dataManager = DataManager.instance;

        m_globalStats[globalStatName].SetStatValue(dataManager.GlobalStats[globalStatName]);
    }

    protected override void OnDisable()
    {
        if(m_globalStats != null)
        {
            foreach (GlobalStat stat in m_globalStats.Values)
            {
                Destroy(stat.gameObject);
            }

            m_globalStats.Clear();
            m_globalStats = null;
        }

        GameEvents.instance.onIncrementGlobalStat -= IncrementGlobalStat;
        GameEvents.instance.onInstantiateGlobalStats -= InstantiateGlobalStats;
    }
}
