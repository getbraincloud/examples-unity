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

    private void Awake()
    {
        if (HelpMessage == null)
        {
            HelpMessage =   "The player stats screen displays all pre-defined statistics rules from the \"User Statistics\" page under the \"Statistics Rules\" tab in the brainCloud portal.\n\n" +
                            "Player stats are user scoped and are only accessible to the user they belong to.\n\n" +
                            "Pressing the increment button next to each player stat will increment that stat by 1.\n\n";
        }

        if (HelpURL == null)
        {
            HelpURL = "https://getbraincloud.com/apidocs/apiref/?cloudcode#capi-playerstats";
        }
    }

    public override void Activate()
    {
        GameEvents.instance.onIncrementUserStat += IncrementUserStats;
        GameEvents.instance.onInstantiatePlayerStats += InstantiatePlayerStats;

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
