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

    private void Start()
    {
        GameEvents.instance.onIncrementGlobalStat += IncrementGlobalStat; 
    }

    public override void Activate(BrainCloudWrapper bc)
    {
        _bc = bc; 
        m_globalStats = new Dictionary<string, GlobalStat>();
        _bc.GlobalStatisticsService.ReadAllGlobalStats(ReadAllGlobalStatsSuccess, Failure_Callback);
        //m_mainScene.AddLogNoLn("[ReadAllGlobalStats]... ");
    }

    public void IncrementGlobalStat(string globalStatName)
    {
        string jsonData = "{ \"" + globalStatName + "\" : 1 }";

        _bc.GlobalStatisticsService.IncrementGlobalStats(jsonData, Success_Callback, Failure_Callback);
    }

    void OnDisable()
    {
        if(m_globalStats != null)
        {
            foreach (GlobalStat stat in m_globalStats.Values)
            {
                Destroy(stat.gameObject);
            }
        }

        GameEvents.instance.onIncrementGlobalStat -= IncrementGlobalStat;
    }


    //*************** Success Callbacks ***************
    private void ReadAllGlobalStatsSuccess(string json, object cb)
    {
        //m_mainScene.AddLog("SUCCESS");
        //m_mainScene.AddLogJson(json);
        //m_mainScene.AddLog("");

        JsonData jObj = JsonMapper.ToObject(json);
        JsonData jStats = jObj["data"]["statistics"];
        IDictionary dStats = jStats as IDictionary;
        if (dStats != null)
        {
            foreach (string key in dStats.Keys)
            {
                GlobalStat newStat = Instantiate(globalStatPrefab, gStatPefabParent);
                newStat.SetStatName((string)key);
                m_globalStats[newStat.GetStatName()] = newStat;

                JsonData value = (JsonData)dStats[key];
                newStat.SetStatValue(value.IsInt ? (int)value : (long)value); //LitJson can't upcast an int to a long.
            }
        }
    }
    
    public override void Success_Callback(string json, object cbObject)
    {
        base.Success_Callback(json, cbObject);
        
        //{"status":200,"data":{"statisticsExceptions":{},"milestones":{},"experiencePoints":0,"quests":{},"experienceLevel":0,"statistics":{"wood":75}}}
        JsonData jObj = JsonMapper.ToObject(json);
        JsonData jStats = jObj["data"]["statistics"];
        IDictionary dStats = jStats as IDictionary;
        if (dStats != null)
        {
            foreach (string key in dStats.Keys)
            {
                JsonData value = (JsonData) dStats[key];
                long valueAsLong = value.IsInt ? (int) value : (long) value;
                m_globalStats[key].SetStatValue(valueAsLong);
            }
        }
    }

    #region Stuff To Remove
    //public override void OnScreenGUI()
    //{
    //    int minLeftWidth = 120;
        
    //    GUILayout.BeginHorizontal();
    //    GUILayout.Box("Global Stat Name", GUILayout.MinWidth(minLeftWidth));
    //    GUILayout.Box("Global Stat Value");
    //    GUILayout.EndHorizontal();
        
    //    foreach (GlobalStatistic ps in m_stats.Values)
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
    //                _bc.GlobalStatisticsService.IncrementGlobalStats(
    //                    "{ '" + ps.name +"':" + ps.increment +"}",
    //                    Success_Callback, Failure_Callback);
    //                m_mainScene.AddLogNoLn("[IncrementStat]... ");
    //            }
    //        }
    //        GUILayout.EndHorizontal();
    //    }
    //}
    #endregion
}
