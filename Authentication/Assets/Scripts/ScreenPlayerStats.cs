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

    private void Start()
    {
        GameEvents.instance.onIncrementUserStat += IncrementUserStats;
        GameEvents.instance.onInstantiatePlayerStats += InstantiatePlayerStats;
    }

    public override void Activate(BrainCloudWrapper bc)
    {
        _bc = bc;
        m_playerStats = new Dictionary<string, PlayerStat>();
        
        BrainCloudInterface.instance.ReadUserState(this);
    }

    public void IncrementUserStats()
    {
        foreach (string key in DataManager.instance.PlayerStats.Keys)
        {
            m_playerStats[key].SetStatValue(DataManager.instance.PlayerStats[key]);
        }
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

    private void OnDisable()
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

        //GameEvents.instance.onIncrementUserStat -= IncrementUserStats;
    }

    #region Stuff to Remove
    //public override void OnScreenGUI()
    //{
    //    int minLeftWidth = 120;
        
    //    GUILayout.BeginHorizontal();
    //    GUILayout.Box("Player Stat Name", GUILayout.MinWidth(minLeftWidth));
    //    GUILayout.Box("Player Stat Value");
    //    GUILayout.EndHorizontal();
        
    //    foreach (PlayerStatistic ps in m_stats.Values)
    //    {
    //        GUILayout.BeginVertical();
    //        GUILayout.Space(5);
    //        GUILayout.EndVertical();
            
    //        GUILayout.BeginHorizontal();
    //        GUILayout.Label(ps.name, GUILayout.MinWidth(minLeftWidth));
    //        GUILayout.Box(ps.value.ToString());
    //        GUILayout.EndHorizontal();
            
    //        // increment
    //        GUILayout.BeginHorizontal();
    //        GUILayout.Space(minLeftWidth);
    //        ps.increment = GUILayout.TextField(ps.increment, GUILayout.ExpandWidth(true));
    //        if (GUILayout.Button("Increment"))
    //        {
    //            long valueAsLong = 0;
    //            double valueAsDouble = 0;
    //            if (long.TryParse(ps.increment, out valueAsLong)
    //                || double.TryParse(ps.increment, out valueAsDouble))
    //            {
    //            	_bc.PlayerStatisticsService.IncrementUserStats(
    //           	    	"{ '" + ps.name +"':" + ps.increment +"}",
    //                	Success_Callback, Failure_Callback);
    //           		m_mainScene.AddLogNoLn("[IncrementStat]... ");
    //            }
    //        }
    //        GUILayout.EndHorizontal();
    //    }
    //}
    #endregion
}
